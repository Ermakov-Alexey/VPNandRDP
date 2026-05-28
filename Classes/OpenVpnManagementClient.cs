using System;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LogsFile;

public class OpenVpnManagementClient : IDisposable
{
    private TcpClient _client;
    private StreamReader _reader;
    private StreamWriter _writer;
    private bool _connected;
    private CancellationTokenSource _readCts;

    public event Action<string> OnStateChange;
    public event Action<string> OnFatalError;

    private TaskCompletionSource<bool> _externalSuccessTcs;

    private string _currentState = "INIT";

    /// <summary>
    /// Подключается к сокету и ждет статуса CONNECTED.
    /// Если externalTcs не null, то успех можно сигнализировать через него (из парсера логов).
    /// </summary>
    public async Task ConnectAsync(string host = "127.0.0.1", int port = 7505, int timeoutSeconds = 40, TaskCompletionSource<bool> externalTcs = null)
    {
        _externalSuccessTcs = externalTcs;
        _readCts = new CancellationTokenSource();
        var globalCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            // --- ЭТАП 1: ПОДКЛЮЧЕНИЕ К СОКЕТУ (С ПОВТОРАМИ) ---
            bool socketConnected = false;
            DateTime startTime = DateTime.Now;

            while (!socketConnected && (DateTime.Now - startTime).TotalSeconds < timeoutSeconds)
            {
                try
                {
                    _client = new TcpClient();
                    var connectTask = _client.ConnectAsync(host, port);
                    if (await Task.WhenAny(connectTask, Task.Delay(500, globalCts.Token)) == connectTask)
                    {
                        socketConnected = true;
                        Logs.Trace(typeof(OpenVpnManagementClient),MethodBase.GetCurrentMethod(), "Socket connected.");
                    }
                    else
                    {
                        _client.Dispose();
                        await Task.Delay(500, globalCts.Token);
                    }
                } catch (Exception)
                {
                    _client?.Dispose();
                    await Task.Delay(500, globalCts.Token);
                }
            }

            if (!socketConnected)
                throw new TimeoutException($"Не удалось подключиться к порту {port}.");

            _reader = new StreamReader(_client.GetStream(), Encoding.UTF8);
            _writer = new StreamWriter(_client.GetStream(), Encoding.UTF8) { AutoFlush = true };
            _connected = true;

            // --- ЭТАП 2: ОТПРАВКА КОМАНД ---
            await _writer.WriteLineAsync("state on");
            await _writer.WriteLineAsync("log all on");
            Logs.Trace(typeof(OpenVpnManagementClient), MethodBase.GetCurrentMethod(), "Commands sent.");

            // --- ЭТАП 3: ЗАПУСК ФОНОВОГО ЧТЕНИЯ ---
            _ = ReadLoopAsync(_readCts.Token);

            // --- ЭТАП 4: ОЖИДАНИЕ СТАТУСА CONNECTED ---
            var successTcs = new TaskCompletionSource<bool>();

            // Функция для фиксации успеха (вызывается и из событий, и извне)
            void ReportSuccess()
            {
                if (!successTcs.Task.IsCompleted)
                    successTcs.TrySetResult(true);

                // Если успех пришел извне (из логов), а мы еще ждем здесь - тоже завершаем
                if (_externalSuccessTcs != null && !_externalSuccessTcs.Task.IsCompleted)
                    _externalSuccessTcs.TrySetResult(true);
            }

            Action<string> tempHandler = state =>
            {
                if (state == "CONNECTED")
                {
                    Logs.Trace(typeof(OpenVpnManagementClient), MethodBase.GetCurrentMethod(), "STATE: CONNECTED received via Socket.");
                    ReportSuccess();
                }
                else if (state == "EXITING" || state == "RECONNECTING" || state == "AUTH_FAILED")
                {
                    if (!successTcs.Task.IsCompleted) successTcs.TrySetResult(false);
                    if (_externalSuccessTcs != null && !_externalSuccessTcs.Task.IsCompleted) _externalSuccessTcs.TrySetResult(false);
                }
            };

            OnStateChange += tempHandler;

            try
            {
                var delayTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), globalCts.Token);
                var completed = await Task.WhenAny(successTcs.Task, delayTask);

