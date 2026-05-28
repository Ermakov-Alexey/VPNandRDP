using AxMSTSCLib;
using DevExpress.Drawing.Internal.Fonts.Interop;
using Microsoft.Win32;
using MSTSCLib;
using System;
using System.Drawing;
using System.Net.Sockets;
using System.Reflection;
using System.Windows.Forms;

namespace VaR
{
    /* RDP v8 requires Windows 7 with:
   * https://support.microsoft.com/en-us/kb/2592687
   * OR
   * https://support.microsoft.com/en-us/kb/2923545
   *
   * Windows 8+ support RDP v8 out of the box.
   */
    public class VaRRdpProtocol8 : VaRRdpProtocol7
    {
        private MsRdpClient8NotSafeForScripting RdpClient8 => (MsRdpClient8NotSafeForScripting)((AxHost)Control).GetOcx();

        protected override Enums.RdpVersion RdpProtocolVersion => Enums.RdpVersion.Rdc8;
        protected FormWindowState LastWindowState = FormWindowState.Minimized;

        // Debounce timer to reduce flickering during resize
        private System.Timers.Timer _resizeDebounceTimer;
        private Size _pendingResizeSize;
        private bool _hasPendingResize;

        public VaRRdpProtocol8()
        {
            // Initialize debounce timer (300ms delay).
            // Keep this in the constructor because it doesn't root the instance in any
            // external static object – it's safe for the temporary probing instances
            // created by RdpProtocolFactory.
            _resizeDebounceTimer = new System.Timers.Timer(300);
            _resizeDebounceTimer.AutoReset = false;
            _resizeDebounceTimer.Elapsed += ResizeDebounceTimer_Elapsed;
        }

