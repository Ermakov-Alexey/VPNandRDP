using AxMSTSCLib;
using MSTSCLib;
using System;
using System.Reflection;
using System.Windows.Forms;

namespace VaR
{
    public class VaRRdpProtocol11 : VaRRdpProtocol10
    {
        private MsRdpClient11NotSafeForScripting RdpClient11 =>
            (MsRdpClient11NotSafeForScripting)((AxHost)Control).GetOcx();

        protected override Enums.RdpVersion RdpProtocolVersion => Enums.RdpVersion.Rdc11;

        protected override AxHost CreateActiveXRdpClientControl()
        {
            LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(), "Попытка создать AxHost");
            return new AxMsRdpClient11NotSafeForScripting();
        }

        public override bool Initialize()
        {
            if (!base.Initialize())
                return false;
            return RdpVersion >= Versions.Rdc100;
        }

        protected override void SetSmartSizing(bool smartSizing)
        {
            try
            {
                if (RdpClient11 != null)
                {
                    // Try AdvancedSettings9.SmartSizing first (v11 specific)
                    try
                    {
                        RdpClient11.AdvancedSettings9.SmartSizing = smartSizing;
                        bool verify = RdpClient11.AdvancedSettings9.SmartSizing;
                        LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(), $"SmartSizing set to {smartSizing} via AdvancedSettings9 (v11), verified={verify}");
                    }
                    catch (Exception ex)
                    {
                        LogsFile.Logs.Warn(GetType(), MethodBase.GetCurrentMethod(), $"AdvancedSettings9.SmartSizing not available: {ex.Message}, trying AdvancedSettings8.SmartSizing");
                        try
                        {
                            RdpClient11.AdvancedSettings8.SmartSizing = smartSizing;
                            bool verify = RdpClient11.AdvancedSettings8.SmartSizing;
                            LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(), $"SmartSizing set to {smartSizing} via AdvancedSettings8 (v11 fallback), verified={verify}");
                        }
                        catch (Exception ex2)
                        {
                            LogsFile.Logs.Warn(GetType(), MethodBase.GetCurrentMethod(), $"AdvancedSettings8.SmartSizing also failed: {ex2.Message}");
                            base.SetSmartSizing(smartSizing);
                        }
                    }
                }
                else
                {
                    LogsFile.Logs.Warn(GetType(), MethodBase.GetCurrentMethod(), "RdpClient11 is null, falling back to base");
                    base.SetSmartSizing(smartSizing);
                }
            }
            catch (Exception ex)
            {
                LogsFile.Logs.Error(GetType(), MethodBase.GetCurrentMethod(), $"SetSmartSizing (v11) failed: {ex.Message}", ex);
                base.SetSmartSizing(smartSizing);
            }
        }
                   
    }
}