                if (completed == delayTask)
                {
                    // ПРОВЕРКА: Может быть, успех уже пришел извне (через externalTcs)?
                    if (_externalSuccessTcs != null && _externalSuccessTcs.Task.IsCompleted && _externalSuccessTcs.Task.Result)
                    {
                        Logs.Trace(typeof(OpenVpnManagementClient), MethodBase.GetCurrentMethod(), "Success reported externally (via Log Parser). Ignoring socket timeout.");
                        return; // Выходим успешно!
                    }

                    throw new TimeoutException($"Таймаут ожидания статуса CONNECTED ({timeoutSeconds} сек).");
                }

                if (!await successTcs.Task)
                    throw new Exception("Получен статус ошибки от OpenVPN.");

                Logs.Trace(typeof(OpenVpnManagementClient), MethodBase.GetCurrentMethod(), "SUCCESS.");
            } finally
            {
                OnStateChange -= tempHandler;
            }
        } catch (OperationCanceledException) when (globalCts.IsCancellationRequested)
        {
            // Проверка на внешний успех при отмене
            if (_externalSuccessTcs != null && _externalSuccessTcs.Task.IsCompleted && _externalSuccessTcs.Task.Result)
                return;

            throw new TimeoutException("Общий таймаут операции.");
        } catch (Exception ex)
        {
            Logs.Error(typeof(OpenVpnManagementClient), MethodBase.GetCurrentMethod(), "Error", ex);
            Dispose();
            throw;
        }
    }

    // Метод, который можно вызвать ИЗВНЕ (например, из парсера логов), чтобы сказать "ВСЁ РАБОТАЕТ"
    public void ReportConnectionEstablishedExternally()
    {
        if (_externalSuccessTcs != null && !_externalSuccessTcs.Task.IsCompleted)
        {
            Logs.Trace(typeof(OpenVpnManagementClient), MethodBase.GetCurrentMethod(), "External success signal received (Initialization Sequence Completed).");
            _externalSuccessTcs.TrySetResult(true);
        }
    }

    private async Task ReadLoopAsync(CancellationToken token)
    {
        while (_connected && _client?.Connected == true && !token.IsCancellationRequested)
        {
            try
            {
                string line = await _reader.ReadLineAsync().WithCancellation(token);
                if (string.IsNullOrEmpty(line)) break;

                if (line.StartsWith(">STATE:"))
                {
                    var parts = line.Split(',');
                    if (parts.Length >= 3)
                    {
                        string newState = parts[2];
                        if (newState != _currentState)
                        {
                            _currentState = newState;
                            OnStateChange?.Invoke(newState);
                        }
                    }
                }
                else if (line.StartsWith(">LOG:") && line.Contains(",FATAL,"))
                {
                    OnFatalError?.Invoke(line);
                }
            } catch (OperationCanceledException) { break; } catch (IOException) { break; } catch (Exception ex)
            {
                Logs.Error(typeof(OpenVpnManagementClient), MethodBase.GetCurrentMethod(), "Read error", ex);
                break;
            }
        }
    }

    public async Task StopGracefullyAsync()
    {
        if (!_connected || _writer == null) return;
        try
        {
            Logs.Trace(typeof(OpenVpnManagementClient), MethodBase.GetCurrentMethod(), "1. Отправляем сигнал ");
            await _writer.WriteLineAsync("signal SIGTERM").ConfigureAwait(false);
            await _writer.FlushAsync().ConfigureAwait(false);
        } catch (Exception ex)
        {
            Logs.Trace(typeof(OpenVpnManagementClient),MethodBase.GetCurrentMethod(), $"Ошибка при отправке SIGTERM: {ex.Message}");
        } finally
        {
            try
            {
                Logs.Trace(typeof(OpenVpnManagementClient), MethodBase.GetCurrentMethod(), "2. Важно: принудительно закрываем writer... ");
                _writer.Close();
                _writer = null;
            } catch
            {
                // ignored
            }
        }
    }

    public void Dispose()
    {
        _connected = false;
        _readCts?.Cancel();
        _reader?.Dispose();
        _writer?.Dispose();
        _client?.Close();
        _client?.Dispose();
        Logs.Trace(typeof(OpenVpnManagementClient), MethodBase.GetCurrentMethod(), "Client disposed.");
    }
}

public static class TaskExtensions
{
    public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<bool>();
        using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
        {
            if (task == await Task.WhenAny(task, tcs.Task).ConfigureAwait(false))
                return await task.ConfigureAwait(false);
            throw new OperationCanceledException(cancellationToken);
        }
    }
}