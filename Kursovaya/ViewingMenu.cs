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
        private DataTable dataTable;
        private int selectedProductRowIndex = -1;

        private int currentPage = 1;
        private int totalPages = 1;

        public ViewingMenu()
        {
            InitializeComponent();

            this.WindowState = FormWindowState.Maximized;

            FillSort();
            FillFilterCategories();
            FillFilterEvents();
            FillDataGridView();

            this.Resize += ViewingMenu_Resize;

            button2.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button3.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            dataGridView1.BackgroundColor = System.Drawing.Color.FromArgb(255, 221, 153);
            textBox2.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
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

        //Создание элементов пагинации
        void Pagination()
        {
            for (int j = 0, count = this.Controls.Count; j < count; ++j)
            {
                if (this.Controls[j].Name.StartsWith("page") ||
                    this.Controls[j].Name == "btnPrev" ||
                    this.Controls[j].Name == "btnNext")
                {
                    this.Controls.RemoveAt(j);
                    j--;
                    count--;
                }
            }

            totalPages = dataGridView1.Rows.Count / 20;
            if (Convert.ToBoolean(dataGridView1.Rows.Count % 20)) totalPages += 1;
            if (totalPages == 0) totalPages = 1;

            int yPosition = dataGridView1.Bottom + 10;
            int leftMargin = 13;

            Button btnPrev = new Button();
            btnPrev.Name = "btnPrev";
            btnPrev.Text = "◀";
            btnPrev.Font = new Font("Microsoft Sans Serif", 8, FontStyle.Bold);
            btnPrev.Size = new Size(30, 25);
            btnPrev.Location = new Point(leftMargin, yPosition);
            btnPrev.Click += new EventHandler(BtnPrev_Click);
            btnPrev.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            btnPrev.FlatStyle = FlatStyle.Flat;
            btnPrev.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnPrev);

            int x = leftMargin + 35;
            int step = 20;

            LinkLabel[] ll = new LinkLabel[totalPages];
            for (int i = 0; i < totalPages; i++)
            {
                int pageNumber = i + 1;
                ll[i] = new LinkLabel();
                ll[i].Text = Convert.ToString(pageNumber);
                ll[i].Font = new Font("Microsoft Sans Serif", 14, FontStyle.Regular);
                ll[i].Name = "page" + pageNumber;
                ll[i].AutoSize = true;
                ll[i].Location = new Point(x, yPosition);
                ll[i].Click += new EventHandler(LinkLabel_Click);
                ll[i].BackColor = Color.Transparent;

                if (pageNumber == currentPage)
                {
                    ll[i].LinkBehavior = LinkBehavior.NeverUnderline;
                    ll[i].ForeColor = Color.DarkRed;
                    ll[i].Font = new Font(ll[i].Font, FontStyle.Bold);
                }
                else
                {
                    ll[i].LinkBehavior = LinkBehavior.AlwaysUnderline;
                    ll[i].ForeColor = Color.Blue;
                    ll[i].Font = new Font(ll[i].Font, FontStyle.Regular);
                }

                this.Controls.Add(ll[i]);
                x += step;
            }

            Button btnNext = new Button();
            btnNext.Name = "btnNext";
            btnNext.Text = "▶";
            btnNext.Font = new Font("Microsoft Sans Serif", 8, FontStyle.Bold);
            btnNext.Size = new Size(30, 25);
            btnNext.Location = new Point(x, yPosition);
            btnNext.Click += new EventHandler(BtnNext_Click);
            btnNext.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            btnNext.FlatStyle = FlatStyle.Flat;
            btnNext.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnNext);

            ShowPage(currentPage);
            UpdateNavigationButtons();
        }

        //Отображение выбранной страницы
        private void ShowPage(int pageNumber)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageNumber > totalPages) pageNumber = totalPages;

            currentPage = pageNumber;

            int countRows = dataGridView1.Rows.Count;
            int sizePage = 20;
            int start = (pageNumber - 1) * sizePage;
            int stop = Math.Min(start + sizePage - 1, countRows - 1);

            for (int j = 0; j < countRows; ++j)
            {
                dataGridView1.Rows[j].Visible = (j >= start && j <= stop);
            }

            if (dataGridView1.Rows.Count > start)
            {
                dataGridView1.FirstDisplayedScrollingRowIndex = start;
            }

            if (dataTable != null)
            {
                UpdateRowCount(0, dataTable.Rows.Count);
            }
        }

        //Обработчик кнопки "Назад"
        private void BtnPrev_Click(object sender, EventArgs e)
        {
            if (currentPage > 1)
            {
                ShowPage(currentPage - 1);
                Pagination();
            }
        }

        //Обработчик кнопки "Вперед"
        private void BtnNext_Click(object sender, EventArgs e)
        {
            if (currentPage < totalPages)
            {
                ShowPage(currentPage + 1);
                Pagination();
            }
        }

        //Обработчик клика по номеру страницы
        private void LinkLabel_Click(object sender, EventArgs e)
        {
            LinkLabel l = sender as LinkLabel;
            if (l != null && int.TryParse(l.Text, out int pageNumber))
            {
                ShowPage(pageNumber);
                Pagination();
            }
        }

        //Обновление состояния кнопок навигации
        private void UpdateNavigationButtons()
        {
            Button btnPrev = this.Controls.Find("btnPrev", false).FirstOrDefault() as Button;
            Button btnNext = this.Controls.Find("btnNext", false).FirstOrDefault() as Button;

            if (btnPrev != null)
            {
                btnPrev.Enabled = (currentPage > 1);
                btnPrev.BackColor = btnPrev.Enabled ?
                    System.Drawing.Color.FromArgb(217, 152, 22) :
                    System.Drawing.Color.FromArgb(200, 200, 200);
                btnPrev.ForeColor = btnPrev.Enabled ? Color.Black : Color.Gray;
            }

            if (btnNext != null)
            {
                btnNext.Enabled = (currentPage < totalPages);
                btnNext.BackColor = btnNext.Enabled ?
                    System.Drawing.Color.FromArgb(217, 152, 22) :
                    System.Drawing.Color.FromArgb(200, 200, 200);
                btnNext.ForeColor = btnNext.Enabled ? Color.Black : Color.Gray;
            }
        }

        //Обработчик изменения размера формы
        private void ViewingMenu_Resize(object sender, EventArgs e)
        {
            int savedPage = currentPage;
            Pagination();
            currentPage = savedPage;
            ShowPage(currentPage);
        }

        //Построение запроса для подсчета количества записей
        private string BuildCountQuery(string where = "")
        {
            StringBuilder query = new StringBuilder();
            query.Append(@"SELECT COUNT(*) 
                FROM CafeActivities.Dishes p 
                LEFT JOIN CafeActivities.Categories c ON p.IdCategory = c.IDcategory 
                LEFT JOIN CafeActivities.`Events` r ON p.IdEvent = r.IDevent");

            List<string> conditions = new List<string>();

            if (comboBox1.SelectedIndex != 0 && comboBox1.SelectedIndex != -1 && comboBox1.SelectedItem != null)
            {
                conditions.Add($"r.`Event` = '{MySqlHelper.EscapeString(comboBox1.SelectedItem.ToString())}'");
            }

            if (comboBox2.SelectedIndex != 0 && comboBox2.SelectedIndex != -1 && comboBox2.SelectedItem != null)
            {
                conditions.Add($"c.Category = '{MySqlHelper.EscapeString(comboBox2.SelectedItem.ToString())}'");
            }

            if (!string.IsNullOrEmpty(where))
            {
                conditions.Add($"(p.Name LIKE '{MySqlHelper.EscapeString(where)}%')");
            }

            if (conditions.Count > 0)
            {
                query.Append(" WHERE " + string.Join(" AND ", conditions));
            }

            return query.ToString();
        }

        //Обновление счетчика строк
        private void UpdateRowCount(int displayedCount, int totalCount)
        {
            int visibleCount = 0;
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                if (dataGridView1.Rows[i].Visible)
                {
                    visibleCount++;
                }
            }

            label14.Text = $"{visibleCount} из {totalCount}";
        }

        //Загрузка данных с подсчетом количества
        private void LoadDataWithCount(string where = "")
        {
            try
            {
                Cursor = Cursors.WaitCursor;

                string query = BuildQuery(where);
                string countQuery = BuildCountQuery(where);

                using (MySqlConnection con = new MySqlConnection(conString))
                {
                    con.Open();

                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                    {
                        dataTable = new DataTable();
                        adapter.Fill(dataTable);
                    }

                    using (MySqlCommand cmd = new MySqlCommand(countQuery, con))
                    {
                        int totalCount = Convert.ToInt32(cmd.ExecuteScalar());
                        UpdateRowCount(dataTable.Rows.Count, totalCount);
                    }

                    DisplayDataInDataGridView(dataTable, where);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }

            Pagination();
        }

        //Построение запроса для получения данных
        private string BuildQuery(string where = "")
        {
            StringBuilder query = new StringBuilder();
            query.Append(@"SELECT 
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
                LEFT JOIN CafeActivities.`Events` r ON p.IdEvent = r.IDevent");

            List<string> conditions = new List<string>();

            if (comboBox1.SelectedIndex != 0 && comboBox1.SelectedIndex != -1 && comboBox1.SelectedItem != null)
            {
                conditions.Add($"r.`Event` = '{MySqlHelper.EscapeString(comboBox1.SelectedItem.ToString())}'");
            }

            if (comboBox2.SelectedIndex != 0 && comboBox2.SelectedIndex != -1 && comboBox2.SelectedItem != null)
            {
                conditions.Add($"c.Category = '{MySqlHelper.EscapeString(comboBox2.SelectedItem.ToString())}'");
            }

            if (!string.IsNullOrEmpty(where))
            {
                conditions.Add($"(p.Name LIKE '{MySqlHelper.EscapeString(where)}%')");
            }

            if (conditions.Count > 0)
            {
                query.Append(" WHERE " + string.Join(" AND ", conditions));
            }

            if (comboBox3.SelectedIndex != 0 && comboBox3.SelectedIndex != -1 && comboBox3.SelectedItem != null)
            {
                query.Append(" ORDER BY p.Price");
                query.Append(comboBox3.SelectedItem.ToString() == "По возрастанию цены" ? " ASC" : " DESC");
            }
            else
            {
                query.Append(" ORDER BY p.`Name` ASC");
            }

            return query.ToString();
        }

        //Отображение данных в DataGridView
        private void DisplayDataInDataGridView(DataTable tableToDisplay, string where = "")
        {
            if (tableToDisplay == null) return;

            dataGridView1.Rows.Clear();

            if (dataGridView1.Columns.Count == 0)
            {
                dataGridView1.Columns.Add("Article", "Артикул");
                dataGridView1.Columns.Add("Name", "Название");
                dataGridView1.Columns.Add("Compound", "Описание");
                dataGridView1.Columns["Compound"].Visible = false;
                dataGridView1.Columns.Add("IdEvent", "Мероприятие");
                dataGridView1.Columns.Add("IdCategory", "Категория");
                dataGridView1.Columns.Add("Weight", "Вес");
                dataGridView1.Columns.Add("Price", "Цена");

                DataGridViewImageColumn imageColumn = new DataGridViewImageColumn();
                imageColumn.Name = "Photo";
                imageColumn.HeaderText = "Фото";
                imageColumn.ImageLayout = DataGridViewImageCellLayout.Zoom;
                imageColumn.Width = 80;
                dataGridView1.Columns.Add(imageColumn);
            }

            DataView dv = new DataView(tableToDisplay);

            if (!string.IsNullOrEmpty(where))
            {
                string searchText = where.ToLower();
                dv.RowFilter = $"Name LIKE '{searchText}%'";
            }

            string imagesFolder = @".\Resources\";

            foreach (DataRowView rowView in dv)
            {
                DataRow row = rowView.Row;

                string photoFileName = row["Photo"].ToString();
                string fullImagePath = Path.Combine(imagesFolder, photoFileName);
                Image img = null;

                if (!string.IsNullOrEmpty(photoFileName) && File.Exists(fullImagePath))
                {
                    using (var fs = new FileStream(fullImagePath, FileMode.Open, FileAccess.Read))
                    {
                        img = Image.FromStream(fs);
                    }
                }
                else
                {
                    string placeholderPath = Path.Combine(imagesFolder, "picture.png");
                    if (File.Exists(placeholderPath))
                    {
                        using (var fs = new FileStream(placeholderPath, FileMode.Open, FileAccess.Read))
                        {
                            img = Image.FromStream(fs);
                        }
                    }
                }

                int rowIndex = dataGridView1.Rows.Add(
                    row["Article"].ToString(),
                    row["Name"].ToString(),
                    row["Compound"].ToString(),
                    row["IdEvent"].ToString(),
                    row["IdCategory"].ToString(),
                    row["Weight"].ToString(),
                    row["Price"].ToString(),
                    img
                );
            }

            int displayedCount = dataGridView1.Rows.Count;
            if (dataGridView1.AllowUserToAddRows && displayedCount > 0)
            {
                displayedCount--;
            }
            UpdateRowCount(displayedCount, tableToDisplay.Rows.Count);
        }

        private bool allowClose = false;

        //Обработчик кнопки возврата в главное меню
        private void button3_Click(object sender, EventArgs e)
        {
            allowClose = true;
            this.Visible = false;
            MainFormMeneger mainFormMeneger = new MainFormMeneger();
            mainFormMeneger.ShowDialog();
            this.Close();
        }

        //Обработчик закрытия формы
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

        //Загрузка категорий в выпадающий список
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

        //Загрузка мероприятий в выпадающий список
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

        //Загрузка вариантов сортировки
        void FillSort()
        {
            comboBox3.Items.Clear();
            comboBox3.Items.Add("Сортировать по");
            comboBox3.Items.Add("По возрастанию цены");
            comboBox3.Items.Add("По убыванию цены");

            comboBox3.SelectedIndex = 0;
        }

        //Загрузка данных в DataGridView
        void FillDataGridView(string where = "")
        {
            LoadDataWithCount(where);
        }

        //Обработчик изменения выбранного мероприятия
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadDataWithCount(textBox1.Text);
        }

        //Обработчик изменения выбранной категории
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadDataWithCount(textBox1.Text);
        }

        //Обработчик изменения выбранной сортировки
        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadDataWithCount(textBox1.Text);
        }

        //Обработчик изменения текста поиска
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            LoadDataWithCount(textBox1.Text);
        }

        //Обработчик кнопки сброса фильтров
        private void button2_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            FillSort();
            FillFilterCategories();
            FillFilterEvents();
            LoadDataWithCount();
        }

        //Ограничение ввода в поле поиска
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox tb = (TextBox)sender;

            if (char.IsControl(e.KeyChar))
                return;

            if (e.KeyChar == ' ')
            {
                if (tb.Text.Length == 0)
                {
                    e.Handled = true;
                    return;
                }
                if (tb.Text.Length > 0 && tb.Text[tb.Text.Length - 1] == ' ')
                {
                    e.Handled = true;
                    return;
                }
                if (tb.Text.Length > 0 && tb.Text[tb.Text.Length - 1] == '-')
                {
                    e.Handled = true;
                    return;
                }
                e.Handled = false;
                return;
            }

            if (e.KeyChar == '-')
            {
                if (tb.Text.Length == 0)
                {
                    e.Handled = true;
                    return;
                }
                if (tb.Text.Length > 0 && tb.Text[tb.Text.Length - 1] == '-')
                {
                    e.Handled = true;
                    return;
                }
                if (tb.Text.Length > 0 && tb.Text[tb.Text.Length - 1] == ' ')
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

            e.Handled = true;
        }

        //Обработчик загрузки формы
        private void ViewingMenu_Load(object sender, EventArgs e)
        {
            LoadDataWithCount();
            dataGridView1.CurrentCell = null;
            dataGridView1.ClearSelection();
            textBox2.Text = "";
        }

        //Обработчик клика по ячейке DataGridView
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                selectedProductRowIndex = e.RowIndex;
            }
        }

        //Обработчик изменения выделения в DataGridView
        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow != null && dataGridView1.CurrentRow.Index >= 0)
            {
                try
                {
                    DataGridViewRow selectedRow = dataGridView1.CurrentRow;
                    textBox2.Text = selectedRow.Cells["Compound"].Value?.ToString() ?? "";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при заполнении поля: {ex.Message}");
                }
            }
        }

        //Установка русской раскладки в поле поиска
        private void textBox1_Enter(object sender, EventArgs e)
        {
            foreach (InputLanguage lang in InputLanguage.InstalledInputLanguages)
            {
                if (lang.Culture.TwoLetterISOLanguageName == "ru")
                {
                    InputLanguage.CurrentInputLanguage = lang;
                    break;
                }
            }
        }
    }
}