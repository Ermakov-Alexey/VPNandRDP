using LogsFile;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using ConnectLIbrary;

namespace VaR
{
    #region класс автообновление
    public static class UpdateForApp
    {
        public static void CheckUpdate(bool install)
        {
            try
            {
                Logs.Trace(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Проверка обновления программы");
                Logs.Trace(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Параметры подключения к ЦО серверу: " + Program.Servers.GetCurrentIPAddress);
                #region проверяем и скачиваем обновление
                string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (path != null && !Directory.Exists(Path.Combine(path, "Update")))
                    Directory.CreateDirectory(Path.Combine(path, "Update"));
                if (path != null && !File.Exists(Path.Combine(path, "Update", AppDomain.CurrentDomain.FriendlyName)))
                {
                    try
                    {
                        Logs.Trace(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Скачиваем обновление первый раз");
                        DownloadUpdate(new ModuleVersion(1, 0, 0, 0), path);
                        Logs.Trace(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Скачивание обновления в первый раз окончено");
                    }
                    catch (Exception ex)
                    {
                        Logs.Error(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Ошибка скачивания обновления", ex);
                    }
                }
                else
                {
                    try
                    {
                        ModuleVersion nVer = new ModuleVersion(FileVersionInfo.GetVersionInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Update", AppDomain.CurrentDomain.FriendlyName)).ProductVersion);
                        Logs.Trace(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Текущая версия:" + nVer);
                        Logs.Trace(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Скачиваем обновление");
                        DownloadUpdate(nVer, path);
                        Logs.Trace(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Скачивание обновления окончено");
                    }
                    catch (Exception ex)
                    {
                        Logs.Error(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Ошибка скачивания обновления", ex);
                    }
                }
                #endregion
                #region проверяем и скачиваем Updater.exe
                if (path != null && File.Exists(Path.Combine(path, "Update", AppDomain.CurrentDomain.FriendlyName)))
                {
                    ModuleVersion ver = new ModuleVersion(FileVersionInfo.GetVersionInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppDomain.CurrentDomain.FriendlyName)).ProductVersion);
                    Logs.Trace(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Текущая версия: " + ver);
                    ModuleVersion newVer = new ModuleVersion(FileVersionInfo.GetVersionInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Update", AppDomain.CurrentDomain.FriendlyName)).ProductVersion);
                    Logs.Trace(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Скачанная версия: " + newVer);
                    if (newVer > ver)
                    {
                        if (!Directory.Exists(Path.Combine(path, "Updater")))
                            Directory.CreateDirectory(Path.Combine(path, "Updater"));
                        if (!File.Exists(Path.Combine(path, "Updater", "Updater.exe")))
                        {
                            try
                            {
                                Logs.Trace(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Скачиваем Updater первый раз");
                                DownloadUpdater(new ModuleVersion(1, 0, 0, 0), path);
                                Logs.Trace(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Скачивание Updater's в первый раз окончено");
                                if (install)
                                    StartUpdater(path, Program.Servers.GetNameDB);
                            }
                            catch (Exception ex)
                            {
                                Logs.Error(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Ошибка скачивания Updater's", ex);
                            }
                        }
                        else
                        {
                            try
                            {
                                ModuleVersion uVer = new ModuleVersion(FileVersionInfo.GetVersionInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Updater", "Updater.exe")).ProductVersion);
                                Logs.Trace(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Текущая версия Updater's:" + uVer);
                                Logs.Trace(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Проверяем и скачиваем Updater");
                                DownloadUpdater(uVer, path);
                                Logs.Trace(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Скачивание Updater's окончено");
                                if (install)
                                    StartUpdater(path, Program.Servers.GetNameDB);
                            }
                            catch (Exception ex)
                            {
                                Logs.Error(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Ошибка скачивания обновления", ex);
                            }
                        }
                    }
                }
                #endregion

            }
            catch (Exception ex)
            {
                Logs.Error(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Ошибка обновления программы", ex);
            }
        }
        public static void StartUpdater(string path, string db)
        {
            try
            {
                Process proc = new Process();
                proc.StartInfo.WorkingDirectory = Path.Combine(path, "Updater");
                proc.StartInfo.FileName = "Updater.exe";
                proc.StartInfo.Arguments = string.Format("Program \"{1}\" {0}", path, AppDomain.CurrentDomain.FriendlyName); // Аргументы командной строки
                Logs.Trace(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Запуск Updater's с аргументом:" + proc.StartInfo.Arguments);
                proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                proc.StartInfo.Verb = "runas";
                proc.Start();
                proc.WaitForExit();
                Thread.Sleep(3000);
            }
            catch (Exception ex)
            {
                Logs.Error(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Ошибка запуска Updater's", ex);
            }
        }
        public static void DownloadUpdate(ModuleVersion ver, string path)
        {
            byte[] ba =Program.Servers. SqlData.SendData($"UPDATE TRY '{AppDomain.CurrentDomain.FriendlyName}' ${ver}", out int a);
            Logs.Trace(typeof(UpdateForApp), MethodBase.GetCurrentMethod(),
                $"UPDATE TRY '{AppDomain.CurrentDomain.FriendlyName}' ${ver}");
            if (a > 0)
            {
                ModuleVersion newVer = new ModuleVersion(SqlData.GetString(ba));
                Logs.Trace(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Имеется обновление до версии: " + newVer);
                Logs.Trace(typeof(UpdateForApp), MethodBase.GetCurrentMethod(),
                    $"UPDATE FILE '{AppDomain.CurrentDomain.FriendlyName}'");
                ba = Program.Servers.SqlData.SendData($"UPDATE FILE '{AppDomain.CurrentDomain.FriendlyName}'", out a);
                if (a > 0)
                {
                    Directory.CreateDirectory(Path.Combine(path, "temp"));
                    File.WriteAllBytes(Path.Combine(path, "temp", "temp.7z"), ba);
                    Logs.Trace(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Получаем файл:" + Path.Combine(path, "temp", "temp.7z"));
                    Logs.Trace(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Архив скачан");
                    Directory.Delete(Path.Combine(path, "Update"), true);
                    Logs.Trace(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Удален старый каталог обновления");
                    try
                    {
                        CallAndWait(Archive.ExtractFromArchive, Path.Combine(path, "temp", "temp.7z"), Path.Combine(path, "Update"), 1);
                    }
                    catch
                    {
                        CallAndWait(Archive.ExtractFromArchiveOld, Path.Combine(path, "temp", "temp.7z"), Path.Combine(path, "Update"), 1);
                    }
                    //ZipFile.ExtractToDirectory(Path.Combine(path, "temp", "temp.7z"), Path.Combine(path, "Update"));
                    Directory.Delete(Path.Combine(path, "temp"), true);
                    Logs.Trace(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Архив распакован и удален");
                }
                else
                {
                    Logs.Warn(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Проблема в скачивании обновления, а=" + a);
                }
                if (newVer == new ModuleVersion(FileVersionInfo.GetVersionInfo(Path.Combine(path, "Update", AppDomain.CurrentDomain.FriendlyName)).ProductVersion))
                    Logs.Trace(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Скачивание обновления прошло успешно");
            }
            else
                Logs.Trace("SqlData", "DownloadArchive", "Обновлений нет");
        }
        public static void DownloadUpdater(ModuleVersion ver, string path)
        {
            byte[] ba = Program.Servers.SqlData.SendData("UPDATE TRY 'Updater.exe' $" + ver, out int a);
            Logs.Trace(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "UPDATE TRY 'Updater.exe' $" + ver);
            if (a > 0)
            {
                ModuleVersion newVer = new ModuleVersion(SqlData.GetString(ba));
                Logs.Trace(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Имеется обновление Updater's до версии: " + newVer);
                ba = Program.Servers.SqlData.SendData("UPDATE FILE 'Updater.exe'", out a);
                Logs.Trace(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "UPDATE FILE 'Updater.exe'");
                if (a > 0)
                {
                    Directory.CreateDirectory(Path.Combine(path, "temp"));
                    File.WriteAllBytes(Path.Combine(path, "temp", "temp.7z"), ba);
                    Logs.Trace(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Архив скачан");
                    Directory.Delete(Path.Combine(path, "Updater"), true);
                    Logs.Trace(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Удален старый каталог обновления");
                    try
                    {
                        CallAndWait(Archive.ExtractFromArchive, Path.Combine(path, "temp", "temp.7z"), Path.Combine(path, "Updater"), 1);
                    }
                    catch
                    {
                        CallAndWait(Archive.ExtractFromArchiveOld, Path.Combine(path, "temp", "temp.7z"), Path.Combine(path, "Updater"), 1);
                    }
                    //ZipFile.ExtractToDirectory(Path.Combine(path, "temp", "temp.7z"), Path.Combine(path, "Updater"));
                    Directory.Delete(Path.Combine(path, "temp"), true);
                    Logs.Trace(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Архив распакован и удален");
                }
                else
                {
                    Logs.Warn(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Проблема в скачивании обновления Updater's, а=" + a);
                }
                if (newVer == new ModuleVersion(FileVersionInfo.GetVersionInfo(Path.Combine(path, "Updater", "Updater.exe")).ProductVersion))
                    Logs.Trace(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Скачивание Updater's прошло успешно");
            }
            else
                Logs.Trace(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Обновлений нет");
        }
        public static bool CallAndWait<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2, int minute, int second = 0)
        {
            var timeout = Convert.ToInt32(second == 0 ? TimeSpan.FromMinutes(minute).TotalMilliseconds : TimeSpan.FromSeconds(second).TotalMilliseconds);
            try
            {
                Thread threadToKill = null;
                Action<T1, T2> wrappedAction = (_, _) =>
                {
                    threadToKill = Thread.CurrentThread;
                    action(arg1, arg2);
                };

                IAsyncResult result = wrappedAction.BeginInvoke(arg1, arg2, null, null);
                if ((timeout != -1) && !result.IsCompleted && (!result.AsyncWaitHandle.WaitOne(timeout, false) || !result.IsCompleted))
                {
                    if (threadToKill != null)
                    {
                        threadToKill.Abort();
                    }
                    throw new TimeoutException();
                }
                else
                {
                    wrappedAction.EndInvoke(result);
                }
                return true;
            }
            catch (TimeoutException)
            {
                Logs.Warn(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "ТаймАут метода " + action.Method.Name);
                return false;
            }
            catch (Exception e2)
            {
                Logs.Error(typeof(UpdateForApp), MethodBase.GetCurrentMethod(), "Ошибка " + action.Method.Name, e2);
                return false;
            }
        }
    }
    #endregion
}
