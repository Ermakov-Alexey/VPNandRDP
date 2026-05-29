using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace VaR;

public static class IpHelper
{
    private static string _cachedPublicIp;
    private static DateTime _cacheTime;

    public static string GetLocalIp()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
            }
        }
        catch
        {
            // ignored
        }

        return "127.0.0.1";
    }

    public static async Task<string> GetPublicIpAsync()
    {
        // Кэшируем на 10 минут, чтобы не спамить внешний сервис
        if (_cachedPublicIp != null && (DateTime.Now - _cacheTime).TotalMinutes < 10)
            return _cachedPublicIp;

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var ip = await client.GetStringAsync("https://api.ipify.org");
            _cachedPublicIp = ip;
            _cacheTime = DateTime.Now;
            return ip;
        } catch
        {
            return GetLocalIp(); // Фоллбэк
        }
    }

    public static async Task<Guid?> GetClientGuidAsync()
    {
        try
        {
            string path = @"C:\IT\IT_RService\config.json";
            // Чтение файла выносим в пул потоков, чтобы не блокировать UI
            var json = await Task.Run(() => File.ReadAllText(path));

            var jObj = JObject.Parse(json);
            if (jObj["guid"] != null && Guid.TryParse(jObj["guid"].ToString(), out var guid))
            {
                return guid;
            }
        } catch { /* Файла нет или нет доступа - не критично */ }
        return null;
    }
}