        public override bool Initialize()
        {
            if (!base.Initialize())
                return false;

            if (RdpVersion < Versions.Rdc81) return false; // minimum dll version checked, loaded MSTSCLIB dll version is not capable

            // Subscribe to static/external events here (not in the constructor) so that
            // temporary probing instances created by RdpProtocolFactory.RdpVersionSupported()
            // are not rooted and do not accumulate memory leaks or spurious callbacks.
            ParentControl.SizeChanged += ResizeEnd;
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
            
            return true;
        }
        protected override void Resize(object sender, EventArgs e)
        {
            if (FrmMain == null) return;

            // Skip resize entirely when minimized or minimizing
            if (FrmMain.WindowState == FormWindowState.Minimized) return;

            // Only resize RDP session on window state changes (Maximize/Restore)
            // Manual drag-resizing will be handled by ResizeEnd()
            if (LastWindowState != FrmMain.WindowState)
            {
                LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(),
                    $"Resize() - Window state changed from {LastWindowState} to {FrmMain.WindowState}, calling DoResizeClient()");
                LastWindowState = FrmMain.WindowState;
                DoResizeClient();
            }
            else
            {
                LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(),
                    $"Resize() - Window state unchanged ({FrmMain.WindowState}), deferring to ResizeEnd()");
            }
        }

        protected override void ResizeEnd(object sender, EventArgs e)
        {
            if (FrmMain == null) return;

            // Skip resize when minimized
            if (FrmMain.WindowState == FormWindowState.Minimized) return;

            LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(),
                $"ResizeEnd() called - WindowState={FrmMain.WindowState}");

            // Update window state tracking
            LastWindowState = FrmMain.WindowState;

            // Debounce the RDP session resize to reduce flickering
            ScheduleDebouncedResize();
        }

        private void ScheduleDebouncedResize()
        {
            if (ParentControl == null) return;

            // Store the pending size
            _pendingResizeSize = ParentControl.Size;
            _hasPendingResize = true;

            // Reset the timer (this delays the resize if called repeatedly)
            _resizeDebounceTimer?.Stop();
            _resizeDebounceTimer?.Start();

            LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(),
                $"Resize debounced - will resize to {_pendingResizeSize.Width}x{_pendingResizeSize.Height} after 300ms");
        }

        private void ResizeDebounceTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!_hasPendingResize) return;

            // Check if controls are still valid (not disposed during shutdown)
            if (Control == null || Control.IsDisposed || ParentControl == null || ParentControl.IsDisposed)
            {
                _hasPendingResize = false;
                return;
            }

            // Guard against the window handle not yet being created or already destroyed
            if (!ParentControl.IsHandleCreated)
            {
                _hasPendingResize = false;
                return;
            }

            _hasPendingResize = false;

            LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(),
                $"Debounce timer fired - executing delayed resize to {_pendingResizeSize.Width}x{_pendingResizeSize.Height}");

            // Marshal to the UI thread because DoResizeClient() accesses WinForms and COM objects.
            // Wrap in try/catch: even after the guards above, there is a disposal race between
            // this timer thread and the UI thread that can cause ObjectDisposedException or
            // InvalidOperationException from BeginInvoke.
            try
            {
                if (ParentControl.InvokeRequired)
                {
                    ParentControl.BeginInvoke(new Action(DoResizeClient));
                }
                else
                {
                    DoResizeClient();
                }
            } catch (ObjectDisposedException ex)
            {
                LogsFile.Logs.Error(GetType(), MethodBase.GetCurrentMethod(),
                    $"ResizeDebounceTimer_Elapsed: control disposed during BeginInvoke ({ex.GetType().Name})", ex);
            } catch (InvalidOperationException ex)
            {
                LogsFile.Logs.Error(GetType(), MethodBase.GetCurrentMethod(),
                    $"ResizeDebounceTimer_Elapsed: control handle unavailable during BeginInvoke ({ex.GetType().Name})", ex);
            }
        }

        private void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            // When display settings change (e.g., outer RDP session reconnects with a different
            // resolution/viewport), schedule a debounced resize so the inner RDP session is
            // updated to match the new panel dimensions once the display has settled.
            // SystemEvents.DisplaySettingsChanged can fire on a non-UI thread, so marshal
            // ScheduleDebouncedResize() back to the UI thread before touching UI state.
            if (!LoginComplete) return;
            if (ParentControl == null || ParentControl.IsDisposed) return;

            if (ParentControl.InvokeRequired)
            {
                ParentControl.BeginInvoke(new Action(ScheduleDebouncedResize));
            }
            else
            {
                ScheduleDebouncedResize();
            }
        }

        protected override AxHost CreateActiveXRdpClientControl()
        {
            LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(), "Попытка создать AxHost");
            return new AxMsRdpClient8NotSafeForScripting();
        }

        private void DoResizeClient()
        {
            if (!LoginComplete)
            {
                LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(),
                    $"Resize skipped for '{ConnectSetup.Hostname}': Login not complete");
                return;
            }

            LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(),
                $"DoResizeClient called - Control.Size={Control?.Size}, ParentControl.Size={ParentControl?.Size}, FitToWindow={ConnectInfo.FitToWindow}, SmartSize={ConnectInfo.SmartSize}");

            if (ConnectInfo.FitToWindow)
            {
                // When FitToWindow is enabled, manually resize the control to fill parent
                // because DockStyle.Fill might not work correctly for ActiveX controls
                if (Control != null)
                {
                    Control.Location = new Point(0, 0);
                    if (ParentControl != null) Control.Size = ParentControl.Size;
                    LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(),
                        $"FitToWindow: Control.Location set to (0,0) but Control.Location={Control.Location}, Control.Size set to {Control.Size}");
                }

                return;
            }
            // FitToWindow: fixed resolution set at connect time, scrollbars handle overflow.
            // SmartSize: SmartSizing scales the image client-side, no session resize needed.
            // Only Fullscreen benefits from dynamically changing the remote session resolution.
            if (ConnectInfo.SmartSize)
            {
                LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(),
                     $"Resize skipped for '{ConnectSetup.Hostname}': SmartSize is enabled, no session resize needed");
                return;
            }

            LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(),
                $"Resizing RDP connection to host '{ConnectSetup.Hostname}'");

            try
            {
                // Use parentControl.Size instead of Control.Size because Control may be docked
                // and not reflect the actual available space
                if (Control != null)
                {
                    if (ParentControl != null)
                    {
                        Size size = ParentControl.Size;

                        LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(),
                            $"Calling UpdateSessionDisplaySettings({size.Width}, {size.Height}) for '{ConnectSetup.Hostname}' (Control.Size={Control.Size}, parentControl.Size={ParentControl.Size})");
                        Control.Dock = DockStyle.Fill;
                        UpdateSessionDisplaySettings((uint)size.Width - 4, (uint)size.Height - 4);

                        LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(),
                            $"Successfully resized RDP session for '{ConnectSetup.Hostname}' to {size.Width}x{size.Height}");
                    }
                }
            } catch (Exception ex)
            {
                LogsFile.Logs.Error(GetType(), MethodBase.GetCurrentMethod(),
                    $"ChangeConnectionResolutionError {ConnectSetup.Hostname}",
                    ex);
            }
        }


        protected virtual void UpdateSessionDisplaySettings(uint width, uint height)
        {
            if (RdpClient8 != null)
                RdpClient8.Reconnect(width, height);
        }

        public override void Close()
        {
            // Unsubscribe from external/static events to prevent memory leaks
            ParentControl.SizeChanged -= ResizeEnd;
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;

            // Clean up debounce timer
            if (_resizeDebounceTimer != null)
            {
                _resizeDebounceTimer.Stop();
                _resizeDebounceTimer.Elapsed -= ResizeDebounceTimer_Elapsed;
                _resizeDebounceTimer.Dispose();
                _resizeDebounceTimer = null;
            }

            
            base.Close();
        }

    }
}
