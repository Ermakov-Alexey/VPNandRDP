using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

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
    /// <summary>
    /// Чтобы найти интерфейс, который прямо сейчас отправляет данные в интернет, нужно установить быстрое "ложное" соединение.
    /// Код ниже находит правильный IP-адрес без отправки реальных пакетов в сеть.
    /// </summary>
    public static string GetActiveLocalIp()
    {
        try
        {
            // Используем любой внешний IP, например, DNS Google. 
            // Реального подключения не происходит, сервер не пингуется.
            using (var socket = new System.Net.Sockets.Socket(
                       System.Net.Sockets.AddressFamily.InterNetwork,
                       System.Net.Sockets.SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                var endPoint = socket.LocalEndPoint as System.Net.IPEndPoint;
                return endPoint?.Address.ToString() ?? "127.0.0.1";
            }
        } catch
        {
            return "127.0.0.1";
        }
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
    //TODO надо проверить
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