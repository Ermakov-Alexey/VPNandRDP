using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace VaR;
public class UserActionLogger : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _apiUrl;
    private readonly string _queueFile;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Timer _flushTimer;

    public UserActionLogger(string apiUrl)
    {
        _apiUrl = apiUrl.TrimEnd('/');
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        _http.DefaultRequestHeaders.Add("X-Api-Key", Constants.VaRKey);
        _queueFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "IT", "VaR", "logs_queue.json");

        // Отправляем накопленное каждые 2 минуты
        _flushTimer = new Timer(async void (_) =>
        {
            try
            {
                await FlushQueueAsync();
            }
            catch
            {
                // ignored
            }
        }, null, TimeSpan.Zero, TimeSpan.FromMinutes(2));
    }

    public async Task LogAsync(UserActionLog log)
    {
        // Сначала пробуем отправить сразу
        if (await SendAsync([log])) return;
        // Не вышло -> кладём в очередь
        await EnqueueAsync(log);
    }

    private async Task<bool> SendAsync(IEnumerable<UserActionLog> logs)
    {
        try
        {
            // Newtonsoft.Json
            var json = JsonConvert.SerializeObject(logs);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync($"{_apiUrl}/api/logs/batch", content);
            return response.IsSuccessStatusCode;
        } catch { return false; }
    }

    private async Task EnqueueAsync(UserActionLog log)
    {
        await _lock.WaitAsync();
        try
        {
            List<UserActionLog> list = new();
            if (File.Exists(_queueFile))
            {
                // В .NET Framework оборачиваем синхронные методы в Task.Run
                var json = await Task.Run(() => File.ReadAllText(_queueFile));
                list = JsonConvert.DeserializeObject<List<UserActionLog>>(json) ?? new();
            }
            list.Add(log);
            var newJson = JsonConvert.SerializeObject(list);
            await Task.Run(() => File.WriteAllText(_queueFile, newJson));
        } finally { _lock.Release(); }
    }

    public async Task FlushQueueAsync()
    {
        if (!await _lock.WaitAsync(0)) return; // Уже идёт отправка
        try
        {
            if (!File.Exists(_queueFile)) return;

            var json = await Task.Run(() => File.ReadAllText(_queueFile));
            var list = JsonConvert.DeserializeObject<List<UserActionLog>>(json);
            if (list == null || list.Count == 0) return;

            if (await SendAsync(list))
            {
                // Успешно отправили -> очищаем очередь
                await Task.Run(() => File.WriteAllText(_queueFile, @"[]"));
            }
        } finally { _lock.Release(); }
    }

    public void Dispose()
    {
        _flushTimer?.Dispose();
        _http?.Dispose();
        _lock?.Dispose();
    }
}
