using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kursovaya
{
    public partial class Authorization : Form
    {
        string conString = $"host={Properties.Settings.Default.host};uid={Properties.Settings.Default.uid};pwd={Properties.Settings.Default.pwd};database={Properties.Settings.Default.database};";

        public Authorization()
        {
            InitializeComponent();
            auth.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            close.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            auth.Enabled = false;
            textBox1.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            textBox2.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
        }

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

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.Text != "" && textBox2.Text != "")
            {
                auth.Enabled = true;
            }
            else
            {
                auth.Enabled = false;
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.Text != "" && textBox2.Text != "")
            {
                auth.Enabled = true;
            }
            else
            {
                auth.Enabled = false;
            }
        }

        private string GetHashPass(string password)
        {
            using (var sh2 = SHA256.Create())
            {
                var sh2byte = sh2.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(sh2byte).Replace("-", "").ToLower();
            }
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Разрешаем управляющие символы
            if (char.IsControl(e.KeyChar))
                return;

            // Английские буквы
            if ((e.KeyChar >= 'a' && e.KeyChar <= 'z') || (e.KeyChar >= 'A' && e.KeyChar <= 'Z'))
                return;

            // Цифры
            if (char.IsDigit(e.KeyChar))
                return;

            // Специальные символы
            char[] allowedSpecialChars = { '!', '@', '#', '$', '%', '^', '&', '*', '(', ')',
                                  '-', '_', '=', '+', '[', ']', '{', '}', ';', ':',
                                  ',', '.', '<', '>', '/', '?', '|', '\\', '~', '`' };

            if (allowedSpecialChars.Contains(e.KeyChar))
                return;

            // Запрещаем все остальные символы
            e.Handled = true;
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Разрешаем управляющие символы
            if (char.IsControl(e.KeyChar))
                return;

            // Английские буквы
            if ((e.KeyChar >= 'a' && e.KeyChar <= 'z') || (e.KeyChar >= 'A' && e.KeyChar <= 'Z'))
                return;

            // Цифры
            if (char.IsDigit(e.KeyChar))
                return;

            // Специальные символы
            char[] allowedSpecialChars = { '!', '@', '#', '$', '%', '^', '&', '*', '(', ')',
                                  '-', '_', '=', '+', '[', ']', '{', '}', ';', ':',
                                  ',', '.', '<', '>', '/', '?', '|', '\\', '~', '`' };

            if (allowedSpecialChars.Contains(e.KeyChar))
                return;

            // Запрещаем все остальные символы
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

        private void auth_Click(object sender, EventArgs e)
        {
            string password = textBox2.Text;

            using (var sh2 = SHA256.Create())
            {
                var sh2byte = sh2.ComputeHash(Encoding.UTF8.GetBytes(password));
                Console.WriteLine(password + BitConverter.ToString(sh2byte).Replace("-", "").ToLower());
            }

            try
            {
                string login = textBox1.Text;
                string hashPassword = string.Empty;
                string hashPasswordInDB = string.Empty;
                string rights = string.Empty;

                using (MySqlConnection con = new MySqlConnection(conString))
                {
                    using (MySqlCommand cmd = new MySqlCommand($"SELECT * FROM Users WHERE Login ='" + login + "';", con))
                    {
                        cmd.CommandType = CommandType.Text;

                        using (MySqlDataAdapter sda = new MySqlDataAdapter(cmd))
                        {
                            using (DataTable dt = new DataTable())
                            {
                                sda.Fill(dt);

                                //сравниваем пароли
                                //получаем хеш от пароля с формы
                                hashPassword = GetHashPass(textBox2.Text.ToString());

                                //и тот хеш, который хранится в БД
                                if (dt.Rows.Count > 0)
                                {
                                    Properties.Settings.Default.userName = dt.Rows[0].ItemArray.GetValue(1).ToString();
                                    hashPasswordInDB = dt.Rows[0].ItemArray.GetValue(3).ToString();
                                    rights = dt.Rows[0].ItemArray.GetValue(4).ToString();
                                }

                                //проверка на корректность введенного пароля
                                if (hashPassword.Equals(hashPasswordInDB))
                                {
                                    this.Visible = false;
                                    if (rights == "1")
                                    {
                                        Properties.Settings.Default.userRole = "Администратор";
                                        allowClose = true;
                                        this.Visible = false;
                                        MainFormAdmin mainFormAdmin = new MainFormAdmin();
                                        mainFormAdmin.ShowDialog();
                                        this.Close();
                                    }
                                    else if (rights == "2")
                                    {
                                        Properties.Settings.Default.userRole = "Менеджер";
                                        allowClose = true;
                                        this.Visible = false;
                                        MainFormMeneger mainFormMeneger = new MainFormMeneger();
                                        mainFormMeneger.ShowDialog();
                                        this.Close();
                                    }
                                    else if (rights == "3")
                                    {
                                        Properties.Settings.Default.userRole = "Директор";
                                        allowClose = true;
                                        this.Visible = false;
                                        MainFormDirector mainFormDirector = new MainFormDirector();
                                        mainFormDirector.ShowDialog();
                                        this.Close();
                                    }
                                    else
                                    {
                                        MessageBox.Show("У пользователя нет прав доступа", "Ошибка авторизации", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
                                }
                                else if (textBox1.Text == Properties.Settings.Default.userAdmin && textBox2.Text == Properties.Settings.Default.passwordAdmin) {
                                    Properties.Settings.Default.userRole = "Администратор";
                                    Properties.Settings.Default.userName = "По умолчанию";
                                    allowClose = true;
                                    this.Visible = false;
                                    MainFormAdmin mainFormAdmin = new MainFormAdmin();
                                    mainFormAdmin.ShowDialog();
                                    this.Close();
                                }
                                else
                                {
                                    textBox1.Text = "";
                                    MessageBox.Show("Введен неправильный логин или пароль.", "Ошибка авторизации", MessageBoxButtons.OK, MessageBoxIcon.Error);

                                    textBox1.Text = "";
                                    textBox2.Text = "";
                                }
                            }
                        }
                    }
                }
            }
            catch (IndexOutOfRangeException)
            {
                MessageBox.Show("Ошибка");
            }
            catch (InvalidOperationException)
            {
                MessageBox.Show("Ошибка");
            }
        }
    }
}
