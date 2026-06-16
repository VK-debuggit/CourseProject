using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kursovaya
{
    public partial class Authorization : Form
    {
        private int failedAttempts = 0;
        private string conString = $"host={Properties.Settings.Default.host};uid={Properties.Settings.Default.uid};pwd={Properties.Settings.Default.pwd};database={Properties.Settings.Default.database};";
        private string currentCaptcha = "";
        private bool isBlocked = false;
        private System.Windows.Forms.Timer blockTimer;
        private int remainingSeconds;

        private bool _isReturnFromInactivity = false;
        private string _expectedUserName = "";
        private string _expectedUserRole = "";
        private bool _authorizedAsExpected = false;
        private Form _returnForm = null;

        public Authorization()
        {
            InitializeComponent();
            InitializeFormAppearance();
            SetupCaptchaPanel();
            SetupBlockTimer();
            SubscribeEvents();
        }

        //Настройка внешнего вида формы и расположения элементов
        private void InitializeFormAppearance()
        {
            auth.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            close.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button1.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            auth.Enabled = false;
            textBox1.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            textBox2.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            textBox3.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            pictureBox2.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);

            label1.Location = new Point(126, label1.Location.Y);
            textBox1.Location = new Point(126, textBox1.Location.Y);
            label2.Location = new Point(126, label2.Location.Y);
            textBox2.Location = new Point(126, textBox2.Location.Y);
            auth.Location = new Point(126, auth.Location.Y);
            close.Location = new Point(126, close.Location.Y);
        }

        //Настройка панели капчи
        private void SetupCaptchaPanel()
        {
            pictureBox2.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox2.Width = 350;
            pictureBox2.Height = 100;
            progressBar1.Visible = false;
            progressBar1.Maximum = 100;
            progressBar1.Value = 0;
            labelTimer.Visible = false;
            panel1.Visible = false;
            GenerateNewCaptcha();
        }

        //Настройка таймера блокировки
        private void SetupBlockTimer()
        {
            blockTimer = new System.Windows.Forms.Timer();
            blockTimer.Interval = 1000;
            blockTimer.Tick += BlockTimer_Tick;
        }

        //Подписка на события текстовых полей
        private void SubscribeEvents()
        {
            textBox1.TextChanged += CheckFieldsForValidation;
            textBox2.TextChanged += CheckFieldsForValidation;
            textBox3.TextChanged += CheckFieldsForValidation;
        }

        //Генерация новой капчи
        private void GenerateNewCaptcha()
        {
            currentCaptcha = GenerateCaptchaText(5);
            pictureBox2.Image = GenerateCaptchaImage(currentCaptcha, 350, 100);
        }

        //Функция генерации случайного текста для капчи
        private string GenerateCaptchaText(int length = 5)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            Random random = new Random();
            char[] stringChars = new char[length];

            for (int i = 0; i < length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            return new string(stringChars);
        }

        //Функция генерации графического изображения капчи
        private Image GenerateCaptchaImage(string captchaText, int width = 350, int height = 100)
        {
            Random random = new Random();
            Bitmap bitmap = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.White);

                for (int i = 0; i < 15; i++)
                {
                    int x1 = random.Next(width);
                    int y1 = random.Next(height);
                    int x2 = random.Next(width);
                    int y2 = random.Next(height);
                    g.DrawLine(new Pen(Color.FromArgb(200, 200, 200), 2), x1, y1, x2, y2);
                }

                Font font = new Font("Arial", 26, FontStyle.Bold | FontStyle.Italic);
                int startX = 20;
                int step = 40;

                for (int i = 0; i < captchaText.Length; i++)
                {
                    float angle = random.Next(-20, 20);
                    using (Matrix matrix = new Matrix())
                    {
                        matrix.RotateAt(angle, new PointF(startX + i * step, height / 2));
                        g.Transform = matrix;

                        Color color = Color.FromArgb(
                            random.Next(50, 200),
                            random.Next(50, 200),
                            random.Next(50, 200)
                        );
                        Brush brush = new SolidBrush(color);

                        g.DrawString(captchaText[i].ToString(), font, brush, startX + i * step, height / 3);
                        g.ResetTransform();
                    }
                }

                for (int i = 0; i < 300; i++)
                {
                    bitmap.SetPixel(random.Next(width), random.Next(height), Color.FromArgb(100, 100, 100));
                }
            }

            return bitmap;
        }

        //Обработчик тиков таймера блокировки
        private void BlockTimer_Tick(object sender, EventArgs e)
        {
            remainingSeconds--;

            if (remainingSeconds <= 0)
            {
                blockTimer.Stop();
                progressBar1.Visible = false;
                labelTimer.Visible = false;
                isBlocked = false;
                failedAttempts = 0;
                panel1.Visible = false;
                textBox1.Text = "";
                textBox2.Text = "";
                textBox3.Text = "";
                textBox1.Enabled = true;
                textBox2.Enabled = true;
                textBox3.Enabled = true;
                button1.Enabled = true;
                auth.Enabled = false;
                label1.Location = new Point(126, label1.Location.Y);
                textBox1.Location = new Point(126, textBox1.Location.Y);
                label2.Location = new Point(126, label2.Location.Y);
                textBox2.Location = new Point(126, textBox2.Location.Y);
                auth.Location = new Point(126, auth.Location.Y);
                close.Location = new Point(126, close.Location.Y);
                GenerateNewCaptcha();

                MessageBox.Show("Блокировка снята. Вы можете продолжить работу.",
                              "Разблокировка", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                int percent = (int)((10 - remainingSeconds) / 10.0 * 100);
                progressBar1.Value = percent;
                labelTimer.Text = $"Блокировка: {remainingSeconds} сек.";
            }
        }

        //Функция хэширования пароля
        private string GetHashPass(string password)
        {
            using (var sh2 = SHA256.Create())
            {
                var sh2byte = sh2.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(sh2byte).Replace("-", "").ToLower();
            }
        }

        //Проверка заполнения полей для активации кнопки авторизации
        private void CheckFieldsForValidation(object sender, EventArgs e)
        {
            if (isBlocked)
            {
                auth.Enabled = false;
                return;
            }

            bool loginPasswordFilled = !string.IsNullOrWhiteSpace(textBox1.Text) &&
                                      !string.IsNullOrWhiteSpace(textBox2.Text);

            if (panel1.Visible)
            {
                bool captchaFilled = !string.IsNullOrWhiteSpace(textBox3.Text);
                auth.Enabled = loginPasswordFilled && captchaFilled;
            }
            else
            {
                auth.Enabled = loginPasswordFilled;
            }
        }

        private bool allowClose = false;

        //Обработчик закрытия формы
        private void Authorization_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.ApplicationExitCall)
            {
                return;
            }

            if (!allowClose)
            {
                e.Cancel = true;
            }
        }

        //Ограничение ввода в поле логина
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar))
                return;

            if ((e.KeyChar >= 'a' && e.KeyChar <= 'z') || (e.KeyChar >= 'A' && e.KeyChar <= 'Z'))
                return;

            if (char.IsDigit(e.KeyChar))
                return;

            char[] allowedSpecialChars = { '!', '@', '#', '$', '%', '^', '&', '*', '(', ')',
                                  '-', '_', '=', '+', '[', ']', '{', '}', ';', ':',
                                  ',', '.', '<', '>', '/', '?', '|', '\\', '~', '`' };

            if (allowedSpecialChars.Contains(e.KeyChar))
                return;

            e.Handled = true;
        }

        //Ограничение ввода в поле пароля
        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar))
                return;

            if ((e.KeyChar >= 'a' && e.KeyChar <= 'z') || (e.KeyChar >= 'A' && e.KeyChar <= 'Z'))
                return;

            if ((e.KeyChar >= 'а' && e.KeyChar <= 'я') || (e.KeyChar >= 'А' && e.KeyChar <= 'Я'))
                return;

            if (char.IsDigit(e.KeyChar))
                return;

            char[] allowedSpecialChars = { '!', '@', '#', '$', '%', '^', '&', '*', '(', ')',
                                  '-', '_', '=', '+', '[', ']', '{', '}', ';', ':',
                                  ',', '.', '<', '>', '/', '?', '|', '\\', '~', '`' };

            if (allowedSpecialChars.Contains(e.KeyChar))
                return;

            e.Handled = true;
        }

        //Обработчик кнопки закрытия приложения
        private void close_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Вы действительно хотите закрыть приложение?", "Сообщение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                BackupManager.CreateBackupOnExit();
                allowClose = true;
                Application.Exit();
            }
        }

        //Обработчик кнопки обновления капчи
        private void button1_Click(object sender, EventArgs e)
        {
            if (!isBlocked && panel1.Visible)
            {
                GenerateNewCaptcha();
                textBox3.Clear();
            }
        }

        //Обработчик кнопки авторизации
        private void auth_Click(object sender, EventArgs e)
        {
            if (isBlocked)
            {
                MessageBox.Show("Система заблокирована! Подождите.", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string login = textBox1.Text;
            string hashPassword = GetHashPass(textBox2.Text);
            bool loginSuccess = false;

            //Проверка авторизации через учетную запись "По умолчанию"
            if (textBox1.Text == Properties.Settings.Default.userAdmin &&
                textBox2.Text == Properties.Settings.Default.passwordAdmin)
            {
                Properties.Settings.Default.userRole = "Администратор";
                Properties.Settings.Default.userName = "По умолчанию";
                Properties.Settings.Default.Save();

                if (_isReturnFromInactivity)
                {
                    HandleReturnAfterInactivity("По умолчанию", "Администратор");
                    return;
                }

                allowClose = true;
                this.Visible = false;
                MainFormAdmin mainFormAdmin = new MainFormAdmin();
                mainFormAdmin.ShowDialog();
                this.Close();
                return;
            }

            //Проверка авторизации через базу данных
            try
            {
                using (MySqlConnection con = new MySqlConnection(conString))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand($"SELECT * FROM Users WHERE Login = @login", con))
                    {
                        cmd.Parameters.AddWithValue("@login", login);

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string hashPasswordInDB = reader["Password"].ToString();
                                string rights = reader["IdRole"].ToString();
                                Properties.Settings.Default.userName = reader["FullName"].ToString();

                                if (hashPassword.Equals(hashPasswordInDB))
                                {
                                    loginSuccess = true;
                                    this.Visible = false;

                                    string role = "";
                                    if (rights == "1") role = "Администратор";
                                    else if (rights == "2") role = "Менеджер";
                                    else if (rights == "3") role = "Директор";

                                    Properties.Settings.Default.userRole = role;
                                    Properties.Settings.Default.userName = reader["FullName"].ToString();
                                    Properties.Settings.Default.Save();

                                    if (_isReturnFromInactivity)
                                    {
                                        HandleReturnAfterInactivity(Properties.Settings.Default.userName, role);
                                        return;
                                    }

                                    if (rights == "1")
                                    {
                                        Properties.Settings.Default.userRole = "Администратор";
                                        allowClose = true;
                                        MainFormAdmin mainFormAdmin = new MainFormAdmin();
                                        mainFormAdmin.ShowDialog();
                                        this.Close();
                                    }
                                    else if (rights == "2")
                                    {
                                        Properties.Settings.Default.userRole = "Менеджер";
                                        allowClose = true;
                                        MainFormMeneger mainFormMeneger = new MainFormMeneger();
                                        mainFormMeneger.ShowDialog();
                                        this.Close();
                                    }
                                    else if (rights == "3")
                                    {
                                        Properties.Settings.Default.userRole = "Директор";
                                        allowClose = true;
                                        MainFormDirector mainFormDirector = new MainFormDirector();
                                        mainFormDirector.ShowDialog();
                                        this.Close();
                                    }
                                    else
                                    {
                                        MessageBox.Show("У пользователя нет прав доступа", "Ошибка авторизации",
                                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            //Обработка неудачных попыток авторизации
            if (panel1.Visible)
            {
                if (textBox3.Text != currentCaptcha)
                {
                    StartBlocking();
                    return;
                }
                else if (!loginSuccess)
                {
                    MessageBox.Show("Введен неправильный логин или пароль!", "Ошибка авторизации",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    StartBlocking();
                    return;
                }
            }
            else
            {
                if (!loginSuccess)
                {
                    failedAttempts = 1;
                    MessageBox.Show("Введен неправильный логин или пароль.",
                                  "Ошибка авторизации", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    panel1.Visible = true;

                    label1.Location = new Point(16, label1.Location.Y);
                    textBox1.Location = new Point(16, textBox1.Location.Y);
                    label2.Location = new Point(16, label2.Location.Y);
                    textBox2.Location = new Point(16, textBox2.Location.Y);
                    auth.Location = new Point(16, auth.Location.Y);
                    close.Location = new Point(16, close.Location.Y);

                    GenerateNewCaptcha();
                    textBox3.Clear();
                    textBox1.Text = "";
                    textBox2.Text = "";
                    auth.Enabled = false;
                    return;
                }
            }
        }

        //Функция запуска блокировки системы
        private void StartBlocking()
        {
            isBlocked = true;
            remainingSeconds = 10;

            textBox1.Enabled = false;
            textBox2.Enabled = false;
            textBox3.Enabled = false;
            button1.Enabled = false;
            auth.Enabled = false;

            progressBar1.Visible = true;
            progressBar1.Value = 0;
            labelTimer.Visible = true;
            labelTimer.Text = $"Блокировка: {remainingSeconds} сек.";

            MessageBox.Show("Введены неверные данные! Система заблокирована на 10 секунд.",
                          "Блокировка", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            blockTimer.Start();
        }

        //Установка флага возврата после неактивности
        public void SetReturnAfterInactivity(Form returnForm, string expectedUserName, string expectedUserRole)
        {
            _isReturnFromInactivity = true;
            _returnForm = returnForm;
            _expectedUserName = expectedUserName;
            _expectedUserRole = expectedUserRole;
        }

        //Проверка успешности авторизации при возврате
        public bool IsAuthorizedAsExpected()
        {
            return _authorizedAsExpected;
        }

        //Обработка возврата после периода неактивности
        private void HandleReturnAfterInactivity(string currentUserName, string currentUserRole)
        {
            _authorizedAsExpected = (currentUserName == _expectedUserName &&
                                    currentUserRole == _expectedUserRole);

            allowClose = true;

            if (_authorizedAsExpected)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show(
                    $"Вы вошли как {currentUserName} ({currentUserRole}).\n" +
                    "Предыдущая форма будет закрыта.",
                    "Смена пользователя",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                if (_returnForm != null && !_returnForm.IsDisposed)
                {
                    _returnForm.Close();
                }
                this.DialogResult = DialogResult.OK;
                OpenMainFormByRole(currentUserRole);
            }
        }

        //Открытие главной формы в зависимости от роли пользователя
        private void OpenMainFormByRole(string role)
        {
            if (role == "Администратор")
            {
                this.Visible = false;
                MainFormAdmin mainFormAdmin = new MainFormAdmin();
                mainFormAdmin.ShowDialog();
                this.Close();
            }
            else if (role == "Менеджер")
            {
                this.Visible = false;
                MainFormMeneger mainFormMeneger = new MainFormMeneger();
                mainFormMeneger.ShowDialog();
                this.Close();
            }
            else if (role == "Директор")
            {
                this.Visible = false;
                MainFormDirector mainFormDirector = new MainFormDirector();
                mainFormDirector.ShowDialog();
                this.Close();
            }
        }

        //Установка английской раскладки при установке курсора в поле логина
        private void textBox1_Enter(object sender, EventArgs e)
        {
            foreach (InputLanguage lang in InputLanguage.InstalledInputLanguages)
            {
                if (lang.Culture.TwoLetterISOLanguageName == "en")
                {
                    InputLanguage.CurrentInputLanguage = lang;
                    break;
                }
            }
        }

        //Установка английской раскладки при установке курсора в поле капчи
        private void textBox3_Enter(object sender, EventArgs e)
        {
            foreach (InputLanguage lang in InputLanguage.InstalledInputLanguages)
            {
                if (lang.Culture.TwoLetterISOLanguageName == "en")
                {
                    InputLanguage.CurrentInputLanguage = lang;
                    break;
                }
            }
        }
    }
}