using System;
using Newtonsoft.Json;

namespace VaR;

public class ConnectionSetup(ConnectRdp connect)
{
    /// <summary>
    /// Имя подключения (отображается во вкладке)
    /// </summary>
    public string Name { get; set; } = connect.Name;

    /// <summary>
    /// Адрес сервера (IP или hostname)
    /// </summary>
    public string Hostname { get; set; } = connect.IP;

    /// <summary>
    /// Имя пользователя для аутентификации
    /// </summary>
    public string Username { get; set; } = connect.LoginRDP;

    /// <summary>
    /// Домен для аутентификации
    /// </summary>
    public string Domain { get; set; } = connect.DomainRDP;

    /// <summary>
    /// Пароль для аутентификации
    /// </summary>
    public string Password { get; set; } = connect.PasswordRDP;

    /// <summary>
    /// Порт RDP (по умолчанию 3389)
    /// </summary>
    public int Port { get; set; } = 3389;
}

/// <summary>
/// Конфигурация RDP подключения
/// </summary>
public class ConnectionInfo
{
    /// <summary>
    /// Глубина цвета (по умолчанию 16 бит)
    /// </summary>
    public Enums.RdpColors Colors { get; set; } = Enums.RdpColors.Colors16Bit;
    /// <summary>
    /// Режим перенаправления звука (по умолчанию не воспроизводить)
    /// </summary>
    public Enums.RdpSounds RedirectSound { get; set; } = Enums.RdpSounds.BringToThisComputer;
    /// <summary>
    /// Перенаправление устройства ввода звука (микрофон)
    /// </summary>
    public bool AudioCaptureRedirectionMode { get; set; } = true;
    /// <summary>
    /// Качество звука при перенаправлении (по умолчанию динамическое)
    /// </summary>
    public Enums.RdpSoundQuality RdpSoundQuality { get; set; } = Enums.RdpSoundQuality.Medium;

