using System.Collections.Generic;

namespace VaR;

public class ConnectRdpComparer : IEqualityComparer<ConnectRdp>
{
    public bool Equals(ConnectRdp x, ConnectRdp y)
    {
        if (ReferenceEquals(x, y))
            return true;

        return x != null &&
               y != null &&
               x.IPRDP.Equals(y.IPRDP) &&
               x.DomainRDP.Equals(y.DomainRDP) &&
               x.LoginRDP.Equals(y.LoginRDP) &&
               x.PasswordRDP.Equals(y.PasswordRDP) &&
               x.IsAliveString.Equals(y.IsAliveString) &&
               x.IsAlive.Equals(y.IsAlive);
    }
    public int GetHashCode(ConnectRdp obj)
    {
        int hashID = obj.IPRDP == null ? 0 : obj.IPRDP.GetHashCode();
        int hashName = obj.DomainRDP == null ? 0 : obj.DomainRDP.GetHashCode();
        int hashEmail = obj.LoginRDP == null ? 0 : obj.LoginRDP.GetHashCode();
        int hashRestoranID = obj.PasswordRDP == null ? 0 : obj.PasswordRDP.GetHashCode();
        int hashDel = obj.IsAlive.GetHashCode();
        int hashIsAliveString = obj.IsAliveString == null ? 0 : obj.IsAliveString.GetHashCode();

        return hashID ^
               hashName ^
               hashEmail ^
               hashRestoranID ^
               hashIsAliveString ^
               hashDel;
    }
}