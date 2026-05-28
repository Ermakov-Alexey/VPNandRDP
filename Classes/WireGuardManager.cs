using LogsFile;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.AccessControl;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace VaR;

public class WireGuardManager
{
    private static readonly string UserDirectory = Path.Combine(Program.AppPath, "Config");
    private static readonly string ConfigFile = Path.Combine(UserDirectory, "wg.conf");
    private readonly Thread _transferUpdateThread;
    private volatile bool _threadsRunning;
    private bool _connected;
    private readonly string _user;
    private readonly ToolStripItem _textBox;
    public WireGuardManager()
    {

    }
    public WireGuardManager(string user, ToolStripItem textBox)
    {
        Application.ApplicationExit += Application_ApplicationExit;
        MakeConfigDirectory();
        _transferUpdateThread = new Thread(TailTransfer) { IsBackground = true };
        _user = user;
        _textBox = textBox;
    }
    private static void MakeConfigDirectory()
    {
        var ds = new DirectorySecurity();
        ds.SetSecurityDescriptorSddlForm("O:BAG:BAD:PAI(A;OICI;FA;;;BA)(A;OICI;FA;;;SY)");
        ds.CreateDirectory(UserDirectory);
    }
    public void ConnectionRemove()
    {
        _threadsRunning = false;
        try
        {
            if (_transferUpdateThread != null)
            {
                _transferUpdateThread.Interrupt();
                _transferUpdateThread.Join();
            }
        } catch (Exception ex)
        {
            Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Error", ex);
        }

        Tunnel.Service.Remove(ConfigFile, true);
        Application.ApplicationExit -= Application_ApplicationExit;
        try
        {
            if (File.Exists(ConfigFile))
                File.Delete(ConfigFile);
            if (Directory.Exists(UserDirectory))
                Directory.Delete(UserDirectory, true);
        } catch (Exception ex)
        {
            Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Error", ex);
        }
    }
    public async Task<bool> ConnectionCreateAndConnect()
    {
        _threadsRunning = true;
        _transferUpdateThread.Start();
        try
        {
            var config = await GenerateNewConfig();
            File.WriteAllText(ConfigFile, config);
            Tunnel.Service.Add(ConfigFile, true);
            Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), "WireGuard connect");

            _connected = true;
        } catch (Exception ex)
        {
            Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Error Create WireGuard", ex);
            Tunnel.Service.Remove(ConfigFile, true);
            Application.ApplicationExit -= Application_ApplicationExit;
            try
            {
                if (File.Exists(ConfigFile))
                    File.Delete(ConfigFile);
            } catch (Exception ex1)
            {
                Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Error", ex1);
            }
            _connected = false;
        }
        return _connected;
    }
    private void Application_ApplicationExit(object sender, EventArgs e)
    {
        Tunnel.Service.Remove(ConfigFile, true);
        try
        {
            if (File.Exists(ConfigFile))
                File.Delete(ConfigFile);
        } catch (Exception ex)
        {
            Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Error", ex);
        }
    }
    private void TailTransfer()
    {
        Tunnel.Driver.Adapter adapter = null;
        while (_threadsRunning)
        {
            if (adapter == null)
            {
                while (_threadsRunning)
                {
                    try
                    {
                        adapter = Tunnel.Service.GetAdapter(ConfigFile);
                        break;
                    } catch
                    {
                        try
                        {
                            Thread.Sleep(1000);
                        } catch (Exception ex) { Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Error", ex); }
                    }
                }
            }
            if (adapter == null)
                continue;
            try
            {
                ulong rx = 0, tx = 0;
                var config = adapter.GetConfiguration();
                foreach (var peer in config.Peers)
                {
                    rx += peer.RxBytes;
                    tx += peer.TxBytes;
                }
                _textBox.GetCurrentParent().Invoke(new Action<ulong, ulong>(UpdateTransferTitle), rx, tx);
                Thread.Sleep(1000);
            } catch { adapter = null; }
        }
    }
    private void UpdateTransferTitle(ulong rx, ulong tx)
    {
        var titleBase = _textBox.Text;
        var idx = titleBase.IndexOf(" - ", StringComparison.Ordinal);
        if (idx != -1)
            titleBase = titleBase.Substring(0, idx);
        if (rx == 0 && tx == 0)
            _textBox.Text = titleBase;
        else
            _textBox.Text = $@"{titleBase} - rx: {FormatBytes(rx)}, tx: {FormatBytes(tx)}";
    }
    private static string FormatBytes(ulong bytes)
    {
        decimal d = bytes;
        string selectedUnit = null;
        foreach (string unit in new[] { "B", "KiB", "MiB", "GiB", "TiB" })
        {
            selectedUnit = unit;
            if (d < 1024)
                break;
            d /= 1024;
        }
        return $"{d:0.##} {selectedUnit}";
    }
    private async Task<string> GenerateNewConfig()
    {
        var keys = Tunnel.Keypair.Generate();
        WireGuardPeerSetting wgPeerSetting = await GetNewWireGuardPeerData(Program.Servers.CurrentApiUrl,
            Program.Servers.CurrentApiKey, keys.Public, _user);
        return
$@"[Interface]
PrivateKey = {keys.Private}
Address = {wgPeerSetting.InternalIP}
DNS = {wgPeerSetting.DnsIP}

[Peer]
PublicKey = {wgPeerSetting.ServerPubKey}
Endpoint = {Program.Servers.GetCurrentIPAddress}:{wgPeerSetting.ServerPort}
AllowedIPs = 0.0.0.0/0
";
    }
    private static HttpClient CreateHttpClient(string apiKey)
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(300) };
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        return client;
    }
    private static readonly JavaScriptSerializer JSON = new();
    private async Task<WireGuardPeerSetting> GetNewWireGuardPeerData(string apiUrl, string apiKey, string publicKey, string user)
    {
        var client = CreateHttpClient(apiKey);
        try
        {
            var encodedKey = Uri.EscapeDataString(publicKey);
            var encodedUser = Uri.EscapeDataString(user);

            var request = new HttpRequestMessage(HttpMethod.Post, $"{apiUrl}/api/GetWireGuardKey?publicKey={encodedKey}&user={encodedUser}");
            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var deserialize = JSON.Deserialize<WireGuardPeerSetting>(json);
                if (CheckPeerSetting(deserialize))
                    return deserialize;
            }

            throw new InvalidOperationException($"Server status is {response.StatusCode}");
        } catch (Exception ex)
        {
            Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Ошибка загрузки данных WireGuard", ex);
            throw;
        } finally
        {
            client.Dispose();
        }
    }

    private bool CheckPeerSetting(WireGuardPeerSetting setting)
    {
        if (setting == null) return false;
        if (string.IsNullOrEmpty(setting.ServerPubKey)) return false;
        if (!IPAddress.TryParse(setting.DnsIP, out _)) return false;
        if (setting.ServerPort <= 0) return false;
        if (!setting.InternalIP.Contains("/")) return false;
        string[] parts = setting.InternalIP.Split('/');
        if (!IPAddress.TryParse(parts[0], out _)) return false;
        if (!int.TryParse(parts[1], out _)) return false;

        return true;
    }
}