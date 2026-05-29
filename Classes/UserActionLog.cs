using System;

namespace VaR;

public class UserActionLog
{
    public int? ContactId { get; set; }
    public int? TerminalServerId { get; set; }
    public string ActionType { get; set; }
    public string ActionDetails { get; set; } // JSON
    public string ClientLocalIp { get; set; }
    public string ClientPublicIp { get; set; }
    public Guid? ClientGuid { get; set; }
    public string AppVersion { get; set; }
    public DateTime ClientTimestamp { get; set; }
}