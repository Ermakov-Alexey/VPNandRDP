using System.Threading.Tasks;
using ConnectLIbrary;
using DotRas;

namespace VaR
{
    public static class VpnManager
    {
        public static async Task IpSecConnectionCreateL2TpAsync(string ipSecConnectionName, Server server)
        {
            using var client = new IpSecClient();
            await Task.Run(() => client.IpSecConnectionCreate(ipSecConnectionName, server.PKey, server.HostOrIP, RasVpnStrategy.L2tpOnly));
        }

        public static async Task IpSecConnectionCreatePptpAsync(string ipSecConnectionName, Server server)
        {
            using var client = new IpSecClient();
            await Task.Run(() => client.IpSecConnectionCreate(ipSecConnectionName, "", server.HostOrIP, RasVpnStrategy.PptpOnly));
        }

        public static async Task<string> IpSecConnectionRemoveAsync(string ipSecConnectionName)
        {
            return await Task.Run(() =>
            {
                using var client = new IpSecClient();
                string s1 = client.IpSecConnectionRemoveAll(ipSecConnectionName);
                string s2 = client.IpSecConnectionRemoveCurrent(ipSecConnectionName);
                return s1 + s2;
            });
        }

        public static async Task IpSecConnectAsync(string ipSecConnectionName, string ipAddress, string username, string password)
        {
            using var client = new IpSecClient();
            await Task.Run(() => client.Connect(ipAddress, username, password, ipSecConnectionName));
        }
    }
}