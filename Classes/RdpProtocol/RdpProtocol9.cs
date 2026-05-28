using AxMSTSCLib;
using MSTSCLib;
using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace VaR
{
    public class VaRRdpProtocol9 : VaRRdpProtocol8
    {
        private MsRdpClient9NotSafeForScripting RdpClient9 => (MsRdpClient9NotSafeForScripting)((AxHost)Control).GetOcx();

        protected override Enums.RdpVersion RdpProtocolVersion => Enums.RdpVersion.Rdc9;

        // Constructor not needed - ResizeEnd is already registered in RdpProtocol8 base class

        public override bool Initialize()
        {
            if (!base.Initialize())
                return false;
            if (RdpVersion < Versions.Rdc81) return false; // minimum dll version checked, loaded MSTSCLIB dll version is not capable

            return true; // minimum dll version checked, loaded MSTSCLIB dll version is not capable
        }
        //TODO онлайн изменение настроек, наверно нужно как то вызвать
        public void SyncSessionDisplaySettings()
        {
            try
            {
                RdpClient9.SyncSessionDisplaySettings();
            } catch (Exception ex)
            {
                LogsFile.Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Ошибка синхронизации настроек отображения", ex);
            }
        }

        protected override AxHost CreateActiveXRdpClientControl()
        {
            LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(), "Попытка создать AxHost");
            return new AxMsRdpClient9NotSafeForScripting();
        }
        protected override void SetSmartSizing(bool smartSizing)
        {
            try
            {
                if (RdpClient9 != null)
                {
                    RdpClient9.AdvancedSettings8.SmartSizing = smartSizing;
                    bool verify = RdpClient9.AdvancedSettings8.SmartSizing;
                    LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(), $"SmartSizing set to {smartSizing} via AdvancedSettings8, verified={verify}");
                }
                else
                {
                    LogsFile.Logs.Warn(GetType(), MethodBase.GetCurrentMethod(), "RdpClient9 is null, falling back to base");
                    base.SetSmartSizing(smartSizing);
                }
            } catch (Exception ex)
            {
                LogsFile.Logs.Error(GetType(), MethodBase.GetCurrentMethod(), $"SetSmartSizing (v9) failed: {ex.Message}", ex);
                base.SetSmartSizing(smartSizing);
            }
        }

        protected override void UpdateSessionDisplaySettings(uint width, uint height)
        {
            try
            {
                if (RdpClient9 != null)
                {
                    RdpClient9.UpdateSessionDisplaySettings(width, height, width, height, Orientation, DesktopScaleFactor, DeviceScaleFactor);
                }
                else
                {
                    base.UpdateSessionDisplaySettings(width, height);
                }
            } catch (Exception)
            {
                // target OS does not support newer method, fallback to an older method
                base.UpdateSessionDisplaySettings(width, height);
            }
        }

    }
}