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

            this.MouseMove += ResetInactivityTimer;
            this.KeyDown += ResetInactivityTimer;
            this.MouseWheel += ResetInactivityTimer;
            this.DoubleClick += ResetInactivityTimer;
            this.MouseDoubleClick += ResetInactivityTimer;
            dataGridView1.Scroll += ResetInactivityTimer;

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

        void Pagination()
        {
            // удаляем LinkLabel служащий для пагинации
            // каждый раз будем создавать новую пагинацию
            for (int j = 0, count = this.Controls.Count; j < count; ++j)
            {
                this.Controls.RemoveByKey("page" + j);
            }

            // узнаём сколько страниц будет
            int size = dataGridView1.Rows.Count / 20; // на каждой странице по 20 зиписей
            if (Convert.ToBoolean(dataGridView1.Rows.Count % 20)) size += 1; // ситуакиця когда при делении получаем не целое число
            LinkLabel[] ll = new LinkLabel[size]; // пагинация на основе элемента ссылка(можно использовать и другой элемент)
            int x = 13, y = 650, step = 15; // место на форме для меню пагинации и расстояние между номерами страниц

            for (int i = 0; i < size; ++i)
            {
                ll[i] = new LinkLabel();
                ll[i].Text = Convert.ToString(i + 1); // текст(номер старницы) который видет пользователь
                ll[i].Name = "page" + i;
                ll[i].AutoSize = true; //!!!
                ll[i].Location = new Point(x, y);
                ll[i].Click += new EventHandler(LinkLabel_Click); // один обработчик для всех пунктов пагинации
                this.Controls.Add(ll[i]); // добавление на форму

                x += step;
            }

            // чтобы понять на какой странице пользователь убираем подчеркивание для активной странице
            // по умолчанию первая страница активна
            ll[0].LinkBehavior = LinkBehavior.NeverUnderline;
        }

        // выбор страницы пагинации
        // те строки которые нам не нужны на выбраной странице - скрываем
        private void LinkLabel_Click(object sender, EventArgs e)
        {
            // возвращаем всем LinkLabel подчеркивание
            foreach (var ctrl in this.Controls)
            {
                if (ctrl is LinkLabel)
                {
                    (ctrl as LinkLabel).LinkBehavior = LinkBehavior.AlwaysUnderline;
                }
            }

            // узнаём какая страница выбрана и убираем подчеркивание для неё
            LinkLabel l = sender as LinkLabel;
            l.LinkBehavior = LinkBehavior.NeverUnderline;

            // узнаём с какой и по какую строку отображать информацию в таблицу
            // другие строки будем скрывать
            int numPage = Convert.ToInt32(l.Text) - 1;
            int countRows = dataGridView1.Rows.Count;
            int sizePage = 20;
            int start = numPage * sizePage;
            int stop = (countRows - start) >= sizePage ? start + sizePage : countRows;

            for (int j = 0; j < countRows; ++j)
            {
                if (j < start || j > stop)
                {
                    dataGridView1.Rows[j].Visible = false;
                }
                else
                {
                    dataGridView1.Rows[j].Visible = true;
                }
            }
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
            inactivityTimer.Stop();
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
            Pagination();
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            FillDataGridView();
            Pagination();
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            FillDataGridView();
            Pagination();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            FillDataGridView(textBox1.Text);
            Pagination();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            FillSort();
            FillFilterCategories();
            FillFilterEvents();
            FillDataGridView();
            Pagination();
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

        private void ViewingMenu_Load(object sender, EventArgs e)
        {
            Pagination();
        }
    }
}
