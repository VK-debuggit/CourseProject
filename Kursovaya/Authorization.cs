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
        int failedAttempts = 0; // Счетчик НЕУДАЧНЫХ попыток (0 или 1)
        string conString = $"host={Properties.Settings.Default.host};uid={Properties.Settings.Default.uid};pwd={Properties.Settings.Default.pwd};database={Properties.Settings.Default.database};";
        private string currentCaptcha = "";
        private bool isBlocked = false;
        private System.Windows.Forms.Timer blockTimer;
        private int remainingSeconds;

        public Authorization()
        {
            InitializeComponent();
            auth.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            close.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button1.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            auth.Enabled = false;
            textBox1.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            textBox2.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            textBox3.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            pictureBox2.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);

            // Настройка PictureBox для капчи
            pictureBox2.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox2.Width = 350;
            pictureBox2.Height = 100;

            // Настройка ProgressBar
            progressBar1.Visible = false;
            progressBar1.Maximum = 100;
            progressBar1.Value = 0;
            labelTimer.Visible = false;

            // Настройка таймера
            blockTimer = new System.Windows.Forms.Timer();
            blockTimer.Interval = 1000;
            blockTimer.Tick += BlockTimer_Tick;

            // Изначально панель с капчей СКРЫТА
            panel1.Visible = false;

            // Подписываемся на события
            textBox1.TextChanged += CheckFieldsForValidation;
            textBox2.TextChanged += CheckFieldsForValidation;
            textBox3.TextChanged += CheckFieldsForValidation;

            // Генерируем капчу (но она пока не видна)
            GenerateNewCaptcha();
        }

        // ========== ГЕНЕРАЦИЯ КАПЧИ ==========

        private void GenerateNewCaptcha()
        {
            currentCaptcha = GenerateCaptchaText(5);
            pictureBox2.Image = GenerateCaptchaImage(currentCaptcha, 350, 100);
        }

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

        private Image GenerateCaptchaImage(string captchaText, int width = 350, int height = 100)
        {
            Random random = new Random();
            Bitmap bitmap = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.White);

                // Рисуем шум (случайные линии)
                for (int i = 0; i < 15; i++)
                {
                    int x1 = random.Next(width);
                    int y1 = random.Next(height);
                    int x2 = random.Next(width);
                    int y2 = random.Next(height);
                    g.DrawLine(new Pen(Color.FromArgb(200, 200, 200), 2), x1, y1, x2, y2);
                }

                // Рисуем текст капчи
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

                // Добавляем точки-шум
                for (int i = 0; i < 300; i++)
                {
                    bitmap.SetPixel(random.Next(width), random.Next(height), Color.FromArgb(100, 100, 100));
                }
            }

            return bitmap;
        }

        // ========== ТАЙМЕР БЛОКИРОВКИ ==========

        private void BlockTimer_Tick(object sender, EventArgs e)
        {
            remainingSeconds--;

            if (remainingSeconds <= 0)
            {
                // Завершаем блокировку
                blockTimer.Stop();
                progressBar1.Visible = false;
                labelTimer.Visible = false;
                isBlocked = false;

                // ВОССТАНАВЛИВАЕМ СОСТОЯНИЕ ПОСЛЕ БЛОКИРОВКИ:
                // 1. Сбрасываем счетчик попыток
                failedAttempts = 0;
                // 2. Скрываем панель с капчей
                panel1.Visible = false;
                // 3. Очищаем поля
                textBox1.Text = "";
                textBox2.Text = "";
                textBox3.Text = "";
                // 4. Разблокируем поля
                textBox1.Enabled = true;
                textBox2.Enabled = true;
                textBox3.Enabled = true;
                button1.Enabled = true;
                auth.Enabled = false;
                // 5. Генерируем новую капчу (но она не видна)
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

        // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========

        private string GetHashPass(string password)
        {
            using (var sh2 = SHA256.Create())
            {
                var sh2byte = sh2.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(sh2byte).Replace("-", "").ToLower();
            }
        }

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

        private void ResetFormFields()
        {
            textBox1.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
            auth.Enabled = false;
        }

        private void StartBlocking()
        {
            isBlocked = true;
            remainingSeconds = 10;

            // Блокируем все поля
            textBox1.Enabled = false;
            textBox2.Enabled = false;
            textBox3.Enabled = false;
            button1.Enabled = false;
            auth.Enabled = false;

            // Показываем прогресс
            progressBar1.Visible = true;
            progressBar1.Value = 0;
            labelTimer.Visible = true;
            labelTimer.Text = $"Блокировка: {remainingSeconds} сек.";

            MessageBox.Show("Введены неверные данные! Система заблокирована на 10 секунд.",
                          "Блокировка", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            blockTimer.Start();
        }

        // ========== ОБРАБОТЧИКИ СОБЫТИЙ ==========

        private bool allowClose = false;

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

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
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

        private void close_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Вы действительно хотите закрыть приложение?", "Сообщение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                allowClose = true;
                Application.Exit();
            }
        }

        // Кнопка обновления капчи
        private void button1_Click(object sender, EventArgs e)
        {
            if (!isBlocked && panel1.Visible)
            {
                GenerateNewCaptcha();
                textBox3.Clear();
            }
        }

        // Кнопка авторизации
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

            // Проверка дефолтного админа
            if (textBox1.Text == Properties.Settings.Default.userAdmin &&
                textBox2.Text == Properties.Settings.Default.passwordAdmin)
            {
                Properties.Settings.Default.userRole = "Администратор";
                Properties.Settings.Default.userName = "По умолчанию";
                allowClose = true;
                this.Visible = false;
                MainFormAdmin mainFormAdmin = new MainFormAdmin();
                mainFormAdmin.ShowDialog();
                this.Close();
                return;
            }

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
                                string hashPasswordInDB = reader["HashPassword"].ToString();
                                string rights = reader["UserRole"].ToString();
                                Properties.Settings.Default.userName = reader["FullName"].ToString();

                                if (hashPassword.Equals(hashPasswordInDB))
                                {
                                    loginSuccess = true;
                                    this.Visible = false;
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

            // ========== НЕУДАЧНАЯ ПОПЫТКА АВТОРИЗАЦИИ ==========

            // Если капча видна - проверяем ВСЕ поля
            if (panel1.Visible)
            {
                // Проверяем капчу
                if (textBox3.Text != currentCaptcha)
                {
                    MessageBox.Show("Неверный код с картинки!", "Ошибка капчи",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    StartBlocking(); // БЛОКИРУЕМ
                    return;
                }
                // Проверяем логин/пароль
                else if (!loginSuccess)
                {
                    MessageBox.Show("Введен неправильный логин или пароль!", "Ошибка авторизации",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    StartBlocking(); // БЛОКИРУЕМ
                    return;
                }
            }
            else // Капча НЕ видна - это первая попытка
            {
                if (!loginSuccess)
                {
                    // Первая неудачная попытка - показываем капчу
                    failedAttempts = 1;
                    MessageBox.Show("Введен неправильный логин или пароль.",
                                  "Ошибка авторизации", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    // ПОКАЗЫВАЕМ КАПЧУ
                    panel1.Visible = true;
                    GenerateNewCaptcha();
                    textBox3.Clear();
                    ResetFormFields();
                    return;
                }
            }
        }
    }
}