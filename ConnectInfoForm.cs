using DevExpress.Utils.Menu;
using DevExpress.XtraEditors;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VaR
{
    public partial class ConnectInfoForm : XtraForm
    {
        public ConnectInfoForm()
        {
            InitializeComponent();
        }

        private void ConnectInfoForm_Load(object sender, EventArgs e)
        {

        }

        private void ConnectInfoForm_Shown(object sender, EventArgs e)
        {
            FileInfo fileConnect = new FileInfo(Path.Combine(Program.AppPath, "connect.json"));
            var connectionInfo = fileConnect.Exists ? JsonConvert.DeserializeObject<ConnectionInfo>(File.ReadAllText(fileConnect.FullName)) : new ConnectionInfo();

            SetRdpColors(connectionInfo.Colors);
            SetRdpSounds(connectionInfo.RedirectSound);
            SetRdpKeyboardHookMode(connectionInfo.KeyboardHookMode);
            SetCacheBitmap(connectionInfo.CacheBitmaps);
            SetRdpAuthenticationLevel(connectionInfo.RdpAuthenticationLevel);
            SetRdpNetworkConnectionType(connectionInfo.RdpNetworkConnectionType);
            SetRdpSoundQuality(connectionInfo.RdpSoundQuality);
            SetRdpCompress(connectionInfo.Compress);

            SetBoolValue(connectionInfo);
        }

        private void okSimpleButton_Click(object sender, EventArgs e)
        {
            ConnectionInfo connectionInfo = new ConnectionInfo
            {
                Colors = GetRdpColors(),
                RedirectSound = GetRdpSounds(),
                KeyboardHookMode = GetRdpKeyboardHookMode(),
                CacheBitmaps = GetCacheBitmap(),
                RdpAuthenticationLevel = GetRdpAuthenticationLevel(),
                RdpNetworkConnectionType = GetRdpNetworkConnectionType(),
                RdpSoundQuality = GetRdpSoundQuality(),
                Compress = GetRdpCompress(),

            };
            GetBoolValue(connectionInfo);

            FileInfo fileConnect = new FileInfo(Path.Combine(Program.AppPath, "connect.json"));
            File.WriteAllText(fileConnect.FullName, JsonConvert.SerializeObject(connectionInfo,Formatting.Indented));
            Close();
        }

        private void cancelSimpleButton_Click(object sender, EventArgs e)
        {
            Close();
        }
        private void reset_simpleButton1_Click(object sender, EventArgs e)
        {
            var connectionInfo = new ConnectionInfo();

            SetRdpColors(connectionInfo.Colors);
            SetRdpSounds(connectionInfo.RedirectSound);
            SetRdpKeyboardHookMode(connectionInfo.KeyboardHookMode);
            SetCacheBitmap(connectionInfo.CacheBitmaps);
            SetRdpAuthenticationLevel(connectionInfo.RdpAuthenticationLevel);
            SetRdpNetworkConnectionType(connectionInfo.RdpNetworkConnectionType);
            SetRdpSoundQuality(connectionInfo.RdpSoundQuality);
            SetRdpCompress(connectionInfo.Compress);

            SetBoolValue(connectionInfo);
        }

        #region RdpColors
        private void SetRdpColors(Enums.RdpColors color)
        {
            DXPopupMenu menu = new DXPopupMenu();
            IEnumerable<Enums.RdpColors> rdpColors = Enum.GetValues(typeof(Enums.RdpColors))
                .OfType<Enums.RdpColors>();
            foreach (var rdpColor in rdpColors)
                menu.Items.Add(new DXMenuItem(rdpColor.ToString(), RdpColorsItem_Click));
            RdpColors_dropDownButton1.DropDownControl = menu;

            RdpColors_dropDownButton1.Text = color.ToString();
            RdpColors_dropDownButton1.Click += (_, _) =>
            {
                RdpColors_dropDownButton1.ShowDropDown();
            };
        }

        private Enums.RdpColors GetRdpColors()
        {
            return Enum.TryParse<Enums.RdpColors>(RdpColors_dropDownButton1.Text, out var result) ? result : Enums.RdpColors.Colors16Bit;
        }
        private void RdpColorsItem_Click(object sender, EventArgs e)
        {
            RdpColors_dropDownButton1.Text = ((DXMenuItem)sender).Caption;
        }
        #endregion
        #region RdpSounds
        private void SetRdpSounds(Enums.RdpSounds sound)
        {
            DXPopupMenu menu = new DXPopupMenu();
            IEnumerable<Enums.RdpSounds> rdpSounds = Enum.GetValues(typeof(Enums.RdpSounds))
                .OfType<Enums.RdpSounds>();
            foreach (var rdpSound in rdpSounds)
                menu.Items.Add(new DXMenuItem(rdpSound.ToString(), RdpSoundsItem_Click));
            RdpSounds_dropDownButton1.DropDownControl = menu;

            RdpSounds_dropDownButton1.Text = sound.ToString();
            RdpSounds_dropDownButton1.Click += (_, _) =>
            {
                RdpSounds_dropDownButton1.ShowDropDown();
            };
        }

        private Enums.RdpSounds GetRdpSounds()
        {
            return Enum.TryParse<Enums.RdpSounds>(RdpSounds_dropDownButton1.Text, out var result) ? result : Enums.RdpSounds.DoNotPlay;
        }
        private void RdpSoundsItem_Click(object sender, EventArgs e)
        {
            RdpSounds_dropDownButton1.Text = ((DXMenuItem)sender).Caption;
        }
        #endregion
        #region RdpKeyboardHookMode
        private void SetRdpKeyboardHookMode(Enums.RdpKeyboardHookMode en)
        {
            DXPopupMenu menu = new DXPopupMenu();
            IEnumerable<Enums.RdpKeyboardHookMode> ens = Enum.GetValues(typeof(Enums.RdpKeyboardHookMode))
                .OfType<Enums.RdpKeyboardHookMode>();
            foreach (var en1 in ens)
                menu.Items.Add(new DXMenuItem(en1.ToString(), RdpKeyboardHookModeItem_Click));
            RdpKeyboardHookMode_dropDownButton2.DropDownControl = menu;

            RdpKeyboardHookMode_dropDownButton2.Text = en.ToString();
            RdpKeyboardHookMode_dropDownButton2.Click += (_, _) =>
            {
                RdpKeyboardHookMode_dropDownButton2.ShowDropDown();
            };
        }

        private Enums.RdpKeyboardHookMode GetRdpKeyboardHookMode()
        {
            return Enum.TryParse<Enums.RdpKeyboardHookMode>(RdpKeyboardHookMode_dropDownButton2.Text, out var result) ? result : Enums.RdpKeyboardHookMode.ApplyAtTheRemoteServer;
        }
        private void RdpKeyboardHookModeItem_Click(object sender, EventArgs e)
        {
            RdpKeyboardHookMode_dropDownButton2.Text = ((DXMenuItem)sender).Caption;
        }
        #endregion
        #region CacheBitmap
        private void SetCacheBitmap(Enums.CacheBitmap en)
        {
            DXPopupMenu menu = new DXPopupMenu();
            IEnumerable<Enums.CacheBitmap> ens = Enum.GetValues(typeof(Enums.CacheBitmap))
                .OfType<Enums.CacheBitmap>();
            foreach (var en1 in ens)
                menu.Items.Add(new DXMenuItem(en1.ToString(), CacheBitmapItem_Click));
            CacheBitmap_dropDownButton3.DropDownControl = menu;

            CacheBitmap_dropDownButton3.Text = en.ToString();
            CacheBitmap_dropDownButton3.Click += (_, _) =>
            {
                CacheBitmap_dropDownButton3.ShowDropDown();
            };
        }
        private Enums.CacheBitmap GetCacheBitmap()
        {
            return Enum.TryParse<Enums.CacheBitmap>(CacheBitmap_dropDownButton3.Text, out var result) ? result : Enums.CacheBitmap.UseCacheBitmaps;
        }
        private void CacheBitmapItem_Click(object sender, EventArgs e)
        {
            CacheBitmap_dropDownButton3.Text = ((DXMenuItem)sender).Caption;
        }
        #endregion
        #region RdpAuthenticationLevel
        private void SetRdpAuthenticationLevel(Enums.RdpAuthenticationLevel en)
        {
            DXPopupMenu menu = new DXPopupMenu();
            IEnumerable<Enums.RdpAuthenticationLevel> ens = Enum.GetValues(typeof(Enums.RdpAuthenticationLevel))
                .OfType<Enums.RdpAuthenticationLevel>();
            foreach (var en1 in ens)
                menu.Items.Add(new DXMenuItem(en1.ToString(), RdpAuthenticationLevelItem_Click));
            RdpAuthenticationLevel_dropDownButton4.DropDownControl = menu;

            RdpAuthenticationLevel_dropDownButton4.Text = en.ToString();
            RdpAuthenticationLevel_dropDownButton4.Click += (_, _) =>
            {
                RdpAuthenticationLevel_dropDownButton4.ShowDropDown();
            };
        }
        private Enums.RdpAuthenticationLevel GetRdpAuthenticationLevel()
        {
            return Enum.TryParse<Enums.RdpAuthenticationLevel>(RdpAuthenticationLevel_dropDownButton4.Text, out var result) ? result : Enums.RdpAuthenticationLevel.WarnIfAuthFails;
        }
        private void RdpAuthenticationLevelItem_Click(object sender, EventArgs e)
        {
            RdpAuthenticationLevel_dropDownButton4.Text = ((DXMenuItem)sender).Caption;
        }
        #endregion
        #region RdpNetworkConnectionType
        private void SetRdpNetworkConnectionType(Enums.RdpNetworkConnectionType en)
        {
            DXPopupMenu menu = new DXPopupMenu();
            IEnumerable<Enums.RdpNetworkConnectionType> ens = Enum.GetValues(typeof(Enums.RdpNetworkConnectionType))
                .OfType<Enums.RdpNetworkConnectionType>();
            foreach (var en1 in ens)
                menu.Items.Add(new DXMenuItem(en1.ToString(), RdpNetworkConnectionTypeItem_Click));
            RdpNetworkConnectionType_dropDownButton5.DropDownControl = menu;

            RdpNetworkConnectionType_dropDownButton5.Text = en.ToString();
            RdpNetworkConnectionType_dropDownButton5.Click += (_, _) =>
            {
                RdpNetworkConnectionType_dropDownButton5.ShowDropDown();
            };
        }
        private Enums.RdpNetworkConnectionType GetRdpNetworkConnectionType()
        {
            return Enum.TryParse<Enums.RdpNetworkConnectionType>(RdpNetworkConnectionType_dropDownButton5.Text, out var result) ? result : Enums.RdpNetworkConnectionType.Modem;
        }
        private void RdpNetworkConnectionTypeItem_Click(object sender, EventArgs e)
        {
            RdpNetworkConnectionType_dropDownButton5.Text = ((DXMenuItem)sender).Caption;
        }
        #endregion
        #region RdpSoundQuality
        private void SetRdpSoundQuality(Enums.RdpSoundQuality en)
        {
            DXPopupMenu menu = new DXPopupMenu();
            IEnumerable<Enums.RdpSoundQuality> ens = Enum.GetValues(typeof(Enums.RdpSoundQuality))
                .OfType<Enums.RdpSoundQuality>();
            foreach (var en1 in ens)
                menu.Items.Add(new DXMenuItem(en1.ToString(), RdpSoundQualityItem_Click));
            RdpSoundQuality_dropDownButton6.DropDownControl = menu;

            RdpSoundQuality_dropDownButton6.Text = en.ToString();
            RdpSoundQuality_dropDownButton6.Click += (_, _) =>
            {
                RdpSoundQuality_dropDownButton6.ShowDropDown();
            };
        }
        private Enums.RdpSoundQuality GetRdpSoundQuality()
        {
            return Enum.TryParse<Enums.RdpSoundQuality>(RdpSoundQuality_dropDownButton6.Text, out var result) ? result : Enums.RdpSoundQuality.Dynamic;
        }
        private void RdpSoundQualityItem_Click(object sender, EventArgs e)
        {
            RdpSoundQuality_dropDownButton6.Text = ((DXMenuItem)sender).Caption;
        }
        #endregion
        #region RdpCompress
        private void SetRdpCompress(Enums.RdpCompress en)
        {
            DXPopupMenu menu = new DXPopupMenu();
            IEnumerable<Enums.RdpCompress> ens = Enum.GetValues(typeof(Enums.RdpCompress))
                .OfType<Enums.RdpCompress>();
            foreach (var en1 in ens)
                menu.Items.Add(new DXMenuItem(en1.ToString(), RdpCompressItem_Click));
            RdpCompress_dropDownButton7.DropDownControl = menu;

            RdpCompress_dropDownButton7.Text = en.ToString();
            RdpCompress_dropDownButton7.Click += (_, _) =>
            {
                RdpCompress_dropDownButton7.ShowDropDown();
            };
        }
        private Enums.RdpCompress GetRdpCompress()
        {
            return Enum.TryParse<Enums.RdpCompress>(RdpCompress_dropDownButton7.Text, out var result) ? result : Enums.RdpCompress.UseCompress;
        }
        private void RdpCompressItem_Click(object sender, EventArgs e)
        {
            RdpCompress_dropDownButton7.Text = ((DXMenuItem)sender).Caption;
        }
        #endregion
        #region BoolValue
        private void SetBoolValue(ConnectionInfo connInfo)
        {
            RedirectClipboard_checkEdit1.Checked = connInfo.RedirectClipboard;
            DisplayThemes_checkEdit2.Checked = connInfo.DisplayThemes;
            DisplayWallpaper_checkEdit3.Checked = connInfo.DisplayWallpaper;
            EnableFontSmoothing_checkEdit4.Checked = connInfo.EnableFontSmoothing;
            EnableDesktopComposition_checkEdit5.Checked = connInfo.EnableDesktopComposition;
            DisableFullWindowDrag_checkEdit6.Checked = !connInfo.DisableFullWindowDrag;
            DisableMenuAnimations_checkEdit7.Checked = connInfo.DisableMenuAnimations;
            DisableCursorShadow_checkEdit8.Checked = connInfo.DisableCursorShadow;
            DisableCursorBlinking_checkEdit9.Checked = connInfo.DisableCursorBlinking;
            RdpAlertIdleTimeout_checkEdit10.Checked = connInfo.RdpAlertIdleTimeout;
            RdpMinutesToIdleTimeout_spinEdit1.Value = connInfo.RdpMinutesToIdleTimeout;
            SmartSize_checkEdit1.Checked = connInfo.SmartSize;
            FitToWindow_checkEdit2.Checked = connInfo.FitToWindow;
            GrabFocusOnConnect_checkEdit3.Checked = connInfo.GrabFocusOnConnect;
            EnableAutoReconnect_checkEdit4.Checked = connInfo.EnableAutoReconnect;
            // ReSharper disable once PossibleLossOfFraction
            KeepAliveInterval_spinEdit1.Value = connInfo.KeepAliveInterval < 10000 ? 0 : connInfo.KeepAliveInterval / 1000;
            RelativeMouseMode_checkEdit6.Checked = connInfo.RelativeMouseMode;
            AudioCaptureRedirectionMode_checkEdit1.Checked=connInfo.AudioCaptureRedirectionMode;
        }
        private void GetBoolValue(ConnectionInfo connInfo)
        {
            connInfo.RedirectClipboard = RedirectClipboard_checkEdit1.Checked;
            connInfo.DisplayThemes = DisplayThemes_checkEdit2.Checked;
            connInfo.DisplayWallpaper = DisplayWallpaper_checkEdit3.Checked;
            connInfo.EnableFontSmoothing = EnableFontSmoothing_checkEdit4.Checked = connInfo.EnableFontSmoothing;
            connInfo.EnableDesktopComposition = EnableDesktopComposition_checkEdit5.Checked;
            connInfo.DisableFullWindowDrag = !DisableFullWindowDrag_checkEdit6.Checked;
            connInfo.DisableMenuAnimations = DisableMenuAnimations_checkEdit7.Checked;
            connInfo.DisableCursorShadow = DisableCursorShadow_checkEdit8.Checked;
            connInfo.DisableCursorBlinking = DisableCursorBlinking_checkEdit9.Checked;
            connInfo.RdpAlertIdleTimeout = RdpAlertIdleTimeout_checkEdit10.Checked;
            connInfo.RdpMinutesToIdleTimeout = (int)RdpMinutesToIdleTimeout_spinEdit1.Value;
            connInfo.SmartSize = SmartSize_checkEdit1.Checked;
            connInfo.FitToWindow = FitToWindow_checkEdit2.Checked;
            connInfo.GrabFocusOnConnect = GrabFocusOnConnect_checkEdit3.Checked;
            connInfo.EnableAutoReconnect = EnableAutoReconnect_checkEdit4.Checked;
            connInfo.KeepAliveInterval = (int)KeepAliveInterval_spinEdit1.Value * 1000;
            connInfo.RelativeMouseMode = RelativeMouseMode_checkEdit6.Checked;
            connInfo.AudioCaptureRedirectionMode = AudioCaptureRedirectionMode_checkEdit1.Checked;
        }
        #endregion

    }
}
