using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kursovaya
{
    public partial class ViewingMenu : Form
    {
        string conString = $"host={Properties.Settings.Default.host};uid={Properties.Settings.Default.uid};pwd={Properties.Settings.Default.pwd};database={Properties.Settings.Default.database};";
        private Timer inactivityTimer;
        private int inactivityTimeout;

        public ViewingMenu()
        {
            InitializeComponent();

            FillSort();
            FillFilterCategories();
            FillFilterEvents();
            FillDataGridView();

            inactivityTimeout = Properties.Settings.Default.InactivityTimeout * 1000;
            inactivityTimer = new Timer();
            inactivityTimer.Interval = inactivityTimeout;
            inactivityTimer.Tick += InactivityTimer_Tick;
            inactivityTimer.Start();

            button2.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button3.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            dataGridView1.BackgroundColor = System.Drawing.Color.FromArgb(255, 221, 153);
            textBox1.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            comboBox1.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            comboBox2.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            comboBox3.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dataGridView1.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            string fullname = Properties.Settings.Default.userName;
            string formattedname = fullname;

            string[] parts = fullname.Split(' ');

            if (parts.Length == 3)
            {
                string lastname = parts[0];
                string firstname = parts[1].Substring(0, 1);
                string middle = parts[2].Substring(0, 1);
                formattedname = $"{lastname} {firstname}.{middle}.";
            }
            label1.Text = formattedname;
            label2.Text = Properties.Settings.Default.userRole;
        }

        private void ResetInactivityTimer(object sender, EventArgs e)
        {
            inactivityTimer.Stop();
            inactivityTimer.Interval = Properties.Settings.Default.InactivityTimeout * 1000;
            inactivityTimer.Start();
        }

        private void InactivityTimer_Tick(object sender, EventArgs e)
        {
            inactivityTimer.Stop();
            ShowLoginForm();
        }

        private void ShowLoginForm()
        {
            this.Hide();
            var loginForm = new Authorization();
            loginForm.ShowDialog();
            this.Show();
            ResetInactivityTimer(null, null);
        }

        private bool allowClose = false;

        private void button3_Click(object sender, EventArgs e)
        {
            allowClose = true;
            this.Visible = false;
            MainFormMeneger mainFormMeneger = new MainFormMeneger();
            mainFormMeneger.ShowDialog();
            this.Close();
        }

        private void ViewingMenu_FormClosing(object sender, FormClosingEventArgs e)
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

        void FillFilterCategories()
        {
            MySqlConnection con = new MySqlConnection(conString);
            con.Open();

            MySqlCommand cmd = new MySqlCommand(@"SELECT * FROM CafeActivities.Categories;", con);
            MySqlDataReader rdr = cmd.ExecuteReader();

            comboBox2.Items.Clear();
            comboBox2.Items.Add("Все категории");

            while (rdr.Read())
            {
                comboBox2.Items.Add(rdr[1].ToString());
            }

            comboBox2.SelectedIndex = 0;

            con.Close();
        }

        void FillFilterEvents()
        {
            MySqlConnection con = new MySqlConnection(conString);
            con.Open();

            MySqlCommand cmd = new MySqlCommand(@"SELECT * FROM CafeActivities.Events;", con);
            MySqlDataReader rdr = cmd.ExecuteReader();

            comboBox1.Items.Clear();
            comboBox1.Items.Add("Все мероприятия");

            while (rdr.Read())
            {
                comboBox1.Items.Add(rdr[1].ToString());
            }

            comboBox1.SelectedIndex = 0;

            con.Close();
        }

        void FillSort()
        {
            comboBox3.Items.Clear();
            comboBox3.Items.Add("Сортировать по");
            comboBox3.Items.Add("По возрастанию цены");
            comboBox3.Items.Add("По убыванию цены");

            comboBox3.SelectedIndex = 0;
        }

        void FillDataGridView(string where = "")
        {
            string conStr = @"SELECT 
                    p.Article, 
                    p.`Name`,
                    p.Compound,
                    r.`Event` as IdEvent, 
                    c.Category as IdCategory,
                    p.Weight,
                    p.Price,
                    p.Photo
                FROM CafeActivities.Dishes p 
                LEFT JOIN CafeActivities.Categories c ON p.IdCategory = c.IDcategory 
                LEFT JOIN CafeActivities.`Events` r ON p.IdEvent = r.IDevent";

            List<string> conditions = new List<string>();

            // Добавляем условия фильтрации мероприятия
            if (comboBox1.SelectedIndex != 0 && comboBox1.SelectedIndex != -1 && comboBox1.SelectedItem != null)
            {
                conditions.Add($"r.`Event` = '{MySqlHelper.EscapeString(comboBox1.SelectedItem.ToString())}'");
            }

            // Добавляем условия фильтрации категории
            if (comboBox2.SelectedIndex != 0 && comboBox2.SelectedIndex != -1 && comboBox2.SelectedItem != null)
            {
                conditions.Add($"c.Category = '{MySqlHelper.EscapeString(comboBox2.SelectedItem.ToString())}'");
            }

            // Добавляем условие поиска 
            if (!string.IsNullOrEmpty(where))
            {
                conditions.Add($"(p.Article LIKE '%{MySqlHelper.EscapeString(where)}%' OR p.Name LIKE '%{MySqlHelper.EscapeString(where)}%' OR p.Compound LIKE '%{MySqlHelper.EscapeString(where)}%')");
            }

            // Объединяем все условия
            if (conditions.Count > 0)
            {
                conStr += " WHERE " + string.Join(" AND ", conditions);
            }

            // Добавляем сортировку по цене
            if (comboBox3.SelectedIndex != 0 && comboBox3.SelectedIndex != -1 && comboBox3.SelectedItem != null)
            {
                conStr += " ORDER BY p.Price";
                conStr += comboBox3.SelectedItem.ToString() == "По возрастанию цены" ? " ASC" : " DESC";
            }
            else
            {
                // Сортировка по умолчанию (по названию)
                conStr += " ORDER BY p.Name ASC";
            }

            // Для отладки - выведем запрос в консоль
            Console.WriteLine("SQL Query: " + conStr);

            try
            {
                using (MySqlConnection con = new MySqlConnection(conString))
                {
                    con.Open();

                    using (MySqlCommand cmd = new MySqlCommand(conStr, con))
                    using (MySqlDataReader rdr = cmd.ExecuteReader())
                    {
                        dataGridView1.Rows.Clear();
                        dataGridView1.Columns.Clear();

                        // Создаем колонки ВМЕСТЕ с колонкой для изображений
                        dataGridView1.Columns.Add("Article", "Артикул");
                        dataGridView1.Columns.Add("Name", "Название");
                        dataGridView1.Columns.Add("Compound", "Описание");
                        dataGridView1.Columns.Add("IdEvent", "Мероприятие");
                        dataGridView1.Columns.Add("IdCategory", "Категория");
                        dataGridView1.Columns.Add("Weight", "Вес");
                        dataGridView1.Columns.Add("Price", "Цена");

                        // Создаем колонку для изображений
                        DataGridViewImageColumn imageColumn = new DataGridViewImageColumn();
                        imageColumn.Name = "Photo";
                        imageColumn.HeaderText = "Фото";
                        imageColumn.ImageLayout = DataGridViewImageCellLayout.Zoom;
                        imageColumn.Width = 80;
                        dataGridView1.Columns.Add(imageColumn);

                        int rowCount = 0;
                        while (rdr.Read())
                        {
                            string imagesFolder = @"C:\Users\Виктория\Downloads\Kursovaya\Kursovaya\Resources";
                            string photoFileName = rdr["Photo"].ToString();
                            string fullImagePath = Path.Combine(imagesFolder, photoFileName);
                            Image img = null;

                            if (File.Exists(fullImagePath))
                            {
                                // Загружаем изображение из файла
                                using (var fs = new FileStream(fullImagePath, FileMode.Open, FileAccess.Read))
                                {
                                    img = Image.FromStream(fs);
                                }
                            }
                            else
                            {
                                img = null;
                            }

                            int rowIndex = dataGridView1.Rows.Add(
                                rdr["Article"].ToString(),
                                rdr["Name"].ToString(),
                                rdr["Compound"].ToString(),
                                rdr["IdEvent"].ToString(),
                                rdr["IdCategory"].ToString(),
                                rdr["Weight"].ToString(),
                                rdr["Price"].ToString(),
                                img
                            );

                        rowCount++;
                        }

                        label14.Text = rowCount.ToString();

                        if (rowCount == 0)
                        {
                            MessageBox.Show("Данные не найдены", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine("Error details: " + ex.ToString());
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            FillDataGridView();
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            FillDataGridView();
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            FillDataGridView();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            FillDataGridView(textBox1.Text);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            FillSort();
            FillFilterCategories();
            FillFilterEvents();
            FillDataGridView();
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox tb = (TextBox)sender;

            // Разрешаем управляющие символы
            if (char.IsControl(e.KeyChar))
                return;

            // Если вводится пробел
            if (e.KeyChar == ' ')
            {
                // Запрещаем пробел в начале или после пробела/дефиса
                if (tb.Text.Length == 0 || tb.Text[tb.Text.Length - 1] == ' ' || tb.Text[tb.Text.Length - 1] == '-')
                {
                    e.Handled = true;
                    return;
                }
                e.Handled = false;
                return;
            }

            if ((e.KeyChar >= 'А' && e.KeyChar <= 'Я') ||
                (e.KeyChar >= 'а' && e.KeyChar <= 'я') ||
                e.KeyChar == 'Ё' || e.KeyChar == 'ё')
            {
                e.Handled = false;
                return;
            }

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
    }
}
