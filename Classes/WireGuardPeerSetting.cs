namespace VaR;

public class WireGuardPeerSetting
{
    public string ServerPubKey { get; set; }
    public int ServerPort { get; set; }
    public string InternalIP { get; set; }
    public string DnsIP { get; set; }
}