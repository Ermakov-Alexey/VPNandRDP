using AxMSTSCLib;
using MSTSCLib;
using System;
using System.Net.Sockets;
using System.Reflection;
using System.Windows.Forms;

namespace VaR
{
    public class VaRRdpProtocol7 : VaRRdpProtocol
    {
        private MsRdpClient7NotSafeForScripting RdpClient7 => (MsRdpClient7NotSafeForScripting)((AxHost)Control).GetOcx();
        protected override Enums.RdpVersion RdpProtocolVersion => Enums.RdpVersion.Rdc7;

        public override bool Initialize()
        {
            if (!base.Initialize())
                return false;

            try
            {
                if (RdpVersion < Versions.Rdc70) return false;
                if (ConnectInfo == null) return false;
                RdpClient7.AdvancedSettings8.AudioQualityMode = (uint)ConnectInfo.RdpSoundQuality;
                RdpClient7.AdvancedSettings8.AudioCaptureRedirectionMode = ConnectInfo.AudioCaptureRedirectionMode;
                RdpClient7.AdvancedSettings8.NetworkConnectionType = (uint)ConnectInfo.RdpNetworkConnectionType;
                RdpClient7.AdvancedSettings8.NegotiateSecurityLayer = ConnectInfo.NegotiateSecurityLayer;
            } catch (Exception ex)
            {
                LogsFile.Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "RdpSetPropsFailed", ex);
                return false;
            }

            return true;
        }

        protected override AxHost CreateActiveXRdpClientControl()
        {
            LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(), "Попытка создать AxHost");
            return new AxMsRdpClient11NotSafeForScripting();
        }
    }
}
