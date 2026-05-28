using AxMSTSCLib;
using DevExpress.Entity.ProjectModel;
using DevExpress.XtraEditors;
using MSTSCLib;
using System;
using System.Drawing;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace VaR
{
    public class VaRRdpProtocol
    {
        #region private value
        private MsRdpClient6NotSafeForScripting _rdpClient;
        private readonly DisplayProperties _displayProperties = new();
        //private AxHost AxHost => (AxHost)Control;
        #endregion

        #region protected value
        protected bool LoginComplete;
        protected virtual Enums.RdpVersion RdpProtocolVersion => Enums.RdpVersion.Rdc6;
        protected Version RdpVersion;
        protected Form FrmMain;
        protected Control ParentControl;
        protected readonly uint Orientation = 0;
        protected uint DesktopScaleFactor => (uint)(_displayProperties.ResolutionScalingFactor.Width * 100);
        protected readonly uint DeviceScaleFactor = 100;
        protected Control Control { get; set; }
        #endregion

        #region public value
        public ConnectionInfo ConnectInfo { get; set; }
        public ConnectionSetup ConnectSetup { get; set; }
        #endregion


        #region public methods
        public virtual bool Initialize()
        {
            try
            {
                if (ConnectInfo == null) return false;
                LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(), $"Initialize started for server '{ConnectSetup!.Hostname}', protocol version: {RdpProtocolVersion}");
                Control = CreateActiveXRdpClientControl();
                if (Control == null)
                    return false;
                LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(), $"Control created: {Control.GetType().Name}, ParentControl: {ParentControl?.GetType().Name}, ParentControl.Size: {ParentControl?.Size}");
                Control.Name = ConnectSetup!.Name;
                Control.Dock = DockStyle.Fill;
                if (ParentControl != null) ParentControl.Controls.Add(Control);
                LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(), $"Control added to parent, Control.Size after DockStyle.Fill: {Control.Size}");
                if (!InitializeActiveXControl()) return false;

                if (RdpVersion < Versions.Rdc61) return false;

                SetRdpClientProperties();
                return true;
            } catch (Exception ex)
            {
                LogsFile.Logs.Error(GetType(), MethodBase.GetCurrentMethod(), $"RDP Initialization failed: {ex.Message}", ex);
                return false;
            }
        }
        public virtual void Close()
        {
            try
            {
                if (_rdpClient != null)
                {
                    _rdpClient.OnConnecting -= RDPEvent_OnConnecting;
                    _rdpClient.OnConnected -= RDPEvent_OnConnected;
                    _rdpClient.OnLoginComplete -= RDPEvent_OnLoginComplete;
                    _rdpClient.OnFatalError -= RDPEvent_OnFatalError;
                    _rdpClient.OnDisconnected -= RDPEvent_OnDisconnected;
                    _rdpClient.OnIdleTimeoutNotification -= RDPEvent_OnIdleTimeoutNotification;

                    _rdpClient.OnLeaveFullScreenMode -= RdClient_OnLeaveFullScreenMode;
                    _rdpClient.OnEnterFullScreenMode -= RdClient_OnEnterFullScreenMode;
                    _rdpClient.OnAuthenticationWarningDismissed -= RdClient_OnAuthenticationWarningDismissed;

                    _rdpClient.OnNetworkStatusChanged -= RdpClient_OnNetworkStatusChanged;
                }
            } catch (Exception ex)
            {
                LogsFile.Logs.Error(GetType(), MethodBase.GetCurrentMethod(), $"RDP Close failed: {ex.Message}", ex);
            }
        }

        public bool Connect()
        {
            try
            {
                LoginComplete = false;
                _rdpClient.Connect();
                Control.Focus();
                return true;
            } catch (Exception ex)
            {
                LogsFile.Logs.Error(GetType(), MethodBase.GetCurrentMethod(), $"RDP Connection failed: {ex.Message}", ex);
                return false;
            }
        }
        public void Disconnect()
        {
            try
            {
                if (_rdpClient==null) return;
                if (_rdpClient.Connected == 1)
                    _rdpClient.Disconnect();
                while (_rdpClient.Connected != 0)
                {
                    Application.DoEvents();
                    Thread.Sleep(50);
                }
            } catch (Exception ex)
            {
                LogsFile.Logs.Error(GetType(), MethodBase.GetCurrentMethod(), $"RDP Disconnection failed: {ex.Message}", ex);
            }
        }
        public void ChangeMultimon(bool multimon)
        {
            if (Control != null)
            {
                if (ConnectInfo.FitToWindow)
                {
                    Size resolution = Screen.FromControl(FrmMain).Bounds.Size;
                    _rdpClient.DesktopWidth = resolution.Width;
                    _rdpClient.DesktopHeight = resolution.Height;
                    LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(), $"Desktop resolution set to {resolution.Width}x{resolution.Height}");
                }
                if (((AxHost)Control).GetOcx() is IMsRdpClientNonScriptable5 ocx5)
                    ocx5.UseMultimon = multimon;
            }
        }
        public void SetFormParent(Form parent)
        {
            FrmMain = parent;
        }
        public void SetParentControl(Control parent)
        {
            ParentControl = parent;
        }

        public bool SetFocusRdp()
        {
            return Control.Focus();
        }
        public Control GetParentControl() => ParentControl;
        public bool RdpVersionSupported()
        {
            try
            {
                using AxHost control = CreateActiveXRdpClientControl();
                new PanelControl().Controls.Add(control);
                control.CreateControl();
                LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(), $"Создан AxHost с типом {control.GetType()}");
                return true;
            } catch
            {
                return false;
            }
        }


        #endregion

        #region private method
        private bool InitializeActiveXControl()
        {
            try
            {
                Control.GotFocus += RdpClient_GotFocus;
                Control.CreateControl();

                while (!Control.Created)
                {
                    Thread.Sleep(50);
                    Application.DoEvents();
                }
                Control.Anchor = AnchorStyles.None;

                _rdpClient = (MsRdpClient6NotSafeForScripting)((AxHost)Control).GetOcx();
                RdpVersion = new Version(_rdpClient.Version);
                return true;
            } catch (COMException ex)
            {
                LogsFile.Logs.Error(GetType(), MethodBase.GetCurrentMethod(),
                    ex.Message.Contains("CLASS_E_CLASSNOTAVAILABLE")
                        ? $"RdpProtocolVersionNotSupported {RdpVersion}"
                        : $"RdpControlCreationFailed {ex.Message}", ex);
                Control.Dispose();
                return false;
            }
        }
        private void SetPerformanceFlags()
        {
            try
            {
                var ci = ConnectInfo;
                int pFlags = 0;
                if (!ci.DisplayThemes)
                    pFlags += (int)Enums.RdpPerformanceFlags.DisableThemes;
                if (!ci.DisplayWallpaper)
                    pFlags += (int)Enums.RdpPerformanceFlags.DisableWallpaper;
                if (ci.EnableFontSmoothing)
                    pFlags += (int)Enums.RdpPerformanceFlags.EnableFontSmoothing;
                if (ci.EnableDesktopComposition)
                    pFlags += (int)Enums.RdpPerformanceFlags.EnableDesktopComposition;
                if (ci.DisableFullWindowDrag)
                    pFlags += (int)Enums.RdpPerformanceFlags.DisableFullWindowDrag;
                if (ci.DisableMenuAnimations)
                    pFlags += (int)Enums.RdpPerformanceFlags.DisableMenuAnimations;
                if (ci.DisableCursorShadow)
                    pFlags += (int)Enums.RdpPerformanceFlags.DisableCursorShadow;
                if (ci.DisableCursorBlinking)
                    pFlags += (int)Enums.RdpPerformanceFlags.DisableCursorBlinking;

                _rdpClient.AdvancedSettings2.PerformanceFlags = pFlags;
            } catch (Exception ex)
            {
                LogsFile.Logs.Error(GetType(), MethodBase.GetCurrentMethod(), $"SetPerformanceFlags failed: {ex.Message}", ex);
            }
        }
        private void SetUseConsoleSession()
        {
            try
            {
                _rdpClient.AdvancedSettings7.ConnectToAdministerServer = ConnectInfo.UseConsoleSession;
            } catch (Exception ex)
            {
                LogsFile.Logs.Error(GetType(), MethodBase.GetCurrentMethod(), $"SetUseConsoleSession failed: {ex.Message}", ex);
            }
        }
        private void SetRedirection()
        {
            try
            {
                var ci = ConnectInfo;
                _rdpClient.AdvancedSettings2.RedirectDrives = ci.RedirectDrives;
                _rdpClient.AdvancedSettings2.RedirectPorts = ci.RedirectPorts;
                _rdpClient.AdvancedSettings2.RedirectPrinters = ci.RedirectPrinters;
                _rdpClient.AdvancedSettings2.DisableRdpdr = !ci.RedirectPrinters ? 1 : 0;
                _rdpClient.AdvancedSettings2.RedirectSmartCards = ci.RedirectSmartCards;
                _rdpClient.SecuredSettings2.AudioRedirectionMode = (int)ci.RedirectSound;
                _rdpClient.AdvancedSettings6.RedirectClipboard = ci.RedirectClipboard;
                _rdpClient.AdvancedSettings.DisableRdpdr = !ci.RedirectClipboard ? 1 : 0;

            } catch (Exception ex)
            {
                LogsFile.Logs.Error(GetType(), MethodBase.GetCurrentMethod(), $"SetRedirection failed: {ex.Message}", ex);
            }
        }
        private void SetAuthenticationLevel()
        {
            try
            {
                _rdpClient.AdvancedSettings5.AuthenticationLevel = (uint)ConnectInfo.RdpAuthenticationLevel;
            } catch (Exception ex)
            {
                LogsFile.Logs.Error(GetType(), MethodBase.GetCurrentMethod(), $"SetAuthenticationLevel failed: {ex.Message}", ex);
            }
        }
        private void SetLoadBalanceInfo()
        {
            try
            {
                if (!string.IsNullOrEmpty(ConnectInfo.LoadBalanceInfo))
                {
                    _rdpClient.AdvancedSettings2.LoadBalanceInfo = ConnectInfo.LoadBalanceInfo;
                }
            } catch (Exception ex)
            {
                LogsFile.Logs.Error(GetType(), MethodBase.GetCurrentMethod(), $"SetLoadBalanceInfo failed: {ex.Message}", ex);
            }
        }
        private void SetRedirectKeys()
        {
            try
            {
                if (!ConnectInfo.RedirectKeys) return;
                _rdpClient.SecuredSettings2.KeyboardHookMode = (int)ConnectInfo.KeyboardHookMode;
                // (int)Enums.RdpKeyboardHookMode.ApplyAtTheRemoteServer;
            } catch (Exception ex)
            {
                LogsFile.Logs.Error(GetType(), MethodBase.GetCurrentMethod(), $"SetRedirectKeys failed: {ex.Message}", ex);
            }
        }
        private void SetEventHandlers()
        {
            try
            {
                _rdpClient.OnConnecting += RDPEvent_OnConnecting;
                _rdpClient.OnConnected += RDPEvent_OnConnected;
                _rdpClient.OnLoginComplete += RDPEvent_OnLoginComplete;
                _rdpClient.OnFatalError += RDPEvent_OnFatalError;
                _rdpClient.OnDisconnected += RDPEvent_OnDisconnected;
                _rdpClient.OnIdleTimeoutNotification += RDPEvent_OnIdleTimeoutNotification;


                _rdpClient.OnLeaveFullScreenMode += RdClient_OnLeaveFullScreenMode;
                _rdpClient.OnEnterFullScreenMode += RdClient_OnEnterFullScreenMode;
                _rdpClient.OnAuthenticationWarningDismissed += RdClient_OnAuthenticationWarningDismissed;

                _rdpClient.OnNetworkStatusChanged += RdpClient_OnNetworkStatusChanged;
            } catch (Exception ex)
            {
                LogsFile.Logs.Error(GetType(), MethodBase.GetCurrentMethod(), $"SetEventHandlers failed: {ex.Message}", ex);
            }
        }

      







        #endregion

        #region protected method
        protected virtual AxHost CreateActiveXRdpClientControl()
        {
            LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(), "Попытка создать AxHost");
            return new AxMsRdpClient6NotSafeForScripting();
        }
        protected virtual void Resize(object sender, EventArgs e) { }
        protected virtual void ResizeEnd(object sender, EventArgs e) { }
        protected virtual void SetRdpClientProperties()
        {
            try
            {
                if (ConnectInfo == null)
                {
                    LogsFile.Logs.Warn(GetType(), MethodBase.GetCurrentMethod(), "ConnectionInfo is null");
                    return;
                }
                if (ConnectSetup == null)
                {
                    LogsFile.Logs.Warn(GetType(), MethodBase.GetCurrentMethod(), "ConnectionSetup is null");
                    return;
                }

                // Server and credentials
                _rdpClient.Server = ConnectSetup.Hostname;
                _rdpClient.UserName = ConnectSetup.Username;
                _rdpClient.Domain = ConnectSetup.Domain;
                _rdpClient.AdvancedSettings2.ClearTextPassword = ConnectSetup.Password;
                _rdpClient.AdvancedSettings2.RDPPort = ConnectSetup.Port;

                // Connection properties
                _rdpClient.FullScreenTitle = "VaR RDP Connection";

                // Idle timeout
                _rdpClient.AdvancedSettings2.MinutesToIdleTimeout = ConnectInfo.RdpMinutesToIdleTimeout;

                // Remote desktop services
                _rdpClient.SecuredSettings2.StartProgram = string.Empty;
                _rdpClient.SecuredSettings2.WorkDir = string.Empty;

                // Performance flags
                SetPerformanceFlags();

                // Other properties
                _rdpClient.AdvancedSettings2.GrabFocusOnConnect = ConnectInfo.GrabFocusOnConnect;
                _rdpClient.AdvancedSettings3.EnableAutoReconnect = ConnectInfo.EnableAutoReconnect;
                _rdpClient.AdvancedSettings2.keepAliveInterval = ConnectInfo.KeepAliveInterval;
                _rdpClient.AdvancedSettings5.AuthenticationLevel = (uint)ConnectInfo.RdpAuthenticationLevel;
                _rdpClient.AdvancedSettings2.EncryptionEnabled = 1;//Encryption cannot be disabled.
                _rdpClient.AdvancedSettings2.BitmapPersistence = (int)ConnectInfo.CacheBitmaps;
                _rdpClient.AdvancedSettings7.EnableCredSspSupport = ConnectInfo.EnableCredSspSupport;
                _rdpClient.AdvancedSettings2.DisplayConnectionBar = ConnectInfo.DisplayConnectionBar;
                _rdpClient.AdvancedSettings4.ConnectionBarShowMinimizeButton = ConnectInfo.ConnectionBarShowMinimizeButton;
                _rdpClient.AdvancedSettings7.RelativeMouseMode = ConnectInfo.RelativeMouseMode;
                _rdpClient.AdvancedSettings.Compress = (int)ConnectInfo.Compress;

                // Дополнительные настройки безопасности
                if (((AxHost)Control).GetOcx() is IMsRdpClientNonScriptable4 ocx4)
                {
                    ocx4.AllowCredentialSaving = ConnectInfo.AllowCredentialSaving; // Не сохранять учетные данные
                    ocx4.PromptForCredentials = ConnectInfo.PromptForCredentials; // Не запрашивать учетные данные
                    ocx4.PromptForCredsOnClient = ConnectInfo.PromptForCredsOnClient; // Не запрашивать учетные данные на клиенте
                }

                // Если поддерживается
                if (((AxHost)Control).GetOcx() is IMsRdpClientNonScriptable5 ocx5)
                {
                    ocx5.AllowPromptingForCredentials = ConnectInfo.AllowPromptingForCredentials; // Не показывать окно ввода пароля
                    ocx5.UseMultimon = ConnectInfo.Multimon;
                }

                // Console session
                SetUseConsoleSession();

                // Redirection
                SetRedirection();

                // Authentication level
                SetAuthenticationLevel();

                // Load balance info
                SetLoadBalanceInfo();

                // Color depth
                _rdpClient.ColorDepth = (int)ConnectInfo.Colors;

                // Smart sizing
                ConnectInfo.SmartSize = ConnectInfo.FitToWindow;
                LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(), $"SmartSize calculated: {ConnectInfo.SmartSize} (FitToWindow={ConnectInfo.FitToWindow})");

                // Desktop resolution
                if (ConnectInfo.FitToWindow)
                {
                    Size resolution = Screen.FromControl(FrmMain).Bounds.Size;
                    _rdpClient.DesktopWidth = resolution.Width;
                    _rdpClient.DesktopHeight = resolution.Height;
                    LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(), $"Desktop resolution set to {resolution.Width}x{resolution.Height}");
                }

                // SmartSizing after DesktopWidth/Height
                SetSmartSizing(ConnectInfo.SmartSize);
                LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(), $"SmartSizing set to {ConnectInfo.SmartSize}");

                // Fullscreen mode
                _rdpClient.FullScreen = false;

                // Redirect keys
                SetRedirectKeys();

                // Event handlers
                SetEventHandlers();
            } catch (Exception ex)
            {
                LogsFile.Logs.Error(GetType(), MethodBase.GetCurrentMethod(), $"SetRdpClientProperties failed: {ex.Message}", ex);
            }
        }
        protected virtual void SetSmartSizing(bool smartSizing)
        {
            try
            {
                _rdpClient.AdvancedSettings2.SmartSizing = smartSizing;
                LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(), $"SmartSizing set to {smartSizing} via AdvancedSettings2");
            } catch (Exception ex)
            {
                LogsFile.Logs.Error(GetType(), MethodBase.GetCurrentMethod(), $"SetSmartSizing failed: {ex.Message}", ex);
            }
        }
        #endregion


        #region Events
        private void RDPEvent_OnFatalError(int errorCode)
        {
            LogsFile.Logs.Warn(GetType(), MethodBase.GetCurrentMethod(), $"RDP Fatal Error: {errorCode}");
        }

        private void RDPEvent_OnDisconnected(int discReason)
        {
            LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(), $"RDP Disconnected: {discReason}");
            if (_rdpClient != null)
            {
                var errorString = _rdpClient.GetErrorDescription((uint)discReason, (uint)_rdpClient.ExtendedDisconnectReason);
                LogsFile.Logs.Warn(GetType(), MethodBase.GetCurrentMethod(), "Ошибка RdClient_OnDisconnected: " + errorString);
            }
            if (FrmMain is ConnectionForm conForm)
            {
                conForm.SetOnDisconnected(ConnectSetup.Hostname, ParentControl);
            }
            else if (FrmMain is FullScreenForm fullForm)
            {
                fullForm.SetOnDisconnected(ConnectSetup.Hostname);
            }
        }

        private void RDPEvent_OnConnecting()
        {
            LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(), "RDP Connecting...");
        }

        private void RDPEvent_OnConnected()
        {
            LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(), "RDP Connected");
            if (FrmMain is ConnectionForm conForm)
            {
                conForm.SetRdpConnected(ConnectSetup.Hostname);
            }
            else if (FrmMain is FullScreenForm fullForm)
            {
                fullForm.SetRdpConnected();
            }
        }

        private void RDPEvent_OnLoginComplete()
        {
            LoginComplete = true;
            LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(), "RDP LoginComplete");
        }

        private void RDPEvent_OnIdleTimeoutNotification()
        {
            Close();
            if (ConnectInfo.RdpAlertIdleTimeout)
            {
                LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(), "The session was disconnected due to inactivity");
            }
        }

        private void RdClient_OnAuthenticationWarningDismissed()
        {
            LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(), "RDP AuthenticationWarningDismissed");
            if (FrmMain is ConnectionForm conForm)
            {
                conForm.SetOnAuthenticationWarningDismissed(ConnectSetup.Hostname);
            }
            else if (FrmMain is FullScreenForm fullForm)
            {
                fullForm.SetOnAuthenticationWarningDismissed();
            }
        }

        private void RdClient_OnEnterFullScreenMode()
        {
            _rdpClient.FullScreen = false;
            LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(), "RDP EnterFullScreenMode");
        }

        private void RdClient_OnLeaveFullScreenMode()
        {
            LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(), "RDP LeaveFullScreenMode");
        }

        private void RdpClient_GotFocus(object sender, EventArgs e)
        {
            LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(), "RDP GotFocus");
        }
        /// <summary>
        /// qualityLevel=1 Менее 512 килобайт в секунду (КБ/с).
        /// qualityLevel=2 От 512 до 1999 Кбит/с.
        /// qualityLevel=3 От 2000 до 9999 Кбит/с.
        /// qualityLevel=4 Не менее 10 000 Кбит/с.
        /// </summary>
        /// <param name="qualityLevel"></param>
        /// <param name="bandwidth"></param>
        /// <param name="rtt"></param>
        private void RdpClient_OnNetworkStatusChanged(uint qualityLevel, int bandwidth, int rtt)
        {
            //TODO надо придумать вывод этих данных
            LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(), $"RDP OnNetworkStatusChanged quality {qualityLevel}");
            LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(), $"RDP OnNetworkStatusChanged bandwidth {bandwidth} Кбит/с");
            LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(), $"RDP OnNetworkStatusChanged Ping rtt {rtt}");
        }

        #endregion
    }
}
