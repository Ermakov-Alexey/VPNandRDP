using DevExpress.XtraEditors;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VaR
{
    public partial class JoinForm : XtraForm
    {
        private readonly Func<IProgress<string>, CancellationToken, Task> _operation;
        private string _caption = string.Empty; // Текущее значение прогресса
        private string _description = string.Empty;
        private CancellationTokenSource _cts;  // Объект для создания токена отмены
        private readonly SimpleButton _cancelButton;  // Кнопка "Отмена" или "Закрыть"
        public JoinForm(Func<IProgress<string>, CancellationToken, Task> operation)
        {
            InitializeComponent();
            Application.EnableVisualStyles();
            StartPosition = FormStartPosition.CenterParent; // Важно — центрировать относительно родителя
            ControlBox = false; // Отключить кнопки закрытия
            FormBorderStyle = FormBorderStyle.FixedDialog; // Чтобы не менять размер
            ShowInTaskbar = false; // Не показывать в панели задач
            _operation = operation;

            _cancelButton = cancelButton;
            if (_cancelButton != null)
            {
                // Обработчик нажатия кнопки "Отмена"
                _cancelButton.Click += (_, _) =>
                {
                    _cts?.Cancel(); // Отправляем запрос на отмену
                    _cancelButton.Enabled = false; // Отключаем кнопку, чтобы избежать повторных нажатий
                };
            }
        }
        protected override async void OnShown(EventArgs e)
        {
            try
            {
                base.OnShown(e);
                progressPanel1.Description = string.Empty;
                var progress = new Progress<string>(value =>  // Создаем объект для отслеживания прогресса
                {
                    var split = value.Split(['|'], StringSplitOptions.RemoveEmptyEntries);
                    _caption = split[0];
                    _description = split.Length > 1 ? split[1] : string.Empty;
                    progressPanel1.Caption = _caption;
                    progressPanel1.Description = _description;

                });
                _cts = new CancellationTokenSource(); // Создаем CancellationTokenSource
                try
                {
                    await _operation(progress, _cts.Token); // Запускаем операцию асинхронно
                } catch (OperationCanceledException)
                {
                    // Обрабатываем отмену (например, показываем сообщение)
                    MessageBox.Show(@"Операция отменена.", @"Отмена", MessageBoxButtons.OK, MessageBoxIcon.Information);
                } catch (Exception ex)
                {
                    LogsFile.Logs.Error(GetType(), MethodBase.GetCurrentMethod(), _operation.Method.Name, ex);
                    MessageBox.Show($@"Ошибка: {ex.Message}");
                } finally
                {
                    this.Close(); // Закрываем окно после завершения
                    _cts?.Dispose(); // Освобождаем ресурсы CancellationTokenSource
                    _cts = null;
                }
            } catch (Exception ex)
            {
                LogsFile.Logs.Error(GetType(), MethodBase.GetCurrentMethod(), "OnShown", ex);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (e.CloseReason == CloseReason.UserClosing) // Если форма закрывается пользователем
            {
                _cts?.Cancel();
            }
        }

        // Добавьте обработчик события FormClosed, чтобы убедиться, что все ресурсы освобождены:
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            _cts?.Dispose(); // Освобождаем ресурсы CancellationTokenSource
            _cts = null;
        }
    }
}
