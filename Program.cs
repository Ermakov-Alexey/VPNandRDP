using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;
using ConnectLIbrary;
using LogsFile;

namespace VaR
{
    static class Program
    {
        public static Servers Servers;
        public static string AppPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IT", "VaR");
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (StartService(args)) return;
            if (HasAdministrativeRight()) return;
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                SetServers();
#if DEBUG
                var contact = new Contact
                {
                    Name = "Иванов Иван",
                    Email = "admin1@ps.ru",
                    //ID = 29
                    ID = 395
                };
#else

                var contact = LoginShow();
                if (contact != null)
#endif
                Application.Run(new ConnectionForm(contact));
            } catch (Exception ex)
            {
                Logs.Error(typeof(Program), MethodBase.GetCurrentMethod(), "Error", ex);
            }
        }

        private static bool HasAdministrativeRight()
        {
            WindowsPrincipal principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            bool hasAdministrativeRight = principal.IsInRole(WindowsBuiltInRole.Administrator);

            if (hasAdministrativeRight == false)
            {
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    Verb = "runas",
                    FileName = Application.ExecutablePath
                };
                try
                {
                    Process.Start(processInfo);
                } catch (Win32Exception)
                {

                }
                Application.Exit();
                return true;
            }

            return false;
        }

        private static bool StartService(string[] args)
        {
            if (args.Length == 3 && args[0] == "/service")
            {
                var t = new Thread(() =>
                {
                    try
                    {
                        var currentProcess = Process.GetCurrentProcess();
                        var uiProcess = Process.GetProcessById(int.Parse(args[2]));
                        if (uiProcess.MainModule != null && currentProcess.MainModule != null && uiProcess.MainModule.FileName != currentProcess.MainModule.FileName)
                            return;
                        uiProcess.WaitForExit();
                        Tunnel.Service.Remove(args[1], false);
                    } catch (Exception ex)
                    {
                        Logs.Error(typeof(Program), MethodBase.GetCurrentMethod(), "Error", ex);
                    }
                });
                t.Start();
                Tunnel.Service.Run(args[1]);
                t.Interrupt();
                return true;
            }

            return false;
        }

        private static Contact LoginShow()
        {
            using LoginForm loginForm = new LoginForm();
            if (loginForm.ShowDialog() == DialogResult.OK)
                return loginForm.Contact;

            return null;
        }

        private static void SetServers()
        {
            //TODO временный apiUrl, потом надо добавить для каждого сервера свой
            var apiUrl = "http://localhost:25000";
            var apiKey = Constants.VaRKey;

            Servers = new Servers();
            //Servers.Add(new Server("hd.pizza-sicilia.ru WireGuard", "hd.pizza-sicilia.ru", 25001, VPNType.WireGuard) { ApiUrl = apiUrl, ApiKey = apiKey });
            //Servers.Add(new Server("192.168.1.1 WireGuard", "192.168.1.1", 25001, VPNType.WireGuard) { ApiUrl = apiUrl, ApiKey = apiKey });
            //TODO это надо проверить дома
            //Servers.Add(new Server("80.80.113.70 WireGuard", "80.80.113.70", 25001, VPNType.WireGuard) { ApiUrl = apiUrl, ApiKey = apiKey });
            Servers.Add(new Server("hd.pizza-sicilia.ru OpenVPN", "hd.pizza-sicilia.ru", 25001, VPNType.OpenVPN) { ApiUrl = apiUrl, ApiKey = apiKey });
            Servers.Add(new Server("194.226.129.132 OpenVPN", "194.226.129.132", 25001, VPNType.OpenVPN) { ApiUrl = apiUrl, ApiKey = apiKey });
            Servers.Add(new Server("80.80.113.70 OpenVPN", "80.80.113.70", 25001, VPNType.OpenVPN) { ApiUrl = apiUrl, ApiKey = apiKey });
            Servers.Add(new Server("194.226.129.132 L2TP", "194.226.129.132", 25001) { ApiUrl = apiUrl, ApiKey = apiKey });
            //Servers.Add(new Server("194.226.129.132 PPTP", "194.226.129.132", 25001, VPNType.PPTP) { ApiUrl = apiUrl, ApiKey = apiKey });
            Servers.Add(new Server("80.80.113.70 L2TP", "80.80.113.70", 25001) { ApiUrl = apiUrl, ApiKey = apiKey });
            //Servers.Add(new Server("80.80.113.70 PPTP", "80.80.113.70", 25001, VPNType.PPTP) { ApiUrl = apiUrl, ApiKey = apiKey });
            Servers.Add(new Server("213.226.124.125 L2TP", "213.226.124.125", 25002) { ApiUrl = apiUrl, ApiKey = apiKey });

        }
    }
}
