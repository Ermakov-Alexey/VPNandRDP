using LogsFile;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace VaR;

public class OpenVpnManager(string configFullPath) : IDisposable
{
    private Process _process;
    private OpenVpnManagementClient _mgmtClient;
    private bool _isRunning;
    private readonly string _workDir = Path.GetDirectoryName(Path.GetDirectoryName(configFullPath));
    public event Action<string> OnLogMessage;
    private readonly bool _connectionEstablished = false;
    private TaskCompletionSource<bool> _connectionTcs;
    public string GetWorkDir => _workDir;
    public async Task<bool> StartAsync(int timeoutSeconds = 30)
    {
        if (_isRunning) return true;
        var procInfo = new ProcessStartInfo
        {
            FileName = Path.Combine(_workDir, "bin", "openvpn.exe"),
            Arguments = $"--config \"{configFullPath}\" --auth-retry interact",
            WorkingDirectory = _workDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            StandardOutputEncoding = Encoding.GetEncoding(866),
            StandardErrorEncoding = Encoding.GetEncoding(866),
            Verb = "runas"
        };
        _process = new Process { StartInfo = procInfo, EnableRaisingEvents = true };
        _process.OutputDataReceived += (_, e) => HandleLog(e.Data);
        _process.ErrorDataReceived += (_, e) => HandleLog(e.Data);

        _process.Exited += (_, _) =>
        {
            _isRunning = false;
            // Если процесс умер до подключения — сообщаем об ошибке
            if (!_connectionEstablished && !_connectionTcs.Task.IsCompleted)
                _connectionTcs.TrySetResult(false);
        };
        _process.Start();
        _isRunning = true;
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
        
        OnLogMessage?.Invoke("OpenVPN запущен. Ожидание подключения...");

        // ЗАПУСК МЕНЕДЖЕРА ПАРАЛЛЕЛЬНО
        // Мы не ждем его завершения, он работает в фоне и обновляет флаги
        _connectionTcs = new TaskCompletionSource<bool>(); // Инициализируем
        _ = RunManagementClientAsync(timeoutSeconds, _connectionTcs);

        // Ждем результата подключения (успех или провал) с общим таймаутом
        var delayTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));
        var completedTask = await Task.WhenAny(_connectionTcs.Task, delayTask);

        if (completedTask == delayTask)
        {
            // Если таймаут истек, проверяем, не пришел ли успех в последнюю миллисекунду
            if (_connectionTcs.Task.IsCompleted && _connectionTcs.Task.Result)
            {
                OnLogMessage?.Invoke("Успех получен в последний момент!");
                return true;
            }

            OnLogMessage?.Invoke($"Таймаут ожидания подключения ({timeoutSeconds} сек).");
            await StopAsync();
            return false;
        }

        bool success = await _connectionTcs.Task;
        if (!success)
        {
            OnLogMessage?.Invoke("Ошибка подключения.");
            await StopAsync();
            return false;
        }

        OnLogMessage?.Invoke("Соединение успешно установлено и активно.");
        return true;
    }
    private async Task RunManagementClientAsync(int timeoutSeconds, TaskCompletionSource<bool> tcs)
    {
        await Task.Delay(1000);
        _mgmtClient = new OpenVpnManagementClient();

        // Подписки
        _mgmtClient.OnStateChange += (state) =>
        {
            OnLogMessage?.Invoke($"Статус OpenVPN: {state}");
            // Здесь можно обновлять UI статусы в реальном времени
        };

        _mgmtClient.OnFatalError += (msg) =>
        {
            OnLogMessage?.Invoke($"Критическая ошибка Mgmt: {msg}");
            // Если соединение уже было установлено, но потом упало - можно инициировать переподключение
            if (!_connectionEstablished)
            {
                _connectionTcs.TrySetResult(false);
            }
        };

        try
        {
            // ВАЖНО: Этот метод теперь вернется сразу после получения статуса CONNECTED
            // или выбросит исключение при таймауте/ошибке.
            // Он НЕ будет висеть все время работы VPN.
            await _mgmtClient.ConnectAsync(timeoutSeconds: timeoutSeconds, externalTcs: tcs);

            // Если мы здесь, значит соединение УСПЕШНО установлено.
            // Метод ConnectAsync внутри себя запустил ReadLoop в фоне.
            // Нам больше ничего делать не нужно, клиент будет слать события через OnStateChange.
        } catch (Exception ex)
        {
            // Если исключение вылетело, но задача уже помечена как успешная (из-за внешнего сигнала) - игнорируем ошибку таймаута
            if (tcs.Task.IsCompleted && tcs.Task.Result)
            {
                Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), "Игнорируем ошибку таймаута, т.к. соединение уже установлено по логам.");
                return;
            }

            OnLogMessage?.Invoke($"Mgmt ошибка: {ex.Message}");
            if (!tcs.Task.IsCompleted) tcs.TrySetResult(false);
        }
    }
    private void HandleLog(string msg)
    {
        if (string.IsNullOrEmpty(msg)) return;
        OnLogMessage?.Invoke($"[OUT] {msg}");

        // ГЛАВНОЕ ИЗМЕНЕНИЕ ЗДЕСЬ:
        if (msg.Contains("Initialization Sequence Completed"))
        {
            // Сообщаем клиенту управления, что всё ОК, даже если он еще ждет событие от сокета
            _mgmtClient?.ReportConnectionEstablishedExternally();

            // И напрямую завершаем задачу ожидания
            if (_connectionTcs != null && !_connectionTcs.Task.IsCompleted)
                _connectionTcs.TrySetResult(true);
        }

        // Обработка ошибок...
        if (msg.Contains("TLS Error") || msg.Contains("Connection refused"))
        {
            if (_connectionTcs != null && !_connectionTcs.Task.IsCompleted)
                _connectionTcs.TrySetResult(false);
        }
    }
    public async Task StopAsync()
    {
        if (!_isRunning && _process == null) return;

        OnLogMessage?.Invoke("Остановка OpenVPN...");

        try
        {
            // 1. Пытаемся остановить мягко через Management Interface
            Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), "Шаг 1: Вызываем mgmt.StopGracefullyAsync");
            if (_mgmtClient != null)
            {
                // Важно: ConfigureAwait(false) позволяет продолжить выполнение не в UI потоке,
                // если мы уже не в нем, но главное здесь - сама логика внутри клиента.
                await _mgmtClient.StopGracefullyAsync().ConfigureAwait(false);
                Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), "Шаг 2: mgmt.StopGracefullyAsync завершен");
            }

            // 2. Асинхронно ждем завершения процесса
            Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), "Шаг 3: Проверяем процесс");
            if (_process is { HasExited: false })
            {
                Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), "Шаг 4: Запускаем ожидание выхода процесса");

                // ВАЖНО: WaitForExit блокирует поток. Мы запускаем его в пуле потоков,
                // чтобы не блокировать ни UI, ни текущий контекст await.
                // ReSharper disable once AccessToDisposedClosure
                var exitTask = Task.Run(() => _process.WaitForExit(10000));

                // Ждем либо выхода процесса, либо таймаута в 10 секунд
                var timeoutTask = Task.Delay(10000);
                var completedTask = await Task.WhenAny(exitTask, timeoutTask).ConfigureAwait(false);

                // Если завершился таймаут (значит процесс не вышел за 10 сек)
                if (completedTask == timeoutTask)
                {
                    Logs.Warn(GetType(), MethodBase.GetCurrentMethod(), "Шаг 6: Таймаут, убиваем процесс");
                    try
                    {
                        _process.Kill();
                        // После Kill тоже нужно подождать, чтобы система освободила ресурсы
                        // Опять используем Task.Run, чтобы не блокировать
                        // ReSharper disable once AccessToDisposedClosure
                        await Task.Run(() => _process.WaitForExit(2000)).ConfigureAwait(false);
                    } catch (Exception ex)
                    {
                        Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Ошибка при Kill процесса", ex);
                    }
                }
                else
                {
                    Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), "Шаг 5: Процесс вышел сам");
                }
            }
            Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), "Шаг 7: Очистка ресурсов");
        } catch (Exception ex)
        {
            Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Ошибка при остановке", ex);
        } finally
        {
            // Очищаем ресурсы
            _mgmtClient?.Dispose();
            _process?.Dispose();
            _process = null;
            _isRunning = false;

            OnLogMessage?.Invoke("Процесс OpenVPN завершен.");
        }
    }
    public void Dispose()
    {
        _ = StopAsync();
    }
}