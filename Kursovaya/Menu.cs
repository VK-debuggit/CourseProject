using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kursovaya
{
    public partial class Menu : Form
    {
        string conString = $"host={Properties.Settings.Default.host};uid={Properties.Settings.Default.uid};pwd={Properties.Settings.Default.pwd};database={Properties.Settings.Default.database};";
        private Timer inactivityTimer;
        private int inactivityTimeout;
        private int rowCount = 0;
        private Image newProductImage;
        private string originalImageFilePath;
        private string _lastInsertedDishId = null; // Хранит артикул последнего добавленного блюда (как строку)

        // Переменные для пагинации 
        private int currentPage = 1;
        private int totalPages = 1;

        // Поля для хранения исходных данных
        private DataGridViewRow selectedRowData = null;
        private string originalArticle = "";
        private string originalName = "";
        private string originalCompound = "";
        private string originalWeight = "";
        private string originalPrice = "";
        private string originalPhoto = "";
        private string originalEvent = "";
        private string originalCategory = "";

        public Menu()
        {
            InitializeComponent();

            FillDataGridView();
            FillFilterCategory();
            FillFilterEvent();

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
            this.Resize += Menu_Resize;

            button1.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button2.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button3.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button4.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button5.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            textBox1.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            textBox2.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            textBox3.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            textBox4.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            textBox5.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            comboBox1.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            comboBox2.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            dataGridView1.BackgroundColor = System.Drawing.Color.FromArgb(255, 221, 153);
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

            this.WindowState = FormWindowState.Maximized;
        }

        // ========== ПАГИНАЦИЯ ==========

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

            for (int i = 0; i < totalPages; i++)
            {
                int pageNumber = i + 1;
                LinkLabel link = new LinkLabel();
                link.Text = Convert.ToString(pageNumber);
                link.Font = new Font("Microsoft Sans Serif", 14, FontStyle.Regular);
                link.Name = "page" + pageNumber;
                link.AutoSize = true;
                link.Location = new Point(x, yPosition);
                link.Click += new EventHandler(LinkLabel_Click);
                link.BackColor = Color.Transparent;

                if (pageNumber == currentPage)
                {
                    link.LinkBehavior = LinkBehavior.NeverUnderline;
                    link.ForeColor = Color.DarkRed;
                    link.Font = new Font(link.Font, FontStyle.Bold);
                }
                else
                {
                    link.LinkBehavior = LinkBehavior.AlwaysUnderline;
                    link.ForeColor = Color.Blue;
                }

                this.Controls.Add(link);
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
            UpdateRowCount();
        }

        private void ShowPage(int pageNumber)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageNumber > totalPages) pageNumber = totalPages;

            currentPage = pageNumber;

            int sizePage = 20;
            int start = (pageNumber - 1) * sizePage;
            int stop = Math.Min(start + sizePage - 1, dataGridView1.Rows.Count - 1);

            for (int j = 0; j < dataGridView1.Rows.Count; ++j)
            {
                dataGridView1.Rows[j].Visible = (j >= start && j <= stop);
            }

            if (dataGridView1.Rows.Count > start)
            {
                dataGridView1.FirstDisplayedScrollingRowIndex = start;
            }

            UpdateRowCount();
        }

        private void BtnPrev_Click(object sender, EventArgs e)
        {
            if (currentPage > 1)
            {
                ShowPage(currentPage - 1);
                Pagination();
                ResetInactivityTimer(sender, e);
            }
        }

        private void BtnNext_Click(object sender, EventArgs e)
        {
            if (currentPage < totalPages)
            {
                ShowPage(currentPage + 1);
                Pagination();
                ResetInactivityTimer(sender, e);
            }
        }

        private void LinkLabel_Click(object sender, EventArgs e)
        {
            LinkLabel l = sender as LinkLabel;
            if (l != null && int.TryParse(l.Text, out int pageNumber))
            {
                ShowPage(pageNumber);
                Pagination();
                ResetInactivityTimer(sender, e);
            }
        }

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

        private void Menu_Resize(object sender, EventArgs e)
        {
            int savedPage = currentPage;
            Pagination();
            currentPage = savedPage;
            ShowPage(currentPage);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Left || keyData == Keys.PageUp)
            {
                if (currentPage > 1)
                {
                    BtnPrev_Click(null, null);
                    return true;
                }
            }
            else if (keyData == Keys.Right || keyData == Keys.PageDown)
            {
                if (currentPage < totalPages)
                {
                    BtnNext_Click(null, null);
                    return true;
                }
            }
            else if (keyData == Keys.Home)
            {
                if (currentPage != 1)
                {
                    ShowPage(1);
                    Pagination();
                    ResetInactivityTimer(null, null);
                }
                return true;
            }
            else if (keyData == Keys.End)
            {
                if (currentPage != totalPages)
                {
                    ShowPage(totalPages);
                    Pagination();
                    ResetInactivityTimer(null, null);
                }
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void UpdateRowCount()
        {
            int totalCount = dataGridView1.Rows.Count;
            int totalInDatabase = 0;
            string q = "SELECT COUNT(*) FROM CafeActivities.Dishes;";
            using (MySqlConnection con = new MySqlConnection(conString))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand(q, con))
                {
                    totalInDatabase = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }

            int visibleCount = 0;
            for (int i = (currentPage - 1) * 20; i < currentPage * 20 && i < totalCount; i++)
            {
                visibleCount++;
            }

            label14.Text = $"{visibleCount} из {totalInDatabase}";
        }

        // ========== ТАЙМЕРЫ ==========

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

        private void button4_Click(object sender, EventArgs e)
        {
            inactivityTimer.Stop();
            allowClose = true;
            this.Visible = false;
            MainFormMeneger mainFormMeneger = new MainFormMeneger();
            mainFormMeneger.ShowDialog();
            this.Close();
        }

        private void Menu_FormClosing(object sender, FormClosingEventArgs e)
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

        // ========== ЗАГРУЗКА ДАННЫХ ==========

        void FillDataGridView()
        {
            // Сортировка по наименованию в алфавитном порядке (Name ASC)
            string SelectQuery = @"SELECT 
	                                p.Article, 
                                    c.Event as Event, 
                                    d.Category as Category, 
                                    p.Name, 
                                    p.Compound, 
                                    p.Weight, 
                                    p.Price, 
                                    p.Photo 
                                    FROM CafeActivities.Dishes p
                                    LEFT JOIN Categories d ON p.IdCategory = d.IDcategory
                                    LEFT JOIN Events c ON p.IdEvent = c.IDevent
                                    ORDER BY p.Name ASC;";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();

                    using (MySqlCommand cmd = new MySqlCommand(SelectQuery, con))
                    using (MySqlDataReader rdr = cmd.ExecuteReader())
                    {
                        dataGridView1.Rows.Clear();
                        dataGridView1.Columns.Clear();

                        DataGridViewImageColumn imageColumn = new DataGridViewImageColumn();
                        imageColumn.Name = "Photo";
                        imageColumn.HeaderText = "Фото";
                        imageColumn.ImageLayout = DataGridViewImageCellLayout.Zoom;
                        imageColumn.Width = 80;

                        // Порядок колонок
                        dataGridView1.Columns.Add("Article", "Артикул");
                        dataGridView1.Columns.Add("Name", "Название");
                        dataGridView1.Columns["Name"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                        dataGridView1.Columns.Add("Event", "Мероприятие");
                        dataGridView1.Columns.Add("Category", "Категория");
                        dataGridView1.Columns.Add("Compound", "Описание");
                        dataGridView1.Columns["Compound"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                        dataGridView1.Columns.Add("Weight", "Вес");
                        dataGridView1.Columns.Add("Price", "Цена");
                        dataGridView1.Columns.Add(imageColumn);

                        // Временный список для хранения всех записей
                        var dishes = new List<(string Article, string Event, string Category, string Name, string Compound, string Weight, string Price, Image Photo, string PhotoFileName)>();
                        rowCount = 0;

                        string imagesFolder = @".\Resources\";

                        while (rdr.Read())
                        {
                            string article = rdr["Article"].ToString();
                            string name = rdr["Name"].ToString();
                            string eventName = rdr["Event"].ToString();
                            string categoryName = rdr["Category"].ToString();
                            string compound = rdr["Compound"].ToString();
                            string weight = rdr["Weight"].ToString();
                            string price = rdr["Price"].ToString();
                            string photoFileName = rdr["Photo"].ToString();
                            string fullImagePath = Path.Combine(imagesFolder, photoFileName);
                            Image img = null;

                            if (!string.IsNullOrEmpty(photoFileName) && File.Exists(fullImagePath))
                            {
                                using (var fs = new FileStream(fullImagePath, FileMode.Open, FileAccess.Read))
                                {
                                    img = Image.FromStream(fs);
                                }
                            }

                            dishes.Add((article, eventName, categoryName, name, compound, weight, price, img, photoFileName));
                            rowCount++;
                        }

                        // Если есть новая запись, перемещаем её в начало
                        if (!string.IsNullOrEmpty(_lastInsertedDishId))
                        {
                            var newDish = dishes.FirstOrDefault(d => d.Article == _lastInsertedDishId);
                            if (newDish.Article != null)
                            {
                                dishes.Remove(newDish);
                                dishes.Insert(0, newDish);
                            }
                            // Сбрасываем ID после использования
                            _lastInsertedDishId = null;
                        }

                        // Добавляем в DataGridView
                        foreach (var dish in dishes)
                        {
                            dataGridView1.Rows.Add(
                                dish.Article,
                                dish.Name,
                                dish.Event,
                                dish.Category,
                                dish.Compound,
                                dish.Weight,
                                dish.Price,
                                dish.Photo
                            );
                        }

                        label14.Text = rowCount.ToString();

                        if (rowCount == 0)
                        {
                            MessageBox.Show("Данные не найдены", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            currentPage = 1;
            Pagination();
        }

        void FillFilterCategory()
        {
            using (MySqlConnection con = new MySqlConnection(conString))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand(@"SELECT * FROM CafeActivities.Categories;", con);
                MySqlDataReader rdr = cmd.ExecuteReader();
                comboBox2.Items.Clear();
                while (rdr.Read())
                {
                    comboBox2.Items.Add(rdr[1].ToString());
                }
                if (comboBox2.Items.Count > 0) comboBox2.SelectedIndex = 0;
                con.Close();
            }
        }

        void FillFilterEvent()
        {
            using (MySqlConnection con = new MySqlConnection(conString))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand(@"SELECT * FROM CafeActivities.Events;", con);
                MySqlDataReader rdr = cmd.ExecuteReader();
                comboBox1.Items.Clear();
                while (rdr.Read())
                {
                    comboBox1.Items.Add(rdr[1].ToString());
                }
                if (comboBox1.Items.Count > 0) comboBox1.SelectedIndex = 0;
                con.Close();
            }
        }

        private void textBox4_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox tb = (TextBox)sender;

            if (char.IsControl(e.KeyChar))
                return;

            if (tb.Text.Length > 0 && char.IsLower(tb.Text[0]))
            {
                int cursorPos = tb.SelectionStart;
                string newText = char.ToUpper(tb.Text[0]) + tb.Text.Substring(1);
                if (tb.Text != newText)
                {
                    tb.Text = newText;
                    tb.SelectionStart = cursorPos;
                }
            }

            if (e.KeyChar == ' ')
            {
                if (tb.Text.Length == 0 || tb.Text[tb.Text.Length - 1] == ' ' || tb.Text[tb.Text.Length - 1] == '-')
                {
                    e.Handled = true;
                    return;
                }
                e.Handled = false;
                return;
            }

            if (e.KeyChar == '-')
            {
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

            e.Handled = true;
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar))
                return;
            if (char.IsDigit(e.KeyChar))
                return;
            e.Handled = true;
        }

        private void textBox3_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox tb = (TextBox)sender;

            if (char.IsControl(e.KeyChar))
                return;

            if (char.IsDigit(e.KeyChar))
            {
                string textBefore = tb.Text.Substring(0, tb.SelectionStart) + tb.Text.Substring(tb.SelectionStart + tb.SelectionLength);
                if (textBefore.Replace(".", "").Length + 1 > 12)
                {
                    e.Handled = true;
                    return;
                }

                int dotIndex = tb.Text.IndexOf('.');
                if (dotIndex != -1)
                {
                    int cursorPosition = tb.SelectionStart;
                    int digitsAfterDot = tb.Text.Length - dotIndex - 1;

                    if (cursorPosition > dotIndex && digitsAfterDot >= 2)
                    {
                        if (tb.SelectionLength == 0)
                        {
                            e.Handled = true;
                            return;
                        }
                    }
                }

                e.Handled = false;
                return;
            }

            if (e.KeyChar == '.')
            {
                if (tb.Text.Contains('.'))
                {
                    e.Handled = true;
                    return;
                }

                if (tb.Text.Length == 0)
                {
                    e.Handled = true;
                    return;
                }

                if (tb.Text.Length > 10)
                {
                    e.Handled = true;
                    return;
                }

                e.Handled = false;
                return;
            }

            e.Handled = true;
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox tb = (TextBox)sender;

            if (char.IsControl(e.KeyChar))
                return;

            if (char.IsDigit(e.KeyChar))
            {
                string textBefore = tb.Text.Substring(0, tb.SelectionStart) + tb.Text.Substring(tb.SelectionStart + tb.SelectionLength);
                if (textBefore.Replace(".", "").Length + 1 > 9)
                {
                    e.Handled = true;
                    return;
                }

                int dotIndex = tb.Text.IndexOf('.');
                if (dotIndex != -1)
                {
                    int cursorPosition = tb.SelectionStart;
                    int digitsAfterDot = tb.Text.Length - dotIndex - 1;

                    if (cursorPosition > dotIndex && digitsAfterDot >= 2)
                    {
                        if (tb.SelectionLength == 0)
                        {
                            e.Handled = true;
                            return;
                        }
                    }
                }

                e.Handled = false;
                return;
            }

            if (e.KeyChar == '.')
            {
                if (tb.Text.Contains('.'))
                {
                    e.Handled = true;
                    return;
                }

                if (tb.Text.Length == 0)
                {
                    e.Handled = true;
                    return;
                }

                if (tb.Text.Length > 7)
                {
                    e.Handled = true;
                    return;
                }

                e.Handled = false;
                return;
            }

            e.Handled = true;
        }

        private void textBox5_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox tb = (TextBox)sender;

            if (char.IsControl(e.KeyChar))
                return;

            if ((e.KeyChar >= 'А' && e.KeyChar <= 'Я') ||
                (e.KeyChar >= 'а' && e.KeyChar <= 'я') ||
                e.KeyChar == 'Ё' || e.KeyChar == 'ё')
            {
                e.Handled = false;
                return;
            }

            if (char.IsDigit(e.KeyChar))
            {
                e.Handled = false;
                return;
            }

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

                e.Handled = false;
                return;
            }

            char[] allowedSymbols = {
                '.', ',', '!', '?', ';', ':', '-', '—',
                '(', ')', '"', '\'', '«', '»',
                '/', '\\', '№', '#', '*',
                '+', '=', '%', '&', '@'
            };

            if (allowedSymbols.Contains(e.KeyChar))
            {
                e.Handled = false;
                return;
            }

            e.Handled = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "JPEG Images|*.jpg;*.jpeg|PNG Images|*.png";
                openFileDialog.Title = "Выберите изображение товара";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        originalImageFilePath = openFileDialog.FileName;

                        if (!IsFileTypeValid(originalImageFilePath))
                        {
                            MessageBox.Show("Недопустимый формат файла. Разрешены: JPG, JPEG, PNG",
                                          "Ошибка формата",
                                          MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        if (!IsFileSizeValid(originalImageFilePath))
                        {
                            return;
                        }

                        pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                        newProductImage = Image.FromFile(originalImageFilePath);

                        using (Image compressed = CompressImage(newProductImage, 3 * 1024 * 1024))
                        {
                            if (compressed != null)
                            {
                                pictureBox1.Image = (Image)compressed.Clone();
                                newProductImage.Dispose();
                                newProductImage = (Image)compressed.Clone();
                            }
                            else
                            {
                                pictureBox1.Image = newProductImage;
                            }
                        }

                        UpdateButtonsState();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}", "Ошибка",
                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private bool IsFileTypeValid(string filePath)
        {
            string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };
            string fileExtension = Path.GetExtension(filePath).ToLower();
            return allowedExtensions.Contains(fileExtension);
        }

        private bool IsFileSizeValid(string filePath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                long maxSize = 10 * 1024 * 1024;
                if (fileInfo.Length > maxSize)
                {
                    MessageBox.Show($"Размер файла {fileInfo.Length / (1024 * 1024)} МБ превышает 10 МБ.",
                                  "Превышен размер файла",
                                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка проверки размера файла: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private Image CompressImage(Image sourceImage, long targetSizeBytes = 3 * 1024 * 1024)
        {
            if (sourceImage == null) return null;

            int quality = 90;
            Image resultImage = null;

            using (MemoryStream ms = new MemoryStream())
            {
                do
                {
                    SaveJpegWithQuality(sourceImage, ms, quality);

                    if (ms.Length <= targetSizeBytes || quality <= 10)
                    {
                        resultImage = Image.FromStream(new MemoryStream(ms.ToArray()));
                        break;
                    }

                    quality -= 10;
                    ms.SetLength(0);
                    ms.Position = 0;

                } while (quality > 0);
            }

            return resultImage;
        }

        private void SaveJpegWithQuality(Image image, Stream stream, int quality)
        {
            ImageCodecInfo jpegCodec = GetEncoderInfo("image/jpeg");

            if (jpegCodec == null)
            {
                image.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
                return;
            }

            using (EncoderParameters encoderParams = new EncoderParameters(1))
            {
                encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
                image.Save(stream, jpegCodec, encoderParams);
            }
        }

        private ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            return codecs.FirstOrDefault(codec => codec.MimeType == mimeType);
        }

        private string SaveImageToFolder(Image image)
        {
            try
            {
                if (image == null)
                    return "";

                string resourcesFolder = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
                if (!Directory.Exists(resourcesFolder))
                {
                    Directory.CreateDirectory(resourcesFolder);
                }

                string fileName = $"dish_{Guid.NewGuid():N}.jpg";
                string fullPath = Path.Combine(resourcesFolder, fileName);

                using (Image compressedImage = CompressImage(image, 3 * 1024 * 1024))
                {
                    if (compressedImage != null)
                    {
                        compressedImage.Save(fullPath, System.Drawing.Imaging.ImageFormat.Jpeg);
                    }
                    else
                    {
                        using (Bitmap bmp = new Bitmap(image.Width, image.Height))
                        {
                            using (Graphics g = Graphics.FromImage(bmp))
                            {
                                g.DrawImage(image, 0, 0, image.Width, image.Height);
                            }
                            bmp.Save(fullPath, System.Drawing.Imaging.ImageFormat.Jpeg);
                        }
                    }
                }

                return fileName;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения изображения: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                return "";
            }
        }

        private bool IsArticleExists(string article)
        {
            string query = "SELECT COUNT(*) FROM Dishes WHERE Article = @article";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@article", article.Trim());
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка проверки артикула: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return true;
                }
            }
        }

        private bool IsDishExists(string nameDish, string compound, int idCategory, int idEvent)
        {
            string query = @"SELECT COUNT(*) FROM Dishes 
                    WHERE Name = @nameDishes 
                    AND Compound = @compoundDishes 
                    AND IdCategory = @idCategory
                    AND IdEvent = @idEvent";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@nameDishes", nameDish.Trim());
                        cmd.Parameters.AddWithValue("@compoundDishes", compound.Trim());
                        cmd.Parameters.AddWithValue("@idCategory", idCategory);
                        cmd.Parameters.AddWithValue("@idEvent", idEvent);

                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка проверки дубликата: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return true;
                }
            }
        }

        private bool IsAnotherDishExistsForEdit(string nameDish, string compound, int idCategory, int idEvent, int currentArticle)
        {
            string query = @"SELECT COUNT(*) FROM Dishes 
                    WHERE Name = @nameDishes 
                    AND Compound = @compoundDishes 
                    AND IdCategory = @idCategory
                    AND IdEvent = @idEvent
                    AND Article != @currentArticle";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@nameDishes", nameDish.Trim());
                        cmd.Parameters.AddWithValue("@compoundDishes", compound.Trim());
                        cmd.Parameters.AddWithValue("@idCategory", idCategory);
                        cmd.Parameters.AddWithValue("@idEvent", idEvent);
                        cmd.Parameters.AddWithValue("@currentArticle", currentArticle);

                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка проверки блюда: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return true;
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string article = textBox1.Text.Trim();
            string weight = textBox2.Text.Trim();
            string price = textBox3.Text.Trim();
            string nameDish = textBox4.Text.Trim();
            string compound = textBox5.Text.Trim();

            int idEvent = comboBox1.SelectedIndex + 1;
            int idCategory = comboBox2.SelectedIndex + 1;

            string photoPath = pictureBox1.Image != null ? SaveImageToFolder(pictureBox1.Image) : "";

            List<string> errors = new List<string>();

            if (string.IsNullOrEmpty(nameDish)) errors.Add("• Название блюда");
            if (string.IsNullOrEmpty(compound)) errors.Add("• Состав блюда");
            if (string.IsNullOrEmpty(weight)) errors.Add("• Вес блюда");
            if (string.IsNullOrEmpty(price)) errors.Add("• Цена блюда");
            if (string.IsNullOrEmpty(article)) errors.Add("• Артикул блюда");
            if (comboBox1.SelectedIndex < 0) errors.Add("• Мероприятие (не выбрано)");
            if (comboBox2.SelectedIndex < 0) errors.Add("• Категория (не выбрана)");

            if (errors.Count > 0)
            {
                string errorMessage = "Пожалуйста, заполните следующие обязательные поля:\n\n" + string.Join("\n", errors);
                MessageBox.Show(errorMessage, "Ошибка валидации",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(weight, System.Globalization.NumberStyles.Any,
                                 System.Globalization.CultureInfo.InvariantCulture,
                                 out decimal weightValue) || weightValue <= 0)
            {
                MessageBox.Show("Введите корректный вес блюда", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox2.Focus();
                return;
            }

            if (!decimal.TryParse(price, System.Globalization.NumberStyles.Any,
                                 System.Globalization.CultureInfo.InvariantCulture,
                                 out decimal priceValue) || priceValue <= 0)
            {
                MessageBox.Show("Введите корректную цену блюда", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox3.Focus();
                return;
            }

            if (IsArticleExists(article))
            {
                MessageBox.Show("Блюдо с таким артикулом уже существует", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (IsDishExists(nameDish, compound, idCategory, idEvent))
            {
                MessageBox.Show("Такое блюдо уже существует в базе данных.", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string query = @"INSERT INTO Dishes (Article, IdEvent, IdCategory, Name, Compound, Weight, Price, Photo) 
                            VALUES (@article, @idEvent, @idCategory, @nameDishes, @compoundDishes, @weight, @price, @photo)";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@article", article);
                        cmd.Parameters.AddWithValue("@weight", weightValue);
                        cmd.Parameters.AddWithValue("@price", priceValue);
                        cmd.Parameters.AddWithValue("@nameDishes", nameDish);
                        cmd.Parameters.AddWithValue("@compoundDishes", compound);
                        cmd.Parameters.AddWithValue("@photo", photoPath);
                        cmd.Parameters.AddWithValue("@idEvent", idEvent);
                        cmd.Parameters.AddWithValue("@idCategory", idCategory);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            // Сохраняем артикул новой записи
                            _lastInsertedDishId = article;

                            MessageBox.Show("Блюдо успешно добавлено", "Успех",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);
                            ClearForm();
                            FillDataGridView();
                            ClearAllFields();
                            Pagination();

                            if (dataGridView1.Rows.Count > 0)
                            {
                                dataGridView1.Rows[0].Selected = true;
                                dataGridView1.FirstDisplayedScrollingRowIndex = 0;
                            }
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    if (ex.Number == 1062)
                    {
                        MessageBox.Show("Такое блюдо уже существует в базе данных!", "Ошибка",
                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        MessageBox.Show($"Ошибка базы данных: {ex.Message}", "Ошибка",
                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка добавления блюда: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null)
            {
                MessageBox.Show("Выберите блюдо для редактирования", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int selectedId = Convert.ToInt32(dataGridView1.CurrentRow.Cells["Article"].Value);
            DataGridViewRow selectedRow = dataGridView1.CurrentRow;

            string oldCategory = selectedRow.Cells["Category"].Value?.ToString() ?? "";
            string oldEvent = selectedRow.Cells["Event"].Value?.ToString() ?? "";
            string oldPhoto = GetCurrentPhotoPath(selectedId);

            string name = textBox4.Text.Trim();
            string compound = textBox5.Text.Trim();
            string weightText = textBox2.Text.Trim();
            string priceText = textBox3.Text.Trim();

            int idCategory;
            int idEvent;

            if (comboBox2.SelectedItem != null && !string.IsNullOrEmpty(comboBox2.SelectedItem.ToString()))
            {
                idCategory = GetCategoryIdFromName(comboBox2.SelectedItem.ToString());
            }
            else
            {
                idCategory = GetCategoryIdFromName(oldCategory);
            }

            if (comboBox1.SelectedItem != null && !string.IsNullOrEmpty(comboBox1.SelectedItem.ToString()))
            {
                idEvent = GetEventIdFromName(comboBox1.SelectedItem.ToString());
            }
            else
            {
                idEvent = GetEventIdFromName(oldEvent);
            }

            string photo = oldPhoto;

            if (pictureBox1.Image != null)
            {
                string newPhotoPath = SaveImageToFolder(pictureBox1.Image);
                if (!string.IsNullOrEmpty(newPhotoPath))
                {
                    photo = newPhotoPath;
                    if (!string.IsNullOrEmpty(oldPhoto))
                    {
                        DeleteOldImageIfNotUsed(oldPhoto, selectedId);
                    }
                }
            }
            else if (!string.IsNullOrEmpty(oldPhoto))
            {
                photo = "";
                DeleteOldImageIfNotUsed(oldPhoto, selectedId);
            }

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Введите название блюда", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox4.Focus();
                return;
            }

            if (string.IsNullOrEmpty(compound))
            {
                MessageBox.Show("Введите состав блюда", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox5.Focus();
                return;
            }

            if (!decimal.TryParse(weightText, out decimal weight) || weight <= 0)
            {
                MessageBox.Show("Введите корректный вес блюда", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox2.Focus();
                return;
            }

            if (!decimal.TryParse(priceText, out decimal price) || price <= 0)
            {
                MessageBox.Show("Введите корректную цену блюда", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox3.Focus();
                return;
            }

            if (IsAnotherDishExistsForEdit(name, compound, idCategory, idEvent, selectedId))
            {
                MessageBox.Show("Такое блюдо уже существует", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string query = @"UPDATE Dishes 
            SET Name = @name, 
                Compound = @compound, 
                Weight = @weight,
                Price = @price,
                Photo = @photo, 
                IdCategory = @idCategory,
                IdEvent = @idEvent
            WHERE Article = @selectedId";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@name", name);
                        cmd.Parameters.AddWithValue("@compound", compound);
                        cmd.Parameters.AddWithValue("@weight", weight);
                        cmd.Parameters.AddWithValue("@price", price);
                        cmd.Parameters.AddWithValue("@photo", photo ?? "");
                        cmd.Parameters.AddWithValue("@idCategory", idCategory);
                        cmd.Parameters.AddWithValue("@idEvent", idEvent);
                        cmd.Parameters.AddWithValue("@selectedId", selectedId);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            // Сбрасываем ID последнего добавленного блюда при редактировании
                            _lastInsertedDishId = null;

                            MessageBox.Show("Блюдо успешно обновлено", "Успех",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);

                            SaveOriginalData();
                            ClearForm();
                            FillDataGridView();
                            ClearAllFields();
                        }
                        else
                        {
                            MessageBox.Show("Блюдо не было обновлено", "Информация",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка обновления блюда: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null)
            {
                MessageBox.Show("Выберите блюдо для удаления", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedIdString = Convert.ToString(dataGridView1.CurrentRow.Cells["Article"].Value);
            int selectedId = Convert.ToInt32(dataGridView1.CurrentRow.Cells["Article"].Value);

            DialogResult result = MessageBox.Show(
                $"Вы уверены, что хотите удалить блюдо с артикулом \"{selectedIdString}\"?",
                "Подтверждение удаления",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            if (IsDishInUse(selectedId))
            {
                MessageBox.Show("Невозможно удалить блюдо, так как оно используется в других таблицах",
                              "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string query = "DELETE FROM Dishes WHERE Article = @dishId";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@dishId", selectedId);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            // Сбрасываем ID последнего добавленного блюда при удалении
                            _lastInsertedDishId = null;

                            MessageBox.Show("Блюдо успешно удалено", "Успех",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);
                            textBox1.Clear();
                            FillDataGridView();
                            ClearAllFields();
                            Pagination();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления блюда: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void DeleteOldImageIfNotUsed(string imageFileName, int currentDishId)
        {
            try
            {
                string query = "SELECT COUNT(*) FROM Dishes WHERE Photo = @photo AND Article != @currentDishId";

                using (MySqlConnection con = new MySqlConnection(conString))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@photo", imageFileName);
                        cmd.Parameters.AddWithValue("@currentDishId", currentDishId);
                        int count = Convert.ToInt32(cmd.ExecuteScalar());

                        if (count == 0)
                        {
                            string resourcesFolder = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
                            string fullPath = Path.Combine(resourcesFolder, imageFileName);

                            if (File.Exists(fullPath))
                            {
                                File.Delete(fullPath);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении файла: {ex.Message}");
            }
        }

        private int GetEventIdFromName(string eventName)
        {
            if (string.IsNullOrEmpty(eventName)) return 0;

            string query = "SELECT IDevent FROM Events WHERE Event = @eventName";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@eventName", eventName);
                        object result = cmd.ExecuteScalar();
                        return result != null ? Convert.ToInt32(result) : 0;
                    }
                }
                catch (Exception ex)
                {
                    return 0;
                }
            }
        }

        private int GetCategoryIdFromName(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName)) return 0;

            string query = "SELECT IDcategory FROM Categories WHERE Category = @categoryName";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@categoryName", categoryName);
                        object result = cmd.ExecuteScalar();
                        return result != null ? Convert.ToInt32(result) : 0;
                    }
                }
                catch (Exception ex)
                {
                    return 0;
                }
            }
        }

        private string GetCurrentPhotoPath(int dishId)
        {
            string query = "SELECT Photo FROM Dishes WHERE Article = @dishId";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@dishId", dishId);
                        object result = cmd.ExecuteScalar();
                        return result?.ToString() ?? "";
                    }
                }
                catch (Exception ex)
                {
                    return "";
                }
            }
        }

        private bool HasDataChanged()
        {
            if (selectedRowData == null)
                return false;

            string currentName = textBox4.Text.Trim();
            string currentCompound = textBox5.Text.Trim();
            string currentWeight = textBox2.Text.Trim();
            string currentPrice = textBox3.Text.Trim();
            string currentEvent = comboBox1.SelectedItem?.ToString() ?? "";
            string currentCategory = comboBox2.SelectedItem?.ToString() ?? "";

            bool textChanged = currentName != originalName ||
                              currentCompound != originalCompound ||
                              currentWeight != originalWeight ||
                              currentPrice != originalPrice ||
                              currentEvent != originalEvent ||
                              currentCategory != originalCategory;

            bool imageChanged = false;

            if (pictureBox1.Image == null && !string.IsNullOrEmpty(originalPhoto))
            {
                imageChanged = true;
            }
            else if (pictureBox1.Image != null && string.IsNullOrEmpty(originalPhoto))
            {
                imageChanged = true;
            }
            else if (pictureBox1.Image != null && !string.IsNullOrEmpty(originalPhoto))
            {
                if (!string.IsNullOrEmpty(originalImageFilePath) || newProductImage != null)
                {
                    imageChanged = true;
                }
            }

            return textChanged || imageChanged;
        }

        private void SaveOriginalData()
        {
            if (selectedRowData != null)
            {
                originalName = textBox4.Text.Trim();
                originalCompound = textBox5.Text.Trim();
                originalWeight = textBox2.Text.Trim();
                originalPrice = textBox3.Text.Trim();
                originalEvent = comboBox1.SelectedItem?.ToString() ?? "";
                originalCategory = comboBox2.SelectedItem?.ToString() ?? "";
                originalPhoto = GetCurrentPhotoPath(Convert.ToInt32(selectedRowData.Cells["Article"].Value));
            }
        }

        void UpdateButtonsState()
        {
            string currentName = textBox4.Text.Trim();
            string currentCompound = textBox5.Text.Trim();
            string currentWeight = textBox2.Text.Trim();
            string currentPrice = textBox3.Text.Trim();

            bool hasValidData = !string.IsNullOrWhiteSpace(currentName) &&
                               !string.IsNullOrWhiteSpace(currentCompound) &&
                               !string.IsNullOrWhiteSpace(currentWeight) &&
                               !string.IsNullOrWhiteSpace(currentPrice) &&
                               comboBox1.SelectedIndex >= 0 &&
                               comboBox2.SelectedIndex >= 0;

            bool dataChanged = HasDataChanged();

            button3.Enabled = (dataGridView1.CurrentRow != null) && hasValidData && dataChanged;
            button5.Enabled = (dataGridView1.CurrentRow != null);
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow != null && dataGridView1.CurrentRow.Index >= 0)
            {
                try
                {
                    originalImageFilePath = null;
                    newProductImage = null;

                    DataGridViewRow selectedRow = dataGridView1.CurrentRow;
                    selectedRowData = selectedRow;

                    textBox1.Text = selectedRow.Cells["Article"].Value?.ToString() ?? "";
                    textBox4.Text = selectedRow.Cells["Name"].Value?.ToString() ?? "";
                    textBox5.Text = selectedRow.Cells["Compound"].Value?.ToString() ?? "";
                    textBox2.Text = selectedRow.Cells["Weight"].Value?.ToString() ?? "";
                    textBox3.Text = selectedRow.Cells["Price"].Value?.ToString() ?? "";

                    string categoryName = selectedRow.Cells["Category"].Value?.ToString() ?? "";
                    string eventName = selectedRow.Cells["Event"].Value?.ToString() ?? "";

                    if (!string.IsNullOrEmpty(categoryName))
                    {
                        int categoryIndex = comboBox2.FindStringExact(categoryName);
                        if (categoryIndex >= 0)
                            comboBox2.SelectedIndex = categoryIndex;
                    }

                    if (!string.IsNullOrEmpty(eventName))
                    {
                        int eventIndex = comboBox1.FindStringExact(eventName);
                        if (eventIndex >= 0)
                            comboBox1.SelectedIndex = eventIndex;
                    }

                    string photoFileName = GetCurrentPhotoPath(Convert.ToInt32(selectedRow.Cells["Article"].Value));
                    LoadImageToPictureBox(photoFileName);

                    SaveOriginalData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при заполнении полей: {ex.Message}");
                }

                UpdateButtonsState();
            }
        }

        private void LoadImageToPictureBox(string photoFileName)
        {
            try
            {
                if (string.IsNullOrEmpty(photoFileName))
                {
                    pictureBox1.Image = null;
                    return;
                }

                string resourcesFolder = @".\Resources\";
                string fullImagePath = Path.Combine(resourcesFolder, photoFileName);

                if (File.Exists(fullImagePath))
                {
                    using (var fs = new FileStream(fullImagePath, FileMode.Open, FileAccess.Read))
                    {
                        pictureBox1.Image = Image.FromStream(fs);
                    }
                }
                else
                {
                    pictureBox1.Image = null;
                }
            }
            catch (Exception ex)
            {
                pictureBox1.Image = null;
            }
        }

        private void ClearForm()
        {
            textBox1.Clear();
            textBox2.Clear();
            textBox3.Clear();
            textBox4.Clear();
            textBox5.Clear();
            pictureBox1.Image = null;
            if (comboBox1.Items.Count > 0) comboBox1.SelectedIndex = 0;
            if (comboBox2.Items.Count > 0) comboBox2.SelectedIndex = 0;

            originalImageFilePath = null;
            newProductImage = null;
            selectedRowData = null;
            originalName = "";
            originalCompound = "";
            originalWeight = "";
            originalPrice = "";
            originalPhoto = "";
            originalEvent = "";
            originalCategory = "";
        }

        private void ClearAllFields()
        {
            dataGridView1.ClearSelection();
            dataGridView1.CurrentCell = null;
            textBox1.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
            textBox4.Text = "";
            textBox5.Text = "";
            pictureBox1.Image = null;
            if (comboBox1.Items.Count > 0) comboBox1.SelectedIndex = -1;
            if (comboBox2.Items.Count > 0) comboBox2.SelectedIndex = -1;
            UpdateButtonsState();
        }

        private void Menu_Load(object sender, EventArgs e)
        {
            ClearAllFields();
            Pagination();
        }

        private bool IsDishInUse(int dishId)
        {
            string checkQueries = @"SELECT COUNT(*) FROM OrderComposition WHERE IdDish = @dishId;";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(checkQueries, con))
                    {
                        cmd.Parameters.AddWithValue("@dishId", dishId);
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
                catch (Exception ex)
                {
                    return true;
                }
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e) => UpdateButtonsState();
        private void textBox3_TextChanged(object sender, EventArgs e) => UpdateButtonsState();
        private void textBox4_TextChanged(object sender, EventArgs e) => UpdateButtonsState();
        private void textBox5_TextChanged(object sender, EventArgs e) => UpdateButtonsState();
        private void textBox1_TextChanged(object sender, EventArgs e) => UpdateButtonsState();
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) => UpdateButtonsState();
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e) => UpdateButtonsState();
    }
}