    /// <summary>
    /// Перенаправлять локальные диски
    /// </summary>
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace, ItemConverterType = null)]
    [JsonIgnore]
    public bool RedirectDrives { get; set; } = false;
    /// <summary>
    /// Перенаправлять локальные порты
    /// </summary>
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace, ItemConverterType = null)]
    [JsonIgnore]
    public bool RedirectPorts { get; set; } = false;
    /// <summary>
    /// Перенаправлять локальные принтеры
    /// </summary>
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace, ItemConverterType = null)]
    [JsonIgnore]
    public bool RedirectPrinters { get; set; } = false;
    /// <summary>
    /// Перенаправлять смарт-карты
    /// </summary>
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace, ItemConverterType = null)]
    [JsonIgnore]
    public bool RedirectSmartCards { get; set; } = false;
    /// <summary>
    /// Перенаправлять буфер обмена
    /// </summary>
    public bool RedirectClipboard { get; set; } = true;
    /// <summary>
    /// Отображать темы Windows на удалённом рабочем столе
    /// </summary>
    public bool DisplayThemes { get; set; } = false;
    /// <summary>
    /// Отображать обои рабочего стола
    /// </summary>
    public bool DisplayWallpaper { get; set; } = true;
    /// <summary>
    /// Включить сглаживание шрифтов
    /// </summary>
    public bool EnableFontSmoothing { get; set; } = false;
    /// <summary>
    /// Включить композицию рабочего стола (Aero)
    /// </summary>
    public bool EnableDesktopComposition { get; set; } = false;
    /// <summary>
    /// Отключить перетаскивание окон целиком
    /// </summary>
    public bool DisableFullWindowDrag { get; set; } = true;
    /// <summary>
    /// Отключить анимацию меню
    /// </summary>
    public bool DisableMenuAnimations { get; set; } = true;
    /// <summary>
    /// Отключить тень курсора
    /// </summary>
    public bool DisableCursorShadow { get; set; } = true;
    /// <summary>
    /// Отключить мигание курсора
    /// </summary>
    public bool DisableCursorBlinking { get; set; } = true;
    /// <summary>
    /// Подключаться к консольной сессии (admin session)
    /// </summary>
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace, ItemConverterType = null)]
    [JsonIgnore]
    public bool UseConsoleSession { get; set; } = false;
    /// <summary>
    /// Перенаправлять клавиатурные сочетания на удалённый сервер
    /// </summary>
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace, ItemConverterType = null)]
    [JsonIgnore]
    public bool RedirectKeys { get; set; } = true;
    /// <summary>
    /// Задает параметры переадресации клавиатуры, определяющие, как и когда применять сочетание клавиш Windows (например, ALT+TAB)
    /// (по умолчанию Примените комбинации клавиш на удаленном сервере)
    /// </summary>
    public Enums.RdpKeyboardHookMode KeyboardHookMode { get; set; } = Enums.RdpKeyboardHookMode.ApplyAtTheRemoteServer;
    /// <summary>
    /// Оповещать о отключении по таймауту бездействия
    /// </summary>
    public bool RdpAlertIdleTimeout { get; set; } = true;
    /// <summary>
    /// Таймаут бездействия в минутах (по умолчанию 10)
    /// </summary>
    public int RdpMinutesToIdleTimeout { get; set; } = 10;
    /// <summary>
    /// Кэширование битмапов: 0 = отключено, 1 = включено
    /// по умолчанию включено
    /// </summary>
    public Enums.CacheBitmap CacheBitmaps { get; set; } = Enums.CacheBitmap.NonCacheBitmaps;
    /// <summary>
    /// Уровень аутентификации (по умолчанию подключаться даже при ошибке)
    /// </summary>
    public Enums.RdpAuthenticationLevel RdpAuthenticationLevel { get; set; } = Enums.RdpAuthenticationLevel.AlwaysConnectEvenIfAuthFails;

    /// <summary>
    /// Информация для балансировки нагрузки (load balancing)
    /// </summary>
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace, ItemConverterType = null)]
    [JsonIgnore]
    public string LoadBalanceInfo { get; set; } = string.Empty;
    /// <summary>
    /// Тип сетевого подключения для оптимизации производительности (по умолчанию модем)
    /// </summary>
    public Enums.RdpNetworkConnectionType RdpNetworkConnectionType { get; set; } = Enums.RdpNetworkConnectionType.Modem;
    /// <summary>
    /// Масштабировать удалённый рабочий стол под размер окна (true) или менять разрешение (false)
    /// </summary>
    public bool FitToWindow { get; set; } = true;
    /// <summary>
    /// Использовать умное масштабирование (SmartSizing)
    /// </summary>
    public bool SmartSize { get; set; } = false;
    /// <summary>
    /// Указывает, должен ли элемент управления клиента находиться в фокусе во время подключения.
    /// Элемент управления не будет пытаться перехватить фокус у окна, работающего в другом процессе.
    /// </summary>
    public bool GrabFocusOnConnect { get; set; } = true;
    /// <summary>
    /// Указывает, следует ли разрешить клиентскому элементу управления автоматически переподключаться к сеансу в случае разрыва сетевого соединения.
    /// </summary>
    public bool EnableAutoReconnect { get; set; } = true;
    /// <summary>
    /// Задает интервал в миллисекундах, с которым клиент отправляет серверу сообщения, подтверждающие подключение к сети.
    /// Параметр групповой политики, определяющий, разрешены ли постоянные клиентские подключения к серверу, может переопределить этот параметр свойства.
    /// Новый интервал в миллисекундах. Значение по умолчанию для этого свойства равно нулю, что отключает сообщения поддержания соединения.
    /// Минимальное допустимое значение этого свойства — 10 000, что соответствует 10 секундам.
    /// </summary>
    public int KeepAliveInterval
    {
        get => _keepAliveInterval;
        set => _keepAliveInterval = value < 10000 ? 0 : value;
    }
    private int _keepAliveInterval = 60000;
    /// <summary>
    /// Указывает, включен ли для данного подключения поставщик услуг безопасности учетных данных (Credential Security Service Provider, CredSSP)
    /// </summary>
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace, ItemConverterType = null)]
    [JsonIgnore]
    public bool EnableCredSspSupport { get; set; } = true;
    /// <summary>
    /// Указывает или получает информацию о том, включен ли уровень безопасности согласования для данного соединения
    /// </summary>
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace, ItemConverterType = null)]
    [JsonIgnore]
    public bool NegotiateSecurityLayer { get; set; } = true;
    /// <summary>
    /// Указывает, следует ли использовать панель подключения. 
    /// </summary>
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace, ItemConverterType = null)]
    [JsonIgnore]
    public bool DisplayConnectionBar { get; set; } = true;
    /// <summary>
    /// Указывает, следует ли отображать кнопку «Свернуть» на панели подключения.
    /// </summary>
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace, ItemConverterType = null)]
    [JsonIgnore]
    public bool ConnectionBarShowMinimizeButton { get; set; } = false;
    /// <summary>
    /// Указывает, следует ли использовать относительный режим мыши.
    /// </summary>
    public bool RelativeMouseMode { get; set; } = true;
    /// <summary>
    /// Указывает, включено ли сжатие.
    /// </summary>
    public Enums.RdpCompress Compress { get; set; } = Enums.RdpCompress.NonCompress;
    /// <summary>
    /// Указывает, отображается ли в диалоговом окне учетных данных флажок, позволяющий сохранять учетные данные.
    /// </summary>
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace, ItemConverterType = null)]
    [JsonIgnore]
    public bool AllowCredentialSaving { get; set; } = false;
    /// <summary>
    /// Указывает или получает информацию о том, включено ли диалоговое окно запроса учетных данных для данного подключения.
    /// </summary>
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace, ItemConverterType = null)]
    [JsonIgnore]
    public bool PromptForCredentials { get; set; } = false;
    /// <summary>
    /// Указывает, отображает ли элемент управления клиента диалоговое окно с запросом учетных данных.
    /// </summary>
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace, ItemConverterType = null)]
    [JsonIgnore]
    public bool PromptForCredsOnClient { get; set; } = false;
    /// <summary>
    /// Указывает, может ли элемент управления ActiveX «Удаленный рабочий стол» запрашивать у пользователя учетные данные.
    /// </summary>
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace, ItemConverterType = null)]
    [JsonIgnore]
    public bool AllowPromptingForCredentials { get; set; } = false;
    /// <summary>
    /// Использовать несколько экранов
    /// </summary>
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace, ItemConverterType = null)]
    [JsonIgnore]
    public bool Multimon { get; set; } = false;
}
