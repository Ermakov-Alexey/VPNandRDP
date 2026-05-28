using LogsFile;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace VaR;

public static class TapDriverInstaller
{
    #region struct
    [StructLayout(LayoutKind.Sequential)]
    private struct SpDevInfoData
    {
        public uint cbSize;
        public Guid classGuid;
        public uint devInst;
        public IntPtr reserved;
    }
    #endregion
    #region const
    // GUID драйвера TAP (стандартный для OpenVPN) {4D36E972-E325-11CE-BFC1-08002BE10318} - Class GUID для Net
    private static readonly Guid GuidDevClassNet = new("{4d36e972-e325-11ce-bfc1-08002be10318}"); // Класс Сетевые карты
    private const uint DigCfPresent = 0x00000002; // Только присутствующие в системе устройства
    private const uint SpdRpHardwareId = 0x00000001; // Свойство: Hardware ID

    private const uint DiirFlagNone = 0x00000000;
    private const uint InstallFlagForce = 0x00000001;
    private const uint DicdGenerateID = 0x00000001;
    private const uint DifRegisterDevice = 0x00000019;
    private const uint DifRemoveDevice = 0x00000002;
    #endregion

    #region DllImports
    [DllImport("newdev.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
    private static extern bool DiUninstallDriverW(
         IntPtr hwndParent,
         string infPath,
         uint flags,
         out bool rebootRequired
     );
    [DllImport("newdev.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
    private static extern bool UpdateDriverForPlugAndPlayDevicesW(
        IntPtr hwndParent,
        string hardwareId,
        string infPath,
        uint installFlags,
        out bool rebootRequired
    );

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr SetupDiCreateDeviceInfoList(ref Guid classGuid, IntPtr hwndParent);

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool SetupDiCreateDeviceInfo(
        IntPtr deviceInfoSet,
        string deviceName,
        ref Guid classGuid,
        string deviceDescription,
        IntPtr hwndParent,
        uint creationFlags,
        ref SpDevInfoData deviceInfoData
    );

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool SetupDiSetDeviceRegistryProperty(
        IntPtr deviceInfoSet,
        ref SpDevInfoData deviceInfoData,
        uint property,
        byte[] propertyBuffer,
        uint propertyBufferSize
    );

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool SetupDiCallClassInstaller(
        uint installFunction,
        IntPtr deviceInfoSet,
        ref SpDevInfoData deviceInfoData
    );

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr SetupDiGetClassDevs(
        ref Guid classGuid,
        string enumerator,
        IntPtr hwndParent,
        uint flags
    );

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiEnumDeviceInfo(
        IntPtr deviceInfoSet,
        uint memberIndex,
        ref SpDevInfoData deviceInfoData
    );

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool SetupDiGetDeviceRegistryProperty(
        IntPtr deviceInfoSet,
        ref SpDevInfoData deviceInfoData,
        uint property,
        out uint propertyRegDataType,
        StringBuilder propertyBuffer,
        uint propertyBufferSize,
        out uint requiredSize
    );

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);
    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiRemoveDevice(IntPtr deviceInfoSet, ref SpDevInfoData deviceInfoData);

    #endregion

    /// <summary>
    /// Программная установка TAP-драйвера по пути к INF-файлу
    /// </summary>
    public static bool CreateAndInstallTapDevice(string infPath, string hardwareId = "tap0901")
    {
        if (!System.IO.File.Exists(infPath))
        {
            Logs.Warn(typeof(TapDriverInstaller), MethodBase.GetCurrentMethod(), $"INF файл не найден: {infPath}");
            return false;
        }
        // 1. Проверяем, есть ли уже активные адаптеры
        Logs.Trace(typeof(TapDriverInstaller), MethodBase.GetCurrentMethod(), "1. Проверяем, есть ли уже активные адаптеры");
        int activeAdapters = CountTapAdapters(hardwareId);
        if (CountTapAdapters(hardwareId) > 0)
        {
            Logs.Trace(typeof(TapDriverInstaller), MethodBase.GetCurrentMethod(),
                $"Найдено активных TAP адаптеров: {activeAdapters}. Пропускаем установку.");
            return true; // Считаем, что всё ок, раз адаптер есть
        }

        Guid netGuid = GuidDevClassNet;
        // 2. Создаем пустой список устройств для класса "Сетевые карты"
        Logs.Trace(typeof(TapDriverInstaller), MethodBase.GetCurrentMethod(), "2. Создаем пустой список устройств для класса Сетевые карты");
        IntPtr deviceInfoSet = SetupDiCreateDeviceInfoList(ref netGuid, IntPtr.Zero);
        if (deviceInfoSet == IntPtr.Zero || deviceInfoSet.ToInt64() == -1)
        {
            Logs.Warn(typeof(TapDriverInstaller), MethodBase.GetCurrentMethod(), $"Ошибка SetupDiCreateDeviceInfoList: 0x{Marshal.GetLastWin32Error():X}");
            return false;
        }

        try
        {
            SpDevInfoData devInfoData = new SpDevInfoData();
            devInfoData.cbSize = (uint)Marshal.SizeOf(devInfoData);

            // 3. Создаем виртуальное устройство (Device Node) в дереве Windows
            // Имя класса пишется как "Net", описание — произвольное
            Logs.Trace(typeof(TapDriverInstaller), MethodBase.GetCurrentMethod(), "3. Создаем виртуальное устройство (Device Node) в дереве Windows");
            if (!SetupDiCreateDeviceInfo(deviceInfoSet, "Net", ref netGuid, "TAP-Windows Adapter V9", IntPtr.Zero, DicdGenerateID, ref devInfoData))
            {
                Logs.Warn(typeof(TapDriverInstaller), MethodBase.GetCurrentMethod(), $"Ошибка SetupDiCreateDeviceInfo: 0x{Marshal.GetLastWin32Error():X}");
                return false;
            }

            // 4. Записываем HardwareID ("tap0901") в свойства созданного устройства
            // Строка должна заканчиваться двойным нулем (\0\0), так как REG_MULTI_SZ
            byte[] hwIdBytes = Encoding.Unicode.GetBytes(hardwareId + "\0\0");
            Logs.Trace(typeof(TapDriverInstaller), MethodBase.GetCurrentMethod(), "4. Записываем HardwareID (tap0901) в свойства созданного устройства");
            if (!SetupDiSetDeviceRegistryProperty(deviceInfoSet, ref devInfoData, SpdRpHardwareId, hwIdBytes, (uint)hwIdBytes.Length))
            {
                Logs.Warn(typeof(TapDriverInstaller), MethodBase.GetCurrentMethod(), $"Ошибка SetupDiSetDeviceRegistryProperty: 0x{Marshal.GetLastWin32Error():X}");
                return false;
            }

            // 5. Регистрируем виртуальное устройство в Windows (оно появится в Диспетчере устройств как "Неизвестное")
            Logs.Trace(typeof(TapDriverInstaller), MethodBase.GetCurrentMethod(), "5. Регистрируем виртуальное устройство в Windows (оно появится в Диспетчере устройств как Неизвестное)");
            if (!SetupDiCallClassInstaller(DifRegisterDevice, deviceInfoSet, ref devInfoData))
            {
                Logs.Warn(typeof(TapDriverInstaller), MethodBase.GetCurrentMethod(), $"Ошибка SetupDiCallClassInstaller: 0x{Marshal.GetLastWin32Error():X}");
                return false;
            }

            // 6. Вот теперь узел существует! Вызываем обновление, чтобы Windows сопоставила его с вашим OemVista.inf
            Logs.Trace(typeof(TapDriverInstaller), MethodBase.GetCurrentMethod(), "6. Вот теперь узел существует! Вызываем обновление, чтобы Windows сопоставила его с вашим OemVista.inf");
            bool updateResult = UpdateDriverForPlugAndPlayDevicesW(
                IntPtr.Zero,
                hardwareId,
                infPath,
                InstallFlagForce,
                out bool rebootRequired
            );

            if (!updateResult)
            {
                Logs.Warn(typeof(TapDriverInstaller), MethodBase.GetCurrentMethod(), $"Ошибка привязки драйвера к узлу: 0x{Marshal.GetLastWin32Error():X}");
                return false;
            }
            Logs.Trace(typeof(TapDriverInstaller), MethodBase.GetCurrentMethod(), "Драйвер установлен");

            if (rebootRequired)
            {
                Logs.Info(typeof(TapDriverInstaller), MethodBase.GetCurrentMethod(), "Драйвер привязан. Системе требуется перезагрузка.");
            }

            return true;
        } catch (Exception ex)
        {
            Logs.Error(typeof(TapDriverInstaller), MethodBase.GetCurrentMethod(), "Критическая ошибка", ex);
            return false;
        } finally
        {
            SetupDiDestroyDeviceInfoList(deviceInfoSet);
        }
    }
    /// <summary>
    /// Программное удаление TAP-драйвера из системы
    /// </summary>
    public static bool UninstallDriver(string infPath)
    {
        if (!IsDriverInstalled())
        {
            Logs.Trace(typeof(TapDriverInstaller), MethodBase.GetCurrentMethod(), "Драйвер TAP отсутствует.");
            return true;
        }
        try
        {
            Logs.Trace(typeof(TapDriverInstaller), MethodBase.GetCurrentMethod(), "Попробуем удалить драйвер TAP");
            bool result = DiUninstallDriverW(IntPtr.Zero, infPath, DiirFlagNone, out bool rebootRequired);

            if (!result)
            {
                int error = Marshal.GetLastWin32Error();
                Logs.Warn(typeof(TapDriverInstaller), MethodBase.GetCurrentMethod(), $"Ошибка удаления драйвера. Win32 Error Code: {error}");
            }
            else
                Logs.Trace(typeof(TapDriverInstaller), MethodBase.GetCurrentMethod(), "Драйвер TAP удален");
            if (rebootRequired)
            {
                Logs.Info(typeof(TapDriverInstaller), MethodBase.GetCurrentMethod(), "Драйвер привязан. Системе требуется перезагрузка.");
            }
            return result;
        } catch (Exception ex)
        {
            Logs.Error(typeof(TapDriverInstaller), MethodBase.GetCurrentMethod(), "Исключение при удалении", ex);
            return false;
        }
    }
    /// <summary>
    /// Программное удаление всех TAP-адаптеров и связанных драйверов.
    /// Аналог tapinstall.exe remove tap*
    /// </summary>
    public static bool UninstallAllTapAdapters()
    {
        Logs.Trace(typeof(TapDriverInstaller), MethodBase.GetCurrentMethod(), "Начало удаления всех TAP-адаптеров...");

        Guid netClassGuid = GuidDevClassNet;
        // DIGCF_PRESENT | DIGCF_ALLCLASSES - ищем во всех классах, но фильтруем по GUID сети
        IntPtr deviceInfoSet = SetupDiGetClassDevs(ref netClassGuid, null, IntPtr.Zero, DigCfPresent);

        if (deviceInfoSet == IntPtr.Zero || deviceInfoSet.ToInt64() == -1)
        {
            Logs.Warn(typeof(TapDriverInstaller), MethodBase.GetCurrentMethod(), "Не удалось получить список устройств.");
            return false;
        }

        try
        {
            SpDevInfoData devInfoData = new SpDevInfoData();
            devInfoData.cbSize = (uint)Marshal.SizeOf(devInfoData);
            uint memberIndex = 0;
            bool foundAny = false;
            int successCount = 0;

            while (SetupDiEnumDeviceInfo(deviceInfoSet, memberIndex, ref devInfoData))
            {
                memberIndex++;
                StringBuilder buffer = new StringBuilder(1024);

                if (SetupDiGetDeviceRegistryProperty(
                    deviceInfoSet,
                    ref devInfoData,
                    SpdRpHardwareId,
                    out _,
                    buffer,
                    (uint)buffer.Capacity,
                    out _))
                {
                    string hwId = buffer.ToString();

                    if (hwId.IndexOf("tap0901", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        foundAny = true;
                        Logs.Trace(typeof(TapDriverInstaller), MethodBase.GetCurrentMethod(), $"Найден TAP адаптер: HWID={hwId}, DevInst={devInfoData.devInst}");

                        // Пробуем удалить устройство напрямую
                        if (SetupDiRemoveDevice(deviceInfoSet, ref devInfoData))
                        {
                            Logs.Trace(typeof(TapDriverInstaller), MethodBase.GetCurrentMethod(), "Устройство успешно удалено.");
                            successCount++;
                        }
                        else
                        {
                            int err = Marshal.GetLastWin32Error();
                            Logs.Warn(typeof(TapDriverInstaller), MethodBase.GetCurrentMethod(),
                                $"Ошибка удаления устройства SetupDiRemoveDevice. Код: {err} (0x{err:X}).");
                        }
                    }
                }
            }

            if (!foundAny)
            {
                Logs.Trace(typeof(TapDriverInstaller), MethodBase.GetCurrentMethod(), "TAP адаптеры не найдены.");
                return true;
            }

            Logs.Trace(typeof(TapDriverInstaller), MethodBase.GetCurrentMethod(), $"Процесс завершен. Удалено устройств: {successCount}");
            return true;

        } catch (Exception ex)
        {
            Logs.Error(typeof(TapDriverInstaller), MethodBase.GetCurrentMethod(), "Критическая ошибка при удалении", ex);
            return false;
        } finally
        {
            SetupDiDestroyDeviceInfoList(deviceInfoSet);
        }
    }

    /// <summary>
    /// Проверяет наличие активных TAP-адаптеров и возвращает их количество.
    /// </summary>
    public static int CountTapAdapters(string targetHardwareId = "tap0901")
    {
        Guid netClassGuid = GuidDevClassNet;
        // DIGCF_PRESENT | DIGCF_ALLCLASSES - ищем во всех классах, но фильтруем по GUID сети
        IntPtr deviceInfoSet = SetupDiGetClassDevs(ref netClassGuid, null, IntPtr.Zero, DigCfPresent);

        if (deviceInfoSet == IntPtr.Zero || deviceInfoSet.ToInt64() == -1)
        {
            return 0;
        }

        try
        {
            SpDevInfoData devInfoData = new SpDevInfoData();
            devInfoData.cbSize = (uint)Marshal.SizeOf(devInfoData);
            uint memberIndex = 0;
            int count = 0;

            while (SetupDiEnumDeviceInfo(deviceInfoSet, memberIndex, ref devInfoData))
            {
                memberIndex++;
                StringBuilder buffer = new StringBuilder(1024);

                if (SetupDiGetDeviceRegistryProperty(
                        deviceInfoSet,
                        ref devInfoData,
                        SpdRpHardwareId,
                        out _,
                        buffer,
                        (uint)buffer.Capacity,
                        out _))
                {
                    string hwId = buffer.ToString();
                    if (hwId.IndexOf(targetHardwareId, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        count++;
                    }
                }
            }
            return count;
        } finally
        {
            SetupDiDestroyDeviceInfoList(deviceInfoSet);
        }
    }
    /// <summary>
    /// Проверяет через SetupAPI, установлена ли в системе сетевая карта с указанным Hardware ID.
    /// </summary>
    /// <param name="targetHardwareId">Hardware ID драйвера (для OpenVPN TAP обычно "tap0901")</param>
    private static bool IsDriverInstalled(string targetHardwareId = "tap0901")
    {
        // 1. Получаем список всех активных сетевых устройств в системе
        Guid netClassGuid = GuidDevClassNet;
        IntPtr deviceInfoSet = SetupDiGetClassDevs(ref netClassGuid, null, IntPtr.Zero, DigCfPresent);

        if (deviceInfoSet == IntPtr.Zero || deviceInfoSet.ToInt64() == -1)
        {
            return false;
        }

        try
        {
            SpDevInfoData devInfoData = new SpDevInfoData();
            devInfoData.cbSize = (uint)Marshal.SizeOf(devInfoData);
            uint memberIndex = 0;

            // 2. Итерируем по всем найденным устройствам
            while (SetupDiEnumDeviceInfo(deviceInfoSet, memberIndex, ref devInfoData))
            {
                memberIndex++;
                StringBuilder buffer = new StringBuilder(1024);

                // 3. Читаем Hardware ID текущего устройства
                if (SetupDiGetDeviceRegistryProperty(
                    deviceInfoSet,
                    ref devInfoData,
                    SpdRpHardwareId,
                    out _,
                    buffer,
                    (uint)buffer.Capacity,
                    out _))
                {
                    string hwId = buffer.ToString();

                    // Если Hardware ID устройства содержит искомую строку (например, "tap0901")
                    if (hwId.IndexOf(targetHardwareId, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true; // Драйвер и устройство найдены!
                    }
                }
            }
        } finally
        {
            // Обязательно освобождаем память, выделенную SetupAPI
            SetupDiDestroyDeviceInfoList(deviceInfoSet);
        }

        return false;
    }
}