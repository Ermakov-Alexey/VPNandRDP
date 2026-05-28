using ConfigFile;
using ConnectLIbrary;
using CredentialManagement;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.ViewInfo;
using DevExpress.XtraTab;
using DevExpress.XtraTab.ViewInfo;
using JCS;
using LogsFile;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace VaR
{
    public partial class ConnectionForm : XtraForm
    {
        private bool _vpnUsed;
        private List<ConnectRdp> _connectList;
        private readonly Contact _contact;
        private readonly Config _cfg;
        private FormWindowState _formState;
        private readonly List<VaRRdpProtocol> _protocols;
        private List<Form> FullForms;

        public ConnectionForm(Contact contact)
        {
            InitializeComponent();
            _contact = contact;
            WindowState = FormWindowState.Maximized;
            _vpnUsed = false;
            _cfg = new Config(Path.Combine(Program.AppPath, "config.ini"));
            ServerIsAlive_toolStripTextBox1.Visible = false;
            VPNIsAlive_toolStripButton2.Visible = false;
            listBoxControl1.DataSource = _connectList;
            _protocols = new List<VaRRdpProtocol>();
            SetIsConnected(false);
            MultiMonToolStripMenuItem.Enabled = Screen.AllScreens.Length > 1;
            new Thread(Archive.ArchiveOldLog).Start();
        }

        #region события с формой
        private void ConnectionForm_Load(object sender, EventArgs e)
        {

        }
        private void ConnectionForm_Shown(object sender, EventArgs e)
        {
            try
            {
                using var joinForm = new JoinForm(async (progress, _) =>
                {
                    progress.Report("Собираем данные");
                    _connectList = await ReadConnectToRdp(_contact);
                    ReadCfg();
                    //new Thread(CleanOldRDP).Start();
                    if (_connectList.Count == 1)
                        splitContainer1.Panel1Collapsed = true;
                    await CheckAliveServerAndConnectVpn(progress);

                    if (!string.IsNullOrEmpty(_contact.VPNUser))
                    {
                        VPNIsAlive_toolStripButton2.Visible = true;
                        VPNIsAlive_toolStripButton2.BackColor = SystemColors.Control;
                        VPNIsAlive_toolStripButton2.Text = @"VPN не активен";
                        toolStripTextBox1.Visible = false;
                        if (_vpnUsed)
                        {
                            VPNIsAlive_toolStripButton2.BackColor = Color.GreenYellow;
                            VPNIsAlive_toolStripButton2.Text = @"VPN активирован";
                        }
                        if (Program.Servers.CurrentServer != null)
                        {
                            toolStripTextBox1.Visible = true;
                            toolStripTextBox1.Text = @$"{Program.Servers.GetHostOrIP}-{Program.Servers.CurrentServer.VPNType}";
                            toolStripTextBox1.Image = Program.Servers.CurrentServer.VPNConnect ? Properties.Resources.accept : Properties.Resources.cancel;
                            toolStripTextBox1.BackColor = Program.Servers.CurrentServer.ServerAlive ? Color.GreenYellow : Color.Red;
                        }
                    }

                    if (_connectList.FindAll(x => x.IsAlive).Any())
                    {
                        ServerIsAlive_toolStripTextBox1.BackColor = Color.GreenYellow;
                        ServerIsAlive_toolStripTextBox1.Text = @"Сервер доступен";
                    }
                    else
                    {
                        ServerIsAlive_toolStripTextBox1.BackColor = Color.Red;
                        ServerIsAlive_toolStripTextBox1.Text = @"Сервер не доступен";
                    }

                    ServerIsAlive_toolStripTextBox1.Visible = true;
                    listBoxControl1.DataSource = _connectList;
                    if (_connectList.Count == 1 && _connectList[0].IsAlive)
                        ConnectToRdp();

                    version_toolStripStatusLabel1.Text = @$"Версия: {Assembly.GetExecutingAssembly().GetName().Version}";
                    contact_toolStripStatusLabel1.Text = _contact.Name;
                    panel1.Location = new Point(splitContainer1.Panel2.Size.Width - 80, 0);
                });
                joinForm.ShowDialog(this);
            } catch (Exception ex)
            {
                Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Error", ex);
            }
        }
        private void ConnectionForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Logs.Trace(GetType(), MethodBase.GetCurrentMethod(),
                $"FormClosing: _vpnUsed={_vpnUsed}, e.Cancel={e.Cancel}, Thread={Thread.CurrentThread.ManagedThreadId}");


            if (_vpnUsed)
            {
                // Отменяем закрытие, пока не отключимся
                e.Cancel = true;

                // Блокируем интерфейс
                this.Enabled = false;

                try
                {
                    using var joinForm = new JoinForm(async (progress, _) =>
                    {
                        progress.Report("До скорой встречи");
                        await VpnDisconnect();
                    });
                    joinForm.ShowDialog();
                } catch (Exception ex)
                {
                    Logs.Fatal(GetType(), MethodBase.GetCurrentMethod(), "Error", ex);
                } finally
                {
                    // После отключения разрешаем закрытие
                    Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), "VPN disconnected, allowing close");
                    this.FormClosing -= ConnectionForm_FormClosing; // Убираем подписку, чтобы не зациклиться
                    this.Close(); // Повторно вызываем Close()
                }
            }
            else
            {
                // Если VPN не использовался, просто разрешаем закрытие
                Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), "VPN not used, allowing close");
                // Ничего не делаем - форма закроется сама
            }

            Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), "FormClosing ending");
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Logs.Trace(GetType(), MethodBase.GetCurrentMethod(),
                $"FormClosed: Thread={Thread.CurrentThread.ManagedThreadId}");
            base.OnFormClosed(e);
        }
        private async void ConnectionForm_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.F5)
                {
                    await CheckConnects();
                    e.Handled = true;
                }
            } catch (Exception ex)
            {
                Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Error", ex);
            }
        }
        #endregion
        #region события контролов
        private void Connection_Button1_Click_1(object sender, EventArgs e)
        {
            ConnectToRdp();
        }

        private void ConnectToRdp()
        {
            try
            {
                ConnectRdp connect = _connectList.Find(x => x.IP == listBoxControl1.SelectedValue.ToString());
                if (!connect.IsAlive)
                    return;
                if (!connect.IsConnected)
                {
                    if (FullScreenToolStripMenuItem.Checked)
                        FullScreenConnection(connect);
                    else
                    {
                        XtraTabPage tab = new XtraTabPage();
                        xtraTabControl1.TabPages.Add(tab);
                        xtraTabControl1.SelectedTabPage = tab;

                        CreateRdpSession(connect, tab);
                        SetIsConnected(connect.IsConnected);
                        ReloadDataSet();
                    }
                }
                else
                {
                    foreach (XtraTabPage item in xtraTabControl1.TabPages)
                        if (item.Name == connect.Name)
                        {
                            xtraTabControl1.TabPages.Remove(item);
                            ReloadDataSet();
                            break;
                        }
                }
            } catch (Exception ex)
            {
                Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Ошибка подключения к RDP", ex);
                XtraMessageBox.Show("Ошибка подключения к серверу: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FullScreen_Button2_Click(object sender, EventArgs e)
        {
            XtraTabPage tab = xtraTabControl1.SelectedTabPage;
            if (tab == null) return;

            VaRRdpProtocol protocol = _protocols.Find(x => x.GetParentControl() == tab.Controls[0]);
            ConnectRdp con = _connectList.Find(x => x.IP == protocol.ConnectSetup.Hostname);
            FullScreenConnection(con, MultiMonToolStripMenuItem.Checked ? null : protocol);
            xtraTabControl1.TabPages.Remove(tab);
        }
        private void FullScreenConnection(ConnectRdp con, VaRRdpProtocol pr = null)
        {
            FullForms ??= new List<Form>();
            FullScreenForm fsf = new FullScreenForm(con, MultiMonToolStripMenuItem.Checked, Screen.FromControl(this), pr);
            FullForms.Add(fsf);
            if (pr != null)
                _protocols.Remove(pr);
            fsf.FormClosing += FSF_FormClosing;
            fsf.FormClosed += FSF_FormClosed;
            fsf.Show();

            if (свернутьВТрейПриПодключенииToolStripMenuItem.Checked)
                ToTray_button1.PerformClick();
            else
                _formState = WindowState;
        }
        private void FSF_FormClosing(object sender, FormClosingEventArgs e)
        {
            FullScreenForm fsf = sender as FullScreenForm;
            Visible = true;
            ShowInTaskbar = true;
            WindowState = _formState;
            notifyIcon1.Visible = false;
            notifyIcon1.Click -= NotifyIcon1_Click;
            if (fsf is { FormClose: false })
            {
                XtraTabPage tab = new XtraTabPage();
                xtraTabControl1.TabPages.Add(tab);
                xtraTabControl1.SelectedTabPage = tab;
                if (fsf is { Multimon: false })
                {//передаем контрол без переключения если был один экран
                    VaRRdpProtocol pr = fsf.GetProtocol();
                    ConnectRdp con = _connectList.Find(x => x.IP == pr.ConnectSetup.Hostname);
                    tab.Name = (string)(tab.Tag = tab.Text = con.Name);
                    _protocols.Add(pr);
                    pr.SetFormParent(this);
                    tab.Controls.Add(pr.GetParentControl());
                }
                else
                {//переключаем потому что могло быть два экрана
                    CreateRdpSession(fsf.Connect, tab);
                }
                SetIsConnected(fsf.Connect.IsConnected);
                ReloadDataSet();
            }
            if (FullForms.Contains(fsf))
                FullForms.Remove(fsf);
        }

        private void FSF_FormClosed(object sender, FormClosedEventArgs e)
        {
            GC_Collect();
            if (CloseToDisconectedToolStripMenuItem.Checked)
            {
                if (!xtraTabControl1.TabPages.Any())
                {
                    if (FullForms == null || !FullForms.Any())
                    {
                        // Безопасное закрытие
                        this.BeginInvoke(new Action(Close));
                    }
                }
            }
        }
        private void NotifyIcon1_Click(object sender, EventArgs e)
        {
            Visible = true;
            ShowInTaskbar = true;
            notifyIcon1.Visible = false;
            notifyIcon1.Click -= NotifyIcon1_Click;
            FormBorderStyle = FormBorderStyle.None;
            panel1.Visible = true;
            WindowState = _formState;
        }
        private void CloseByDisconnect_toolStripButton2_CheckedChanged(object sender, EventArgs e)
        {
            _cfg.WriteBoolean("Setting", "CloseByDisconnect", CloseToDisconectedToolStripMenuItem.Checked);
        }
        private void FullScreenToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            _cfg.WriteBoolean("Setting", "Full", FullScreenToolStripMenuItem.Checked);
        }
        private void MultiMonToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            _cfg.WriteBoolean("Setting", "Multimon", MultiMonToolStripMenuItem.Checked);
        }
        private void свернутьВТрейПриПодключенииToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            _cfg.WriteBoolean("Setting", "Tray", свернутьВТрейПриПодключенииToolStripMenuItem.Checked);
        }
        private void ListBoxControl1_DrawItem(object sender, ListBoxDrawItemEventArgs e)
        {
            try
            {
                if (e.Index >= 0)
                {
                    string text = e.Item.ToString();
                    var fill = _connectList.Find(x => x.IP == text).IsAlive ? Color.Green : Color.Red;
                    if (_connectList.Find(x => x.IP == text).IsConnected)
                        fill = Color.Yellow;
                    if ((e.State & DrawItemState.Selected) != 0)
                        fill = Color.Blue;
                    e.Appearance.BackColor = fill;
                }
            } catch (Exception ex)
            {
                Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Error", ex);
            }
        }
        private void XtraTabControl1_ControlRemoved(object sender, ControlEventArgs e)
        {
            try
            {
                if (e.Control is XtraTabPage tab)
                {
                    VaRRdpProtocol protocol = _protocols.Find(x => x.GetParentControl() == tab.Controls[0]);
                    if (protocol != null)
                    {
                        protocol.Disconnect();
                        protocol.Close();
                        _connectList.Find(x => x.IP == protocol.ConnectSetup.Hostname).IsConnected = false;
                        _protocols.Remove(protocol);
                        GC_Collect();
                    }
                }
                SetIsConnected(false);
                if (CloseToDisconectedToolStripMenuItem.Checked)
                {
                    if (!xtraTabControl1.TabPages.Any() && (FullForms == null || !FullForms.Any()))
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            if (!xtraTabControl1.TabPages.Any() && (FullForms == null || !FullForms.Any()))
                            {
                                this.Close();
                            }
                        }));
                    }
                }
            } catch (Exception ex)
            {
                Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Error", ex);
            }
            ReloadDataSet();
        }
        private void XtraTabControl1_CloseButtonClick(object sender, EventArgs e)
        {
            XtraTabControl control = sender as XtraTabControl;
            if (e is ClosePageButtonEventArgs arg)
            {
                XtraTabPage tab = arg.Page as XtraTabPage;
                if (control != null)
                    control.TabPages.Remove(tab);
            }
        }
        private void ListBoxControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxControl1.SelectedValue != null)
                try
                {
                    ConnectRdp connect = _connectList.Find(x => x.IP == listBoxControl1.SelectedValue.ToString());
                    Connection_Button1.Text = connect.IsConnected ? @"Отключиться" : @"Подключитьcя";
                } catch (Exception ex)
                {
                    Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Error", ex);
                }
        }
        private void ListBoxControl1_DoubleClick(object sender, EventArgs e)
        {
            ConnectRdp connect = _connectList.Find(x => x.IP == listBoxControl1.SelectedValue.ToString());
            if (!connect.IsConnected)
                ConnectToRdp();
            else
                foreach (XtraTabPage tab in xtraTabControl1.TabPages)
                    if (tab.Name == connect.Name)
                    {
                        xtraTabControl1.SelectedTabPage = tab;
                        return;
                    }
        }
        private void Refresh_toolStripButton2_Click(object sender, EventArgs e)
        {
            using var joinForm = new JoinForm(async (progress, _) =>
            {
                progress.Report("");
                await RefreshButton();
            });
            joinForm.ShowDialog(this);

        }
        private async Task RefreshButton()
        {
            try
            {
                List<ConnectRdp> connectListCopy = _connectList.Clone().ToList();
                _connectList = await ReadConnectToRdp(_contact);
                foreach (ConnectRdp cl in connectListCopy)
                {
                    ConnectRdp conn = _connectList.Find(x => x.IP.Equals(cl.IP));
                    if (conn != null)
                    {
                        conn.IsAlive = cl.IsAlive;
                        conn.IsConnected = cl.IsConnected;
                    }
                }
                splitContainer1.Panel1Collapsed = _connectList.Count == 1;
                listBoxControl1.DataSource = _connectList;
                await CheckConnects();
                GC_Collect();
            } catch (Exception ex)
            {
                Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Error", ex);
            }
        }
        private void ToolStripButton2_Click(object sender, EventArgs e)
        {
            splitContainer1.Panel1Collapsed = !splitContainer1.Panel1Collapsed;
        }

        #region события RD
        public void SetOnDisconnected(string server, Control parent)
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Terminal Server Client\Servers");
                if (key != null)
                    foreach (string item in key.GetSubKeyNames())
                    {
                        if (item == server)
                            try
                            {
                                key.DeleteSubKey(item, false);
                            } catch (Exception ex)
                            {
                                Logs.Error(GetType(), MethodBase.GetCurrentMethod(),
                                    "Ошибка delete regedit Terminal Servers " + key + ": ", ex);
                            }
                    }
            } catch (Exception ex)
            {
                Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Ошибка delete regedit Terminal Servers: ", ex);
            }
            if (_connectList.Count == 1)
            {
                if (Connection_Button1.Text == @"Отключиться")
                {
                    Connection_Button1.Text = @"Подключиться";
                    _connectList[0].IsConnected = false;
                }
            }
            try
            {
                if (parent != null)
                    xtraTabControl1.TabPages.Remove((XtraTabPage)parent.Parent);
            } catch (Exception ex)
            {
                Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Error", ex);
            }


        }
        public async void SetOnAuthenticationWarningDismissed(string server)
        {
            try
            {
                ConnectRdp connect = _connectList.Find(x => x.IP == server);
                if (connect != null)
                {
                    try
                    {
                        RegistryKey key4 = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Terminal Server Client\Servers\" + connect.IP);
                        if (key4 != null)
                        {
                            byte[] key = (byte[])key4.GetValue("CertHash");
                            if (!key.SequenceEqual(connect.CertHashObject))
                            {
                                connect.ByteArrayToString(key);
                                try
                                {
                                    List<SqlParam> sqlParams =
                                    [
                                        new()
                                        {
                                            Name = "@certHash", SqlDbTypeValue = (int)SqlDbType.NVarChar,
                                            Value = connect.CertHash
                                        },

                                        new()
                                        {
                                            Name = "@ip", SqlDbTypeValue = (int)SqlDbType.NVarChar,
                                            Value = connect.IP
                                        }

                                    ];
                                    await ApiBridge.ExecuteSqlAsync("UPDATE [TerminalServer] SET [CertHash] = @certHash where [IP] = @ip", sqlParams);
                                } catch (Exception ex)
                                {
                                    Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Error", ex);
                                }
                            }
                        }
                    } catch (Exception ex)
                    {
                        Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Error", ex);
                    }
                }
            } catch (Exception ex)
            {
                Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Error", ex);
            }
        }
        public void SetRdpConnected(string server)
        {
            var conn = _connectList.Find(x => x.IP == server);
            if (conn != null)
                conn.IsConnected = true;
            ReloadDataSet();
        }

        #endregion

        #endregion

        #region прочие Методы
        private async Task CheckConnects()
        {
            if (await CheckConnectionsAsync())
            {
                ServerIsAlive_toolStripTextBox1.BackColor = Color.GreenYellow;
                ServerIsAlive_toolStripTextBox1.Text = @"Сервер доступен";
            }
            else
            {
                ServerIsAlive_toolStripTextBox1.BackColor = Color.Red;
                ServerIsAlive_toolStripTextBox1.Text = @"Сервер не доступен";
            }
            ReloadDataSet();
        }
        private void ReloadDataSet()
        {
            string sel = listBoxControl1.SelectedValue.ToString();
            listBoxControl1.DataSource = _connectList;
            listBoxControl1.SelectedValue = sel;
            ConnectRdp connect1 = _connectList.Find(x => x.IP == sel);
            Connection_Button1.Text = connect1.IsConnected ? "Отключиться" : "Подключитьcя";
        }
        private void SetIsConnected(bool value)
        {
            Connection_Button1.Text = value ? "Отключиться" : "Подключиться";
        }

        private void CreateRdpSession(ConnectRdp connect, XtraTabPage tab)
        {
            try
            {
                VaRRdpProtocol rdpProtocol = new RdpProtocolFactory().Build(Enums.RdpVersion.Highest);

                rdpProtocol.ConnectSetup = new ConnectionSetup(connect);
                rdpProtocol.ConnectInfo = GetConnectionInfo();


                tab.Name = (string)(tab.Tag = tab.Text = connect.Name);
                var panel = new PanelControl();
                panel.BorderStyle = BorderStyles.NoBorder;
                tab.Controls.Add(panel);
                panel.Dock = DockStyle.Fill;
                rdpProtocol.SetFormParent(this);
                rdpProtocol.SetParentControl(panel);

                if (rdpProtocol.Initialize())
                {
                    SetRegistryCertHash(connect);
                    rdpProtocol.Connect();
                    _protocols.Add(rdpProtocol);
                }
            } catch (Exception ex)
            {
                Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Ошибка создания RDP сессии с VaRRdpProtocol", ex);
            }
        }
        private void SetRegistryCertHash(ConnectRdp connect)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(connect.CertHash))
                {
                    RegistryKey key4 = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Terminal Server Client\Servers\" + connect.IP);
                    if (key4 != null)
                        key4.SetValue("CertHash", connect.CertHashObject, RegistryValueKind.Binary);
                }
            } catch (Exception ex)
            {
                Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Ошибка установки сертификата", ex);
            }
        }
        private ConnectionInfo GetConnectionInfo()
        {
            FileInfo fileConnect = new FileInfo(Path.Combine(Program.AppPath, "connect.json"));
            var connectionInfo = fileConnect.Exists ? JsonConvert.DeserializeObject<ConnectionInfo>(File.ReadAllText(fileConnect.FullName)) : new ConnectionInfo();
            return connectionInfo;
        }
        private void ReadCfg()
        {
            FullScreenToolStripMenuItem.Checked = !_cfg.IfRead("Setting", "Full") || _cfg.ReadBoolean("Setting", "Full");
            CloseToDisconectedToolStripMenuItem.Checked = _cfg.IfRead("Setting", "CloseByDisconnect") || _cfg.ReadBoolean("Setting", "CloseByDisconnect");
            MultiMonToolStripMenuItem.Checked = _cfg.ReadBoolean("Setting", "Multimon");
            свернутьВТрейПриПодключенииToolStripMenuItem.Checked = _cfg.ReadBoolean("Setting", "Tray");
        }
        private async Task VpnDisconnect()
        {
            if (Program.Servers.CurrentServer.VPNConnect)
                await RemoveCreatedVpn(Program.Servers.CurrentServer);
            _vpnUsed = false;
        }
        private async Task<List<ConnectRdp>> ReadConnectToRdp(Contact contact)
        {
            try
            {
                var list = new List<SqlParam>
                {
                    new () { Name = "@id", SqlDbTypeValue = (int)SqlDbType.Int, Value = contact.ID }
                };
                var contacts = await ApiBridge.QuerySqlAsync<Contact>("SELECT [VPNUser],[VPNPassword] FROM Contacts WHERE ID=@id", list);
                if (contacts.Any())
                {
                    contact.VPNUser = contacts[0].VPNUser;
                    contact.VPNPassword = contacts[0].VPNPassword;
                }
                List<ConnectRdp> connectRdp = await ApiBridge.QuerySqlAsync<ConnectRdp>(
                    @"SELECT TerminalServer.IP as IP, TerminalAccess.Domain as DomainRDP, TerminalAccess.Login as LoginRDP, TerminalAccess.Password as PasswordRDP, TerminalServer.NAME as Name, TerminalServer.CertHash as CertHash
FROM TerminalAccess INNER JOIN
 TerminalServer ON TerminalAccess.ServerID = TerminalServer.ID
WHERE (TerminalAccess.ContactID = @id) order by TerminalServer.NAME", list);
                return connectRdp;
            } catch
            {
                return new List<ConnectRdp>();
            }
        }
        #endregion

        #region  Очистка старого в реестре
        private void CleanOldRdp()
        {
            #region Clean TERMSRV Credential
            CredentialSet credential = new CredentialSet("TERMSRV*");
            credential.Load();
            foreach (Credential cred in credential)
                cred.Delete();
            #endregion

            #region Clean Registry Terminal Servers
            try
            {
                RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Terminal Server Client\Default");
                if (key != null)
                    foreach (string item in key.GetValueNames())
                    {
                        try
                        {
                            key.DeleteValue(item, false);
                        } catch (Exception ex)
                        {
                            Logs.Error(GetType(), MethodBase.GetCurrentMethod(),
                                "Ошибка delete regedit Terminal Server Default " + key + ": ", ex);
                        }
                    }
            } catch (Exception ex)
            {
                Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Ошибка delete regedit Terminal Server Default: ", ex);
            }
            try
            {
                RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Terminal Server Client\Servers");
                if (key != null)
                    foreach (string item in key.GetSubKeyNames())
                    {
                        try
                        {
                            key.DeleteSubKey(item, false);
                        } catch (Exception ex)
                        {
                            Logs.Error(GetType(), MethodBase.GetCurrentMethod(),
                                "Ошибка delete regedit Terminal Servers " + key + ": ", ex);
                        }
                    }
            } catch (Exception ex)
            {
                Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Ошибка delete regedit Terminal Servers: ", ex);
            }
            #endregion

            #region Clean Default RDP Files
            foreach (string fileP in SafeEnumerateFiles(Environment.ExpandEnvironmentVariables(@"%USERPROFILE%"), "*.rdp", SearchOption.AllDirectories))
                File.Delete(fileP);
            foreach (FileInfo file in new DirectoryInfo(Environment.ExpandEnvironmentVariables(@"%AppData%\Microsoft\Windows\Recent\AutomaticDestinations")).EnumerateFiles())
                file.Delete();
            #endregion

        }
        private static IEnumerable<string> SafeEnumerateFiles(string path, string searchPattern = "*.*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            var dirs = new Stack<string>();
            dirs.Push(path);

            while (dirs.Count > 0)
            {
                string currentDirPath = dirs.Pop();
                if (searchOption == SearchOption.AllDirectories)
                {
                    try
                    {
                        string[] subDirs = Directory.GetDirectories(currentDirPath);
                        foreach (string subDirPath in subDirs)
                        {
                            dirs.Push(subDirPath);
                        }
                    } catch (UnauthorizedAccessException)
                    {
                        continue;
                    } catch (DirectoryNotFoundException)
                    {
                        continue;
                    }
                }

                string[] files;
                try
                {
                    files = Directory.GetFiles(currentDirPath, searchPattern);
                } catch (UnauthorizedAccessException)
                {
                    continue;
                } catch (DirectoryNotFoundException)
                {
                    continue;
                }

                foreach (string filePath in files)
                {
                    yield return filePath;
                }
            }
        }
        #endregion

        #region Украшения формы
        private void XtraTabControl1_SelectedPageChanged(object sender, TabPageChangedEventArgs e)
        {
            try
            {
                if (e.Page is { Controls.Count: > 0 } tab)
                {
                    if (tab.Controls[0] is PanelControl { Controls.Count: > 0 } panel)
                        panel.Controls[0].Focus();
                }
            } catch (Exception ex)
            {
                Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Error", ex);
            }
        }
        private void XtraTabControl1_Click(object sender, EventArgs e)
        {
            try
            {
                if (sender is XtraTabControl control)
                {
                    XtraTabPage tab = control.SelectedTabPage;
                    if (tab is { Controls.Count: > 0 })
                        if (tab.Controls[0] is PanelControl { Controls.Count: > 0 } panel)
                        {
                            var findProtocol = _protocols.Find(x => x.GetParentControl() == panel);
                            if (findProtocol != null)
                                findProtocol.SetFocusRdp();
                        }
                }
            } catch (Exception ex)
            {
                Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Error", ex);
            }
        }
        private void ListBoxControl1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ListBoxControl box = sender as ListBoxControl;
                if (box != null)
                {
                    if (box.GetViewInfo() is BaseListBoxViewInfo vi)
                    {
                        BaseListBoxViewInfo.ItemInfo ii = vi.GetItemInfoByPoint(e.Location);
                        if (ii != null)
                            box.SelectedIndex = ii.Index;
                    }
                }

                подключитьсяToolStripMenuItem.Text = Connection_Button1.Text;
                if (box != null) contextMenuStrip1.Show(box, e.Location);
            }
        }
        private void ПодключитьсяToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConnectToRdp();
        }
        private void ОбновитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            refresh_toolStripButton2.PerformClick();
        }
        private void ПоIPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listBoxControl1.DataSource = _connectList.OrderBy(x => new Version(x.IP)).ToList();
        }
        private void ПоИмениToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listBoxControl1.DataSource = _connectList.OrderBy(x => x.Name).ToList();
        }
        private void ПоДоступностиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listBoxControl1.DataSource = _connectList.OrderBy(x => !x.IsAlive).ToList();
        }
        private void ПоПодключениямToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listBoxControl1.DataSource = _connectList.OrderBy(x => !x.IsConnected).ToList();
        }
        private void XtraTabControl1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                var hitInfo = xtraTabControl1.CalcHitInfo(e.Location);
                if (!(hitInfo.Page is { } tab)) return;
                xtraTabControl1.TabPages.Remove(tab);
            }
        }

        private bool _template;
        private void ПолнаяToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _template = true;
            listBoxControl1.Refresh();
        }
        private void КраткаяToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _template = false;
            listBoxControl1.Refresh();
        }
        private void ListBoxControl1_CustomItemTemplate(object sender, CustomItemTemplateEventArgs e)
        {
            e.Template = _template ? listBoxControl1.Templates[0] : listBoxControl1.Templates[1];
        }
        #endregion
        #region Проверки доступности сервера и создание/подключение VPN
        private async Task CheckAliveServerAndConnectVpn(IProgress<string> progress)
        {
            progress.Report("Проверяем соединение");
            if (await CheckConnectionsAsync())
                return;
            if (string.IsNullOrEmpty(_contact.VPNUser))
                return;

            await VpnManager.IpSecConnectionRemoveAsync("ipsec");
            await OpenVpnConnectionRemove(Program.Servers.CurrentServer);
            new WireGuardManager().ConnectionRemove();
            Program.Servers.SetFirstServer();
            do
            {
                Server server = Program.Servers.CurrentServer;
                progress.Report($"Пробуем подключиться через {server.Name}");
                if (await VpnCreateAndConnect(_contact, server))
                {
                    _vpnUsed = true;
                    server.VPNConnect = true;
                    if (await CheckConnectionsAsync(server.VPNType == VPNType.OpenVPN))
                    {
                        Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), $"{server.Name} через {server.VPNType} с {server.HostOrIP}: доступно {_connectList.Count(x => x.IsAlive)} из {_connectList.Count}!");
                        server.ServerAlive = true;
                        break;
                    }
                    Logs.Warn(GetType(), MethodBase.GetCurrentMethod(), $"{server.Name} через {server.VPNType} с {server.HostOrIP}: сервера не доступны!");
                    await RemoveCreatedVpn(server);
                    server.ServerAlive = false;
                }
                else server.VPNConnect = false;
            } while (Program.Servers.NextServer());
        }


        public async Task<bool> CheckConnectionsAsync(bool tt = false)
        {
            //TODO временно проверка
            Thread.Sleep(3000);
            if (!tt)
                return tt;
            Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), "Начало проверки соединений...");
            var tasks = _connectList.Select(async connect =>
            {
                connect.IsAlive = await IsOnlineAsync(connect.IPRDP, 25555, 2000);
                Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), $"Сервер {connect.IP}: {(connect.IsAlive ? "ONLINE" : "OFFLINE")}");
            });
            await Task.WhenAll(tasks);
            int aliveCount = _connectList.Count(x => x.IsAlive);
            if (aliveCount > 0)
            {
                Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), $"Прямое соединение: доступно {aliveCount} из {_connectList.Count}!");
                return true;
            }
            Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), "Ни одно соединение не доступно.");
            return false;
        }
        private async Task<bool> IsOnlineAsync(IPAddress ip, int port, int timeout = 2000)
        {
            if (ip == null) return false;

            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var cts = new CancellationTokenSource(timeout);
            try
            {
                var connectTask = socket.ConnectAsync(new IPEndPoint(ip, port));
                await Task.WhenAny(connectTask, Task.Delay(timeout, cts.Token));
                if (connectTask.IsCompleted && socket.Connected)
                {
                    return true;
                }
                return false;
            } catch (Exception)
            {
                return false;
            } finally
            {
                cts.Cancel();
            }
        }
        #region OpenVPN
        private async Task<bool> VpnCreateAndConnect(Contact contact, Server server)
        {
            switch (server.VPNType)
            {
                case VPNType.L2TP:
                    await VpnManager.IpSecConnectionCreateL2TpAsync("ipsec", server);
                    return await VpnIpSecConnect(contact, server);
                case VPNType.PPTP:
                    await VpnManager.IpSecConnectionCreatePptpAsync("ipsec", server);
                    return await VpnIpSecConnect(contact, server);
                case VPNType.OpenVPN:
                    return await OpenVpnConnect(contact, server);
                case VPNType.WireGuard:
                    if (OSVersionInfo.OSBits == OSVersionInfo.SoftwareArchitecture.Bit32)
                        return false;
                    statusStrip3.Visible = true;
                    WireGuardManager wireGuardConnect = new WireGuardManager(contact.Name, wgTransferTitle_toolStripStatusLabel1);
                    server.WireGuardConnect = wireGuardConnect;
                    return await wireGuardConnect.ConnectionCreateAndConnect();
            }
            return false;
        }
        private async Task RemoveCreatedVpn(Server server)
        {
            switch (server.VPNType)
            {
                case VPNType.L2TP:
                case VPNType.PPTP:
                    await VpnManager.IpSecConnectionRemoveAsync("ipsec");
                    break;
                case VPNType.OpenVPN:
                    await OpenVpnConnectionRemove(server);
                    break;
                case VPNType.WireGuard:
                    WireGuardManager wg = (WireGuardManager)server.WireGuardConnect;
                    statusStrip3.Visible = false;
                    wg.ConnectionRemove();
                    server.WireGuardConnect = null;
                    break;
            }
            _vpnUsed = false;
        }
        private async Task<bool> VpnIpSecConnect(Contact contact, Server server)
        {
            try
            {
                Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), $"{server.Name} через {server.VPNType}: соединение создано до {server.HostOrIP}!");
                await VpnManager.IpSecConnectAsync("ipsec", server.HostOrIP, contact.VPNUser, contact.VPNPassword);
                Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), $"{server.Name} через {server.VPNType}: соединение подключено с {server.HostOrIP}!");
                server.VPNUsed = true;
                Thread.Sleep(1000);
                return true;
            } catch (Exception ex)
            {
                await VpnManager.IpSecConnectionRemoveAsync("ipsec");
                Logs.Error(GetType(), MethodBase.GetCurrentMethod(), $"{server.Name} через {server.VPNType}: соединение оборвалось до {server.HostOrIP}! Соединение удалено!", ex);
                return false;
            }
        }
        private async Task<bool> OpenVpnConnect(Contact contact, Server server)
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string os = GetOsInfo();
            try
            {
                Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), $"{server.Name} через {server.VPNType}: соединение создано до {server.HostOrIP}!");
                if (!DownLoadOpenVpn(path, os))//download path
                    throw new Exception("Error DownLoadOpenVpn");
                string pathConfig = await CreateConfigOpenVpn(path, os, contact, server);
                if (!TapDriverInstaller.CreateAndInstallTapDevice(Path.Combine(path, $@"OpenVPN_{os}", "TAP", "driver", "OemVista.inf")))
                    throw new Exception("Error TapDriverManager.InstallDriverFromInf");
                if (!await OpenVpnConnection(server, pathConfig))
                    throw new Exception("Error OpenVpnConnection");
                Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), $"{server.Name} через {server.VPNType}: соединение подключено с {server.HostOrIP}!");
                server.VPNUsed = true;
                Thread.Sleep(1000);
                return true;
            } catch (Exception ex)
            {
                await OpenVpnConnectionRemove(server);
                Logs.Error(GetType(), MethodBase.GetCurrentMethod(), $"{server.Name} через {server.VPNType}: соединение оборвалось до {server.HostOrIP}! Соединение удалено!", ex);
                return false;
            }
        }
        private async Task<bool> OpenVpnConnection(Server server, string pathConfig)
        {
            try
            {
                OpenVpnManager openVpnManager = new OpenVpnManager(pathConfig);
                server.OpenVpnConnect = openVpnManager;
                openVpnManager.OnLogMessage += OpenVpnManager_OnLogMessage;
                return await openVpnManager.StartAsync();
            } catch (Exception ex)
            {
                Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Error", ex);
                return false;
            }
        }
        private void OpenVpnManager_OnLogMessage(string msg)
        {
            Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), msg);
        }
        private async Task OpenVpnConnectionRemove(Server server)
        {
            var openVpnManager = (OpenVpnManager)server.OpenVpnConnect;
            if (openVpnManager != null)
            {
                try
                {
                    await openVpnManager.StopAsync();
                    Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), "OpenVPN полностью остановлен.");
                } finally
                {
                    openVpnManager.OnLogMessage -= OpenVpnManager_OnLogMessage;
                    openVpnManager.Dispose();
                    server.OpenVpnConnect = null;
                }
            }
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(path)) return;
            string os = GetOsInfo();
            if (!TapDriverInstaller.UninstallAllTapAdapters())
                Logs.Warn(GetType(), MethodBase.GetCurrentMethod(), "TAP remove Error");
            if (Directory.Exists(Path.Combine(path, $@"OpenVPN_{os}")))
                Directory.Delete(Path.Combine(path, $@"OpenVPN_{os}"), true);
        }
        #region Проверка запущенной программы под пользователем. Не хочу удалять, хорошие методы. Для истории
        [Obsolete]
        private static void CheckRunProgramAndClose(string programName)
        {
            foreach (Process ps1 in Process.GetProcessesByName(programName))
                if (GetProcessUser(ps1, Environment.UserName))
                    ps1.Kill();
        }
        private static bool GetProcessUser(Process process, string userName)
        {
            IntPtr processHandle = IntPtr.Zero;
            try
            {
                OpenProcessToken(process.Handle, 8, out processHandle);
                WindowsIdentity wi = new WindowsIdentity(processHandle);
                string user = wi.Name;
                user = user.Contains(@"\") ? user.Substring(user.IndexOf(@"\", StringComparison.Ordinal) + 1) : user;
                return user == userName;
            } catch
            {
                return false;
            } finally
            {
                if (processHandle != IntPtr.Zero)
                {
                    CloseHandle(processHandle);
                }
            }
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);
        #endregion
        private static string GetOsInfo()
        {
            string os = "";
            switch (OSVersionInfo.Name)
            {
                case "Windows 7":
                    os = "win7";
                    break;
                //переход нужно проверить
                case "Windows 8":
                case "Windows 8.1":
                case "Windows 10":
                    os = "win10";
                    break;
            }
            switch (OSVersionInfo.OSBits)
            {
                case OSVersionInfo.SoftwareArchitecture.Bit64:
                    os += "x64";
                    break;
                case OSVersionInfo.SoftwareArchitecture.Bit32:
                    os += "x32";
                    break;
            }

            return os;
        }
        private bool DownLoadOpenVpn(string path, string os)
        {
            try
            {
                byte[] ba = Program.Servers.SqlData.GetFileOpen(os);
                if (ba != null)
                {
                    Directory.CreateDirectory(Path.Combine(path, "temp1"));
                    File.WriteAllBytes(Path.Combine(path, "temp1", "temp.7z"), ba);
                    Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), $"Получаем файл:{Path.Combine(path, "temp1", "temp.7z")}");
                    Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), "Архив скачан");
                    try
                    {
                        UpdateForApp.CallAndWait(Archive.ExtractFromArchive, Path.Combine(path, "temp1", "temp.7z"), path, 1);
                    } catch
                    {
                        UpdateForApp.CallAndWait(Archive.ExtractFromArchiveOld, Path.Combine(path, "temp1", "temp.7z"), path, 1);
                    }
                    Directory.Delete(Path.Combine(path, "temp1"), true);
                    Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), "Архив распакован и удален");
                    return true;
                }
                Logs.Warn(GetType(), MethodBase.GetCurrentMethod(), "Проблема в скачивании архива");
                return false;
            } catch (Exception e)
            {
                Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Проблема в скачивании архива", e);
                return false;
            }
        }
        private static async Task<string> CreateConfigOpenVpn(string path, string os, Contact contact, Server server)
        {
            string pathConfig = Path.Combine(path, $@"OpenVPN_{os}", "config", "client.ovpn");
            if (File.Exists(pathConfig))
                File.Delete(pathConfig);
            string config = await GetOpenVpnConfig(server.ApiUrl, server.ApiKey);
            string passphrase = await GetOpenVpnPassPhrase(server.ApiUrl, server.ApiKey);
            using (StreamWriter writer = new StreamWriter(pathConfig, false))
            {
                await writer.WriteLineAsync(config.Replace("@@IP@@", server.TcpConnectString.ipEndPoint.Address.ToString()));
                await writer.WriteLineAsync($"ca {Path.Combine(path, $"OpenVPN_{os}", "config", "cert_export_test-CA.crt").Replace(@"\", @"\\")}");
                await writer.WriteLineAsync($"cert {Path.Combine(path, $"OpenVPN_{os}", "config", "cert_export_test-client-ovpn-1.crt").Replace(@"\", @"\\")}");
                await writer.WriteLineAsync($"key {Path.Combine(path, $"OpenVPN_{os}", "config", "cert_export_test-client-ovpn-1.key").Replace(@"\", @"\\")}");
                await writer.WriteLineAsync($"--auth-user-pass {Path.Combine(path, $"OpenVPN_{os}", "config", "user-pwd.txt").Replace(@"\", @"\\")}");
                await writer.WriteLineAsync($"--askpass {Path.Combine(path, $"OpenVPN_{os}", "config", "keypass.txt").Replace(@"\", @"\\")}");
            }
            using (StreamWriter writer = new StreamWriter(Path.Combine(path, $@"OpenVPN_{os}", "config", "keypass.txt"), false))
            {
                await writer.WriteLineAsync(passphrase);
            }
            using (StreamWriter writer = new StreamWriter(Path.Combine(path, $@"OpenVPN_{os}", "config", "user-pwd.txt"), false))
            {
                await writer.WriteLineAsync(contact.VPNUser);
                await writer.WriteLineAsync(contact.VPNPassword);
            }

            return pathConfig;
        }
        private static HttpClient CreateHttpClient(string apiKey)
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(300) };
            client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
            return client;
        }
        private static async Task<string> GetOpenVpnConfig(string apiUrl, string apiKey)
        {
            var client = CreateHttpClient(apiKey);
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{apiUrl}/api/GetOpenVpnConfig/");
                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var text = await response.Content.ReadAsStringAsync();
                    return text;
                }

                throw new InvalidOperationException($"Server status is {response.StatusCode}");
            } catch (Exception ex)
            {
                Logs.Error(typeof(ConnectionForm), MethodBase.GetCurrentMethod(),
                    "Ошибка загрузки данных OpenVpnConfig", ex);
                throw;
            } finally
            {
                client.Dispose();

            }
        }
        private static async Task<string> GetOpenVpnPassPhrase(string apiUrl, string apiKey)
        {
            var client = CreateHttpClient(apiKey);
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{apiUrl}/api/GetOpenVpnPassPhrase/");
                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var text = await response.Content.ReadAsStringAsync();
                    return text;
                }
                throw new InvalidOperationException($"Server status is {response.StatusCode}");
            } catch (Exception ex)
            {
                Logs.Error(typeof(ConnectionForm), MethodBase.GetCurrentMethod(), "Ошибка загрузки данных GetOpenVpnPassPhrase", ex);
                throw;
            } finally
            {
                client.Dispose();

            }
        }
        #endregion
        #endregion
        #region панель закрытия
        private void Min_button1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                WindowState = FormWindowState.Minimized;
        }
        private void Close_button2_MouseDown(object sender, MouseEventArgs e)
        {
           Close();
        }
        private void Max_button1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (WindowState == FormWindowState.Maximized)
                {
                    panel1.Visible = false;
                    WindowState = FormWindowState.Normal;
                    FormBorderStyle = FormBorderStyle.Sizable;
                    //тут делаем full screen
                }
                else
                    WindowState = FormWindowState.Maximized;
            }
        }
        private void splitContainer1_Panel2_Resize(object sender, EventArgs e)
        {
            panel1.Location = new Point(splitContainer1.Panel2.Size.Width - 80, 0);

        }
        #endregion
        protected override void WndProc(ref Message m)
        {
            bool max = false;
            if (m.Msg == 0x00A3)
            {
                m.Msg = 0x0112;
                m.WParam = new IntPtr(0xF030);
                m.HWnd = new IntPtr(0x1203ec);
                m.LParam = new IntPtr(0x8606eb);
                max = true;
            }
            if (m.Msg == 0x0112) // WM_SYSCOMMAND
            {
                if (m.WParam == new IntPtr(0xF030))
                {
                    FormBorderStyle = FormBorderStyle.None;
                    panel1.Visible = true;
                }
            }
            base.WndProc(ref m);
            if (max)
                WindowState = FormWindowState.Maximized;
        }
        private void ToTray_button1_Click(object sender, EventArgs e)
        {
            ShowInTaskbar = false;
            notifyIcon1.Click += NotifyIcon1_Click;
            notifyIcon1.Icon = Properties.Resources._1511;
            notifyIcon1.Visible = true;
            notifyIcon1.Text = @"VaR";
            _formState = WindowState;
            WindowState = FormWindowState.Minimized;
            this.Visible = false;
        }
        private void ChangeScreen_simpleButton1_Click(object sender, EventArgs e)
        {
            if (Screen.AllScreens.Length > 1)
            {
                Screen[] sc = Screen.AllScreens;
                if (Equals(sc[0], Screen.FromControl(this)))
                {
                    _formState = WindowState;
                    WindowState = FormWindowState.Normal;
                    this.Location = new Point(sc[1].Bounds.X, sc[1].Bounds.Y);
                    WindowState = _formState;
                }
                else
                {
                    _formState = WindowState;
                    WindowState = FormWindowState.Normal;
                    this.Location = new Point(sc[0].Bounds.X, sc[0].Bounds.Y);
                    WindowState = _formState;
                }
            }
        }
        private void GC_Collect()
        {
            try
            {
                Logs.Info(GetType(), MethodBase.GetCurrentMethod(), "В памяти до очистки [" + SizeSuffix(GC.GetTotalMemory(false), 3) + "] ");
                Logs.Info(GetType(), MethodBase.GetCurrentMethod(), "Поколений на данный момент [" + GC.MaxGeneration + "]");
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Logs.Info(GetType(), MethodBase.GetCurrentMethod(), "GC.Collect()");
                Logs.Info(GetType(), MethodBase.GetCurrentMethod(), "В памяти после Collect [" + SizeSuffix(GC.GetTotalMemory(false), 3) + "] ");
            } catch (Exception ex)
            {
                Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Ошибка timerGC_Elapsed", ex);
            }
        }
        private static string SizeSuffix(long value, int decimalPlaces = 1)
        {
            if (value < 0)
            {
                return "-" + SizeSuffix(-value);
            }

            int i = 0;
            decimal dValue = value;
            while (Math.Round(dValue, decimalPlaces) >= 1000)
            {
                dValue /= 1024;
                i++;
            }
            // 1. Сначала формируем строку формата: "n1", "n2" и т.д.
            string formatString = "n" + decimalPlaces;

            // 2. Преобразуем число в строку по этому формату
            string formattedValue = dValue.ToString(formatString);

            // 3. Используем простую интерполяцию
            return $"{formattedValue} {SizeSuffixes[i]}";
            //return string.Format("{0:n" + decimalPlaces + "} {1}", dValue, SizeSuffixes[i]);
        }
        private static readonly string[] SizeSuffixes = ["bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"];

        private void настройкиСоединенияToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using ConnectInfoForm connectInfoForm = new ConnectInfoForm();
            connectInfoForm.ShowDialog(this);
        }
    }
}