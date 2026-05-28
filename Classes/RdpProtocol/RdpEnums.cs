using System;
using System.ComponentModel;

namespace VaR.Enums
{
    // RDP Performance Flags
    [Flags]
    public enum RdpPerformanceFlags
    {
        [Description("strRDPDisableWallpaper")]
        DisableWallpaper = 0x1,

        [Description("strRDPDisableFullWindowdrag")]
        DisableFullWindowDrag = 0x2,

        [Description("strRDPDisableMenuAnimations")]
        DisableMenuAnimations = 0x4,

        [Description("strRDPDisableThemes")]
        DisableThemes = 0x8,

        [Description("strRDPDisableCursorShadow")]
        DisableCursorShadow = 0x20,

        [Description("strRDPDisableCursorblinking")]
        DisableCursorBlinking = 0x40,

        [Description("strRDPEnableFontSmoothing")]
        EnableFontSmoothing = 0x80,

        [Description("strRDPEnableDesktopComposition")]
        EnableDesktopComposition = 0x100,

    }

    // RDP Colors
    public enum RdpColors
    {
        Colors256 = 8,
        Colors15Bit = 15,
        Colors16Bit = 16,
        Colors24Bit = 24,
        Colors32Bit = 32
    }

    // RDP Sounds
    public enum RdpSounds
    {
        BringToThisComputer = 0,
        LeaveAtRemoteComputer = 1,
        DoNotPlay = 2
    }
    public enum RdpNetworkConnectionType
    {
        /// <summary>
        /// Modem (56 Kbps)
        /// </summary>
        Modem = 1,

        /// <summary>
        /// Low-speed broadband (256 Kbps to 2 Mbps)
        /// </summary>
        BroadbandLow = 2,

        /// <summary>
        /// Satellite (2 Mbps to 16 Mbps, with high latency)
        /// </summary>
        Satellite = 3,

        /// <summary>
        /// High-speed broadband (2 Mbps to 10 Mbps)
        /// </summary>
        BroadbandHigh = 4,

        /// <summary>
        /// Wide area network (WAN) (10 Mbps or higher, with high latency)
        /// </summary>
        Wan = 5,

        /// <summary>
        /// Local area network (LAN) (10 Mbps or higher)
        /// </summary>
        Lan = 6,

        /// <summary>
        /// Automatically detect the connection type. Warning: setting
        /// this will prevent the client from setting several performance
        /// options such as displaying wallpaper and remote cursors.
        /// </summary>
        AutoDetect = 7
    }
    // RDP Resolutions
    public enum RdpResolutions
    {
        SmartSize,
        FitToWindow,
        Fullscreen
    }

    // RDP Authentication Level
    public enum RdpAuthenticationLevel
    {
        AlwaysConnectEvenIfAuthFails = 0,
        DontConnectWhenAuthFails = 1,
        WarnIfAuthFails = 2
    }

    // RDP Disk Drives
    public enum RdpDiskDrives
    {
        None,
        Local,
        All,
        Custom
    }

    public enum RdpKeyboardHookMode
    {
        /// <summary>
        /// Apply key combinations only locally at the client computer
        /// </summary>
        ApplyOnlyLocalClientComputer,
        /// <summary>
        /// Apply key combinations at the remote server
        /// </summary>
        ApplyAtTheRemoteServer,
        /// <summary>
        /// Apply key combinations to the remote server only when the client is running in full-screen mode. This is the default value
        /// </summary>
        ApplyAtTheRemoteServerInFullScreenMode
    }
    /// <summary>
    /// Указывает, включено ли кэширование точечных рисунков. Постоянное кэширование может повысить производительность, но требует дополнительного места на диске.
    /// </summary>
    public enum CacheBitmap
    {
        NonCacheBitmaps,
        UseCacheBitmaps
    }
    /// <summary>
    /// RDP Redirect Sound
    /// </summary>
    public enum RdpSoundQuality
    {
        Dynamic = 0,
        Medium = 1,
        High = 2
    }

    public enum RdpCompress
    {
        NonCompress,
        UseCompress
    }
    /// <summary>
    /// Версия RDP клиента
    /// </summary>
    public enum RdpVersion
    {
        Rdc6,
        Rdc7,
        Rdc8,
        Rdc9,
        Rdc10,
        Rdc11,
        //Rdc12,
        Highest = 1000
    }

}
