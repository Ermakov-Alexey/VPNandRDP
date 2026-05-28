using System;
using System.Linq;
using System.Reflection;
using DotRas;
using LogsFile;

namespace VaR;

public class IpSecClient : IDisposable
{
    private static readonly string PhoneBookPathAll;
    private static readonly string PhoneBookPathCurrent;
    private RasPhoneBook _rasPhoneBook;
    private RasDialer _rasDialer;

    static IpSecClient()
    {
        PhoneBookPathAll = RasPhoneBook.GetPhoneBookPath(RasPhoneBookType.AllUsers);
        PhoneBookPathCurrent = RasPhoneBook.GetPhoneBookPath(RasPhoneBookType.User);
    }
    #region Public methods
    public void IpSecConnectionCreate(string ipSecConnectionName, string presharedKey, string ipAddress, RasVpnStrategy strategy)
    {
        using RasPhoneBook rasPhoneBook = new RasPhoneBook();
        rasPhoneBook.Open(PhoneBookPathAll);

        RasEntry ipSecRasEntry =
            RasEntry.CreateVpnEntry(
                ipSecConnectionName,
                ipAddress,
                strategy,
                RasDevice.Create(ipSecConnectionName, RasDeviceType.Vpn)
            );

        ipSecRasEntry.EncryptionType = RasEncryptionType.Optional;
        ipSecRasEntry.EntryType = RasEntryType.Vpn;
        ipSecRasEntry.Options.RequireDataEncryption = true;
        if (strategy == RasVpnStrategy.L2tpOnly)
            ipSecRasEntry.Options.UsePreSharedKey = true; // used only for IPSec - L2TP/IPsec VPN
        ipSecRasEntry.Options.UseLogOnCredentials = false;
        ipSecRasEntry.Options.RequireChap = false;
        ipSecRasEntry.Options.RequireMSChap2 = true;
        ipSecRasEntry.Options.SecureFileAndPrint = true;
        ipSecRasEntry.Options.SecureClientForMSNet = true;
        ipSecRasEntry.Options.ReconnectIfDropped = false;

        rasPhoneBook.Entries.Add(ipSecRasEntry);
        if (strategy == RasVpnStrategy.L2tpOnly)
            ipSecRasEntry.UpdateCredentials(RasPreSharedKey.Client, presharedKey);
    }
    public string IpSecConnectionRemoveAll(string ipSecConnectionName)
    {
        using RasPhoneBook rasPhoneBook = new RasPhoneBook();
        rasPhoneBook.Open(PhoneBookPathAll);

        Disconnect(ipSecConnectionName);
        return RasEntryDelete(ipSecConnectionName, rasPhoneBook);
    }
    public string IpSecConnectionRemoveCurrent(string ipSecConnectionName)
    {
        using RasPhoneBook rasPhoneBook = new RasPhoneBook();
        rasPhoneBook.Open(PhoneBookPathCurrent);

        Disconnect(ipSecConnectionName);
        return RasEntryDelete(ipSecConnectionName, rasPhoneBook);
    }
    public RasHandle Connect(string vpnEndpoint, string username, string password, string ipSecConnectionName)
    {
        VpnConnectionBind();
        _rasDialer.PhoneNumber = vpnEndpoint;
        _rasDialer.EntryName = ipSecConnectionName;
        _rasDialer.PhoneBookPath = PhoneBookPathAll;
        _rasDialer.Credentials = new System.Net.NetworkCredential(username, password);

        try
        {
            RasHandle handle = _rasDialer.Dial();

            // DotRas выбрасывает исключение при ошибке. Если мы здесь — Dial отработал.
            // Финальная проверка: убедимся, что соединение действительно висит в активных
            var active = RasConnection.GetActiveConnections();
            if (active.All(c => c.EntryName != ipSecConnectionName))
                throw new InvalidOperationException($"Соединение '{ipSecConnectionName}' не найдено в активных после Dial().");

            Logs.Info(GetType(), MethodBase.GetCurrentMethod(), $"Успешно подключено: {ipSecConnectionName}");
            return handle;
        } catch (RasException ex) // или Exception, если версия DotRas старая
        {
            Logs.Error(GetType(), MethodBase.GetCurrentMethod(), $"Ошибка подключения {ipSecConnectionName}", ex);
            throw;
        }
    }
    #endregion

    #region Private methods
    private string RasEntryDelete(string ipSecConnectionName, RasPhoneBook rasPhoneBook)
    {
        RasEntry ipSecRasEntry = RasEntryFindByName(rasPhoneBook, ipSecConnectionName);
        if (ipSecRasEntry == null)
            return $"VPN connection {ipSecConnectionName} not found.";
        if (ipSecRasEntry.Remove())
            return $"VPN connection {ipSecConnectionName} removed successfully.";
        return $"RasEntry.Remove() {ipSecConnectionName} failed.";
    }
    private void Disconnect(string ipSecConnectionName)
    {
        RasConnection rasConnection = IpSecActiveConnectionGet(ipSecConnectionName);
        if (rasConnection != null)
        {
            Logs.Info(GetType(), MethodBase.GetCurrentMethod(), $@"Hanging up the connection {rasConnection.EntryName}");
            rasConnection.HangUp();
        }
    }
    private void VpnConnectionBind()
    {
        _rasDialer = new RasDialer();
        _rasDialer.EapOptions = new RasEapOptions(false, false, false);
        _rasDialer.HangUpPollingInterval = 0;
        _rasDialer.Options = new RasDialOptions(false, false, false, false, false, false, false, false, false, false, false);
    }
    private RasConnection IpSecActiveConnectionGet(string vpnConnectionName)
    {
        return RasConnection.GetActiveConnections().FirstOrDefault(x => x.EntryName == vpnConnectionName);
    }
    private static RasEntry RasEntryFindByName(RasPhoneBook rasPhoneBook, string rasEntryName)
    {
        return rasPhoneBook.Entries.FirstOrDefault(entry => entry.Name == rasEntryName);
    }
    #endregion

    #region IDisposable Support
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_rasPhoneBook != null)
            {
                _rasPhoneBook.Dispose();
                _rasPhoneBook = null;
            }
            if (_rasDialer != null)
            {
                _rasDialer.Dispose();
                _rasDialer = null;
            }
        }
    }
    public void Dispose()
    {
        Dispose(true);
    }
    #endregion
}