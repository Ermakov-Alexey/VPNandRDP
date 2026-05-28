using ConnectLIbrary;
using DevExpress.XtraEditors;
using LogsFile;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VaR
{
    public partial class LoginForm : XtraForm
    {
        private List<Contact> _contacts;
        public Contact Contact;
        private SqlData _sqlData;
        public LoginForm()
        {
            InitializeComponent();
            Contact = null;
            pass_textBox.UseSystemPasswordChar = true;
        }
        private void UpdateTitles(string internalHeader)
        {
            titleLabel.Text = internalHeader;
        }
        private async void LoginForm_Shown(object sender, EventArgs e)
        {
            try
            {
                UpdateTitles(@" ");
                Enabled = false;
                do
                {
                    if (await IsOnlineAsync(Program.Servers.GetCurrentIPAddress, Program.Servers.GetCurrentPort))
                        break;
                }
                while (Program.Servers.NextServer());
                if (Program.Servers.CurrentServer == null)
                    UpdateTitles(@"Нет подключения к серверу, попробуйте позже");
                else
                    _sqlData = Program.Servers.SqlData;
                if (Program.Servers.TcpConnectString != null)
                {
                    UpdateTitles(@"Проверка обновления");
                    Thread tt = new Thread(() =>
                    {
                        try
                        {
                            UpdateForApp.CheckUpdate(true);
                        } catch (Exception ex)
                        {
                            Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Error", ex);
                        }
                    });
                    tt.Start();
                    tt.Join();

                    UpdateTitles(@" ");

                    login_textBox1.Enabled = true;
                    pass_textBox.Enabled = true;
                    vhod_button1.Enabled = true;
                    viewPass_checkBox1.Enabled = true;
                    do
                    {
                        try
                        {
                            _contacts = await ReadContact();
                        } catch (Exception ex)
                        {
                            Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Error", ex);
                        }
                        if (_contacts == null)
                        {
                            DialogResult dialogResult = MessageBox.Show(@"Ошибка связи с сервером. Повторить?", @"Ошибка", MessageBoxButtons.RetryCancel);
                            if (dialogResult == DialogResult.Cancel)
                            {
                                Contact = null;
                                Close();
                            }
                        }
                    } while (_contacts == null);

                    Contact = null;
                }
            } catch (Exception ex)
            {
                Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Error", ex);
            } finally
            {
                Enabled = true;
                login_textBox1.Focus();
            }
        }
        private void LoginForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.OK) return;
            Contact = null;
            Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), "Выход LoginForm");
        }
        private void Login_textBox1_TextChanged(object sender, EventArgs e)
        {
            if (login_textBox1.Text == "")
            {
                generate_button1.Visible = false;
                UpdateTitles(@" ");
                Contact = null;
                return;
            }
            if (_contacts.Count > 0)
            {
                Contact = login_textBox1.Text.Contains("@") ? _contacts.Find(x => x.Email == login_textBox1.Text) : _contacts.Find(x => x.Login == login_textBox1.Text);
            }
            if (Contact != null)
            {
                UpdateTitles(Contact.Name);
                generate_button1.Visible = true;
            }
            else
            {
                generate_button1.Visible = false;
                UpdateTitles(@" ");
                Contact = null;
            }
        }
        private async void Login_button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (Contact != null)
                {
                    if (!await ApiLoginAsync(Contact.ID, pass_textBox.Text))
                    {
                        MessageBox.Show(@"Неправильный пароль");
                        pass_textBox.SelectAll();
                    }
                    else
                    {
                        DialogResult = DialogResult.OK;
                        Logs.Trace(GetType(), MethodBase.GetCurrentMethod(), $"Выход LoginForm, логин/пароль подошёл, пользователь {Contact.Name}");
                        Close();
                    }
                }
                else
                    MessageBox.Show(@"Не правильно указан пользователь");
            } catch (Exception ex)
            {
                Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Error", ex);
            }
        }
        private async Task<bool> ApiLoginAsync(int userId, string password)
        {
            var list = new List<SqlParam>
            {
                new () { Name = "@userId", SqlDbTypeValue = (int)SqlDbType.Int, Value = userId },
                new () { Name = "@password", SqlDbTypeValue = (int)SqlDbType.NVarChar, Value = SqlData.GetMD5(password) }
            };
            var query = "SELECT 1 AS [Result] FROM [Contacts] where [ID]=@userId and [Password]=@password";
            var results = await ApiBridge.ScalarSqlAsync(query, list);
            return results != null && (int)results == 1;
        }
        private void ViewPass_checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            pass_textBox.UseSystemPasswordChar = !pass_textBox.UseSystemPasswordChar;
        }
        private void LoginForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((Keys)e.KeyChar == Keys.Escape)
            {
                Close();
                e.Handled = true;
            }
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

        #region oldIsOnline
        [Obsolete]
        private bool IsOnline(IPAddress ip, int port, int timeout = 1000)
        {
            if (ip != null)
            {
                IPEndPoint endPoint = new IPEndPoint(ip, port);
                Socket checkerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IAsyncResult result = checkerSocket.BeginConnect(endPoint, ConnectCallback, checkerSocket);
                if (!result.AsyncWaitHandle.WaitOne(timeout, false))
                {
                    checkerSocket.Close();
                    return false;
                }
                checkerSocket.Close();
                return true;
            }
            return false;
        }
        [Obsolete]
        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;
                client.EndConnect(ar);
            } catch (Exception ex)
            {
                Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "ERROR", ex);
            }
        }
        #endregion
        private async Task<List<Contact>> ReadContact()
        {
            List<Contact> contacts = await ApiBridge.QuerySqlAsync<Contact>(
            "SELECT ID, Name, Email FROM Contacts WHERE [Delete] = 'false' and [Password] is not NULL");
            return contacts.Count == 0 ? null : contacts;
        }
        private async void Generate_button1_Click(object sender, EventArgs e)
        {
            try
            {
                string newPass = GetRandomPasswordSecure(6);
                if (!_sqlData.SendMailData($"{newPass}:{Contact.Email}:VaR"))
                {
                    MessageBox.Show(@"Пароль не отправлен");
                }
                else
                {
                    await UpdatePassword(SqlData.GetMD5(newPass), Contact.Email);
                    MessageBox.Show(@"Вам на почту отправлен новый пароль");
                }
            } catch (Exception ex)
            {
                Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "Error", ex);
            }
        }
        private async Task UpdatePassword(string newPass, string email)
        {
            var list = new List<SqlParam>
            {
                new() { Name = "@newPass", SqlDbTypeValue = (int)SqlDbType.NVarChar, Value = newPass },
                new() { Name = "@email", SqlDbTypeValue = (int)SqlDbType.NVarChar, Value = email }
            };
            var query = "UPDATE [Contacts] SET [Password] = @newPass where [Email] = @email";
            await ApiBridge.ExecuteSqlAsync(query, list);
        }
        private static string GetRandomPasswordSecure(int pwdLength)
        {
            const string ch = "qwertyuiopasdfghjkzxcvbnmQWERTYUPASDFGHJKLZXCVBNM1234567890";
            var sb = new StringBuilder(pwdLength);
            using (var rng = RandomNumberGenerator.Create())
            {
                var buffer = new byte[sizeof(uint)];
                while (sb.Length < pwdLength)
                {
                    rng.GetBytes(buffer);
                    uint value = BitConverter.ToUInt32(buffer, 0);
                    sb.Append(ch[(int)(value % ch.Length)]);
                }
            }
            return sb.ToString();
        }
        private void close_simpleButton1_Click(object sender, EventArgs e)
        {
            Close();
        }
    }

}
