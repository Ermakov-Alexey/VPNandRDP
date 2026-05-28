using ConnectLIbrary;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using LogsFile;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace VaR
{
    public partial class FullScreenForm : Form
    {
        public ConnectRdp Connect;
        public bool FormClose;
        public bool Multimon;
        private Screen _screen;
        private VaRRdpProtocol _protocol;
        public FullScreenForm(ConnectRdp cc, bool multiMon, Screen sc, VaRRdpProtocol pr = null)
        {
            InitializeComponent();
            Multimon = multiMon;
            Connect = cc;
            _screen = sc;
            _protocol = pr;
            FormClose = false;
        }
        private void FullScreenForm_Shown(object sender, EventArgs e)
        {
            if (Screen.AllScreens.Length > 1 && Multimon)
            {
                Screen scP = Screen.PrimaryScreen;
                Screen scS = Screen.AllScreens.ToList().Find(a => a.Primary == false);
                Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), $@"Screen Primary=({scP.Bounds})");
                Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), $@"Screen Secondary=({scS.Bounds})");



                int x = scP.Bounds.X < scS.Bounds.X ? scP.Bounds.X : scS.Bounds.X;
                int y = scP.Bounds.Y < scS.Bounds.Y ? scP.Bounds.Y : scS.Bounds.Y;
                Location = new Point(x, y);


                int wid = scP.Bounds.Width + scS.Bounds.Width;
                int hei = scP.Bounds.Height;
                Size = new Size(wid, hei);
            }
            else
            {
                Location = new Point(_screen.Bounds.X, _screen.Bounds.Y);
                Size = _screen.Bounds.Size;
            }
            Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), $@"Location=({Location}) Size=({Size})");
            TopMost = true;
            label1.Text = Connect.Name;
            panel1.Width = label1.Width + 62;
            Text = Connect.Name;
            if (_protocol == null)
                CreateRdpSession();
            else
            {
                Tag = Connect.Name;
                _protocol.SetFormParent(this);
                Controls.Add(_protocol.GetParentControl());
            }
            RdFocus();
        }
        private void CreateRdpSession()
        {
            try
            {
                VaRRdpProtocol rdpProtocol = new RdpProtocolFactory().Build(Enums.RdpVersion.Highest);

                rdpProtocol.ConnectSetup = new ConnectionSetup(Connect);
                rdpProtocol.ConnectInfo = GetConnectionInfo();

                Tag = Connect.Name;
                var panel = new PanelControl();
                panel.BorderStyle = BorderStyles.NoBorder;
                Controls.Add(panel);
                panel.Dock = DockStyle.Fill;
                rdpProtocol.SetFormParent(this);
                rdpProtocol.SetParentControl(panel);

                if (rdpProtocol.Initialize())
                {
                    SetRegistryCertHash(Connect);
                    rdpProtocol.Connect();
                    _protocol = rdpProtocol;
                }
            } catch (Exception ex)
            {
                Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Ошибка создания RDP сессии", ex);
            }
        }
        private ConnectionInfo GetConnectionInfo()
        {
            FileInfo fileConnect = new FileInfo(Path.Combine(Program.AppPath, "connect.json"));
            var connectionInfo = fileConnect.Exists ? JsonConvert.DeserializeObject<ConnectionInfo>(File.ReadAllText(fileConnect.FullName)) : new ConnectionInfo();
            connectionInfo.Multimon = Multimon;
            return connectionInfo;
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

        #region public event
        public void SetOnDisconnected(string server)
        {
            try
            {
                //TODO проверить попадание
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
            if (!_reconnect)
            {
                Close();
            }
            _reconnect = false;
            //Close();
        }
        public void SetRdpConnected()
        {
            Connect.IsConnected = true;
        }
        public async void SetOnAuthenticationWarningDismissed()
        {
            try
            {
                try
                {
                    RegistryKey key4 = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Terminal Server Client\Servers\" + Connect.IP);
                    if (key4 != null)
                    {
                        byte[] key = (byte[])key4.GetValue("CertHash");
                        if (!key.SequenceEqual(Connect.CertHashObject))
                        {
                            Connect.ByteArrayToString(key);
                            try
                            {
                                List<SqlParam> sqlParams =
                                [
                                    new()
                                        {
                                            Name = "@certHash", SqlDbTypeValue = (int)SqlDbType.NVarChar,
                                            Value = Connect.CertHash
                                        },

                                        new()
                                        {
                                            Name = "@ip", SqlDbTypeValue = (int)SqlDbType.NVarChar,
                                            Value = Connect.IP
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
            } catch (Exception ex)
            {
                Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Error", ex);
            }
        }
        #endregion

        #region панель формы
        private void Panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (_mouseDown)
            {
                int deltaX = e.X - _mousePos.X;
                //int deltaY = e.Y - _mousePos.Y;
                panel1.Location = new Point(panel1.Left + deltaX, panel1.Top /* + deltaY */);
            }
        }
        Point _mousePos;
        bool _mouseDown;
        private void Panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _mouseDown = true;
                _mousePos = new Point(e.X, e.Y);
            }
        }
        private void Panel1_MouseUp(object sender, MouseEventArgs e)
        {
            if (_mouseDown)
            {
                _mouseDown = false;
            }
        }
        private void Label1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _mouseDown = true;
                _mousePos = new Point(e.X, e.Y);
            }
        }
        private void Label1_MouseUp(object sender, MouseEventArgs e)
        {
            if (_mouseDown)
            {
                _mouseDown = false;
            }
        }
        private void Label1_MouseMove(object sender, MouseEventArgs e)
        {
            if (_mouseDown)
            {
                int deltaX = e.X - _mousePos.X;
                //int deltaY = e.Y - _mousePos.Y;
                panel1.Location = new Point(panel1.Left + deltaX, panel1.Top /* + deltaY */);
            }
        }
        private void Min_button1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                this.WindowState = FormWindowState.Minimized;
        }
        private void Max_button1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                //TODO попробуем передать RDP
                //_protocol.Disconnect();
                //_protocol.Close();
                Close();
            }
        }

        public VaRRdpProtocol GetProtocol()
        {
            return _protocol;
        }
        private void Close_button2_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                FormClose = true;
                Connect.IsConnected = false;
                Close();
            }
        }
        private void ВосстановитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //TODO попробуем передать RDP
            //_protocol.Disconnect();
            //_protocol.Close();
            Close();
        }
        private void СвернутьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }
        private void ЗакрытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormClose = true;
            Connect.IsConnected = false;
            Close();
        }
        #endregion


        private FormWindowState _lastWindowState = FormWindowState.Minimized;
        private void FullScreenForm_Resize(object sender, EventArgs e)
        {
            if (WindowState != _lastWindowState)
            {
                _lastWindowState = WindowState;
                if (WindowState == FormWindowState.Maximized)
                    RdFocus();
                if (WindowState == FormWindowState.Normal)
                    RdFocus();
            }
        }

        private void RdFocus()
        {
            _protocol?.SetFocusRdp();
        }

        private void НаДругойЭкранToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!Multimon)
            {
                if (Screen.AllScreens.Length > 1)
                {
                    Screen[] sc = Screen.AllScreens;
                    if (Equals(sc[0], Screen.FromControl(this)))
                    {
                        Location = new Point(sc[1].Bounds.X, sc[1].Bounds.Y);
                        _screen = sc[0];
                    }
                    else
                    {
                        Location = new Point(sc[0].Bounds.X, sc[0].Bounds.Y);
                        _screen = sc[1];
                    }
                }
            }
            else
            {
                panel1.Location = panel1.Location.X > (Size.Width / 2) ? panel1.Location with { X = panel1.Location.X - (Size.Width / 2) } : panel1.Location with { X = panel1.Location.X + (Size.Width / 2) };
            }
        }
        private bool _reconnect;
        private void OneTwoScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Screen.AllScreens.Length <= 1) return;

            _reconnect = true;
            _protocol.Disconnect();
            if (Multimon)
            {
                Location = new Point(_screen.Bounds.X, _screen.Bounds.Y);
                Size = _screen.Bounds.Size;
                Multimon = false;
                _protocol.ChangeMultimon(Multimon);
                if (panel1.Location.X > Size.Width)
                    panel1.Location = panel1.Location with { X = panel1.Location.X - Size.Width };
            }
            else
            {
                Screen scP = Screen.PrimaryScreen;
                Screen scS = Screen.AllScreens.ToList().Find(a => a.Primary == false);
                Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), $@"Screen Primary=({scP.Bounds})");
                Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), $@"Screen Secondary=({scS.Bounds})");


                int x = scP.Bounds.X < scS.Bounds.X ? scP.Bounds.X : scS.Bounds.X;
                int y = scP.Bounds.Y < scS.Bounds.Y ? scP.Bounds.Y : scS.Bounds.Y;
                Location = new Point(x, y);


                int wid = scP.Bounds.Width + scS.Bounds.Width;
                int hei = scP.Bounds.Height;
                Size = new Size(wid, hei);
                Multimon = true;
                _protocol.ChangeMultimon(Multimon);
            }

            Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), $@"Location=({Location}) Size=({Size})");
            _protocol.Connect();
        }
    }
}
