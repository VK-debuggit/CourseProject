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
    public partial class Menu : Form
    {
        string conString = $"host={Properties.Settings.Default.host};uid={Properties.Settings.Default.uid};pwd={Properties.Settings.Default.pwd};database={Properties.Settings.Default.database};";

        private int rowCount = 0;
        private Image newProductImage; // Поле для нового изображения
        private string originalImageFilePath; // Сохраняем путь к выбранному файлу

        // Поля для хранения исходных данных
        private DataGridViewRow selectedRowData = null;
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
        }

        private bool allowClose = false;

        private void button4_Click(object sender, EventArgs e)
        {
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

        void FillDataGridView()
        {
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
                                    LEFT JOIN Events c ON p.IdEvent = c.IDevent;";

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

                        // Создаем колонку для изображений
                        DataGridViewImageColumn imageColumn = new DataGridViewImageColumn();
                        imageColumn.Name = "Photo";
                        imageColumn.HeaderText = "Фото";
                        imageColumn.ImageLayout = DataGridViewImageCellLayout.Zoom;
                        imageColumn.Width = 80;

                        dataGridView1.Columns.Add("Article", "Артикул");
                        dataGridView1.Columns.Add("Event", "Мероприятие");
                        dataGridView1.Columns.Add("Category", "Категория");
                        dataGridView1.Columns.Add("Name", "Наименование");
                        dataGridView1.Columns.Add("Compound", "Описание");
                        dataGridView1.Columns.Add("Weight", "Вес");
                        dataGridView1.Columns.Add("Price", "Цена");
                        dataGridView1.Columns.Add(imageColumn);

                        rowCount = 0;
                        while (rdr.Read())
                        {
                            string imagesFolder = @".\Resources\";
                            string photoFileName = rdr["Photo"].ToString();
                            string fullImagePath = Path.Combine(imagesFolder, photoFileName);
                            Image img = null;

                            if (!string.IsNullOrEmpty(photoFileName) && File.Exists(fullImagePath))
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
                                rdr["Event"].ToString(),
                                rdr["Category"].ToString(),
                                rdr["Name"].ToString(),
                                rdr["Compound"].ToString(),
                                rdr["Weight"].ToString(),
                                rdr["Price"].ToString(),
                                img
                            );

                            rowCount++;
                        }

                        label14.Text = rowCount.ToString();

                        // Показываем информацию о загруженных данных
                        if (rowCount == 0)
                        {
                            MessageBox.Show("Данные не найдены", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine("Error details: " + ex.ToString());
                }
            }
        }

        void FillFilterCategory()
        {
            MySqlConnection con = new MySqlConnection(conString);
            con.Open();

            MySqlCommand cmd = new MySqlCommand(@"SELECT * FROM CafeActivities.Categories;", con);
            MySqlDataReader rdr = cmd.ExecuteReader();

            comboBox2.Items.Clear();

            while (rdr.Read())
            {
                comboBox2.Items.Add(rdr[1].ToString());
            }

            comboBox2.SelectedIndex = 0;

            con.Close();
        }

        void FillFilterEvent()
        {
            MySqlConnection con = new MySqlConnection(conString);
            con.Open();

            MySqlCommand cmd = new MySqlCommand(@"SELECT * FROM CafeActivities.Events;", con);
            MySqlDataReader rdr = cmd.ExecuteReader();

            comboBox1.Items.Clear();

            while (rdr.Read())
            {
                comboBox1.Items.Add(rdr[1].ToString());
            }

            comboBox1.SelectedIndex = 0;

            con.Close();
        }

        private void textBox4_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox tb = (TextBox)sender;

            if (char.IsControl(e.KeyChar))
                return;

            if (tb.Text.Length > 0 && char.IsLower(tb.Text[0]))
            {
                int cursorPos = tb.SelectionStart;

                // Делаем только первую букву заглавной
                string newText = char.ToUpper(tb.Text[0]) + tb.Text.Substring(1);

                if (tb.Text != newText)
                {
                    tb.Text = newText;
                    tb.SelectionStart = cursorPos;
                }
            }

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

            // Если вводится дефис
            if (e.KeyChar == '-')
            {
                // Запрещаем дефис в начале или после пробела/дефиса
                if (tb.Text.Length == 0 || tb.Text[tb.Text.Length - 1] == ' ' || tb.Text[tb.Text.Length - 1] == '-')
                {
                    e.Handled = true;
                    return;
                }
                e.Handled = false;
                return;
            }

            // Проверяем русские буквы
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
            // Разрешаем управляющие символы
            if (char.IsControl(e.KeyChar))
                return;

            // Цифры
            if (char.IsDigit(e.KeyChar))
                return;

            // Запрещаем все остальные символы
            e.Handled = true;
        }

        private void textBox3_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox tb = (TextBox)sender;

            // Разрешаем управляющие символы
            if (char.IsControl(e.KeyChar))
                return;

            // Разрешаем цифры
            if (char.IsDigit(e.KeyChar))
            {
                // Получаем текст до вставки
                string textBefore = tb.Text.Substring(0, tb.SelectionStart) + tb.Text.Substring(tb.SelectionStart + tb.SelectionLength);

                // Проверяем общую длину числа (с учетом новой цифры)
                if (textBefore.Replace(".", "").Length + 1 > 12) // Максимум 12 цифр (10 до точки + 2 после)
                {
                    e.Handled = true;
                    return;
                }

                // Проверяем цифры после точки
                int dotIndex = tb.Text.IndexOf('.');
                if (dotIndex != -1)
                {
                    int cursorPosition = tb.SelectionStart;
                    int digitsAfterDot = tb.Text.Length - dotIndex - 1;

                    // Если курсор находится после точки и уже есть 2 цифры после точки
                    if (cursorPosition > dotIndex && digitsAfterDot >= 2)
                    {
                        // Если выбрана часть текста, разрешаем замену
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

            // Разрешаем десятичную точку
            if (e.KeyChar == '.')
            {
                // Запрещаем несколько точек
                if (tb.Text.Contains('.'))
                {
                    e.Handled = true;
                    return;
                }

                // Запрещаем точку в начале
                if (tb.Text.Length == 0)
                {
                    e.Handled = true;
                    return;
                }

                // Проверяем, что перед точкой не более 10 цифр
                if (tb.Text.Length > 10)
                {
                    e.Handled = true;
                    return;
                }

                e.Handled = false;
                return;
            }

            // Запрещаем все остальные символы
            e.Handled = true;
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox tb = (TextBox)sender;

            // Разрешаем управляющие символы
            if (char.IsControl(e.KeyChar))
                return;

            // Разрешаем цифры
            if (char.IsDigit(e.KeyChar))
            {
                // Получаем текст до вставки
                string textBefore = tb.Text.Substring(0, tb.SelectionStart) + tb.Text.Substring(tb.SelectionStart + tb.SelectionLength);

                // Проверяем общую длину числа (с учетом новой цифры)
                if (textBefore.Replace(".", "").Length + 1 > 9) // Максимум 9 цифр (7 до точки + 2 после)
                {
                    e.Handled = true;
                    return;
                }

                // Проверяем цифры после точки
                int dotIndex = tb.Text.IndexOf('.');
                if (dotIndex != -1)
                {
                    int cursorPosition = tb.SelectionStart;
                    int digitsAfterDot = tb.Text.Length - dotIndex - 1;

                    // Если курсор находится после точки и уже есть 2 цифры после точки
                    if (cursorPosition > dotIndex && digitsAfterDot >= 2)
                    {
                        // Если выбрана часть текста, разрешаем замену
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

            // Разрешаем десятичную точку
            if (e.KeyChar == '.')
            {
                // Запрещаем несколько точек
                if (tb.Text.Contains('.'))
                {
                    e.Handled = true;
                    return;
                }

                // Запрещаем точку в начале
                if (tb.Text.Length == 0)
                {
                    e.Handled = true;
                    return;
                }

                // Проверяем, что перед точкой не более 7 цифр
                if (tb.Text.Length > 7)
                {
                    e.Handled = true;
                    return;
                }

                e.Handled = false;
                return;
            }

            // Запрещаем все остальные символы
            e.Handled = true;
        }

        private void textBox5_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox tb = (TextBox)sender;

            if (char.IsControl(e.KeyChar))
                return;

            // Разрешаем цифры
            if (char.IsDigit(e.KeyChar))
            {
                e.Handled = false;
                return;
            }

            // Разрешаем десятичную точку или запятую
            if (e.KeyChar == '.')
            {
                // Запрещаем точку/запятую в начале
                if (tb.Text.Length == 0)
                {
                    e.Handled = true;
                    return;
                }

                e.Handled = false;
                return;
            }

            if (tb.Text.Length > 0)
            {
                int cursorPos = tb.SelectionStart;

                // Разделяем текст по точкам с пробелом
                string[] sentences = tb.Text.Split(new[] { ". " }, StringSplitOptions.None);

                // Обрабатываем каждое предложение
                for (int i = 0; i < sentences.Length; i++)
                {
                    if (!string.IsNullOrEmpty(sentences[i]))
                    {
                        // Делаем первую букву каждого предложения заглавной
                        if (sentences[i].Length > 0 && char.IsLower(sentences[i][0]))
                        {
                            sentences[i] = char.ToUpper(sentences[i][0]) + sentences[i].Substring(1);
                        }
                    }
                }

                // Собираем текст обратно
                string newText = string.Join(". ", sentences);

                if (tb.Text != newText)
                {
                    tb.Text = newText;
                    tb.SelectionStart = cursorPos;
                }
            }

            // Если вводится пробел
            if (e.KeyChar == ' ')
            {
                // Запрещаем пробел в начале
                if (tb.Text.Length == 0)
                {
                    e.Handled = true;
                    return;
                }

                // Запрещаем пробел после пробела
                if (tb.Text.Length > 0 && tb.Text[tb.Text.Length - 1] == ' ')
                {
                    e.Handled = true;
                    return;
                }

                // Разрешаем пробел после буквы
                return;
            }

            // Проверяем русские буквы
            if ((e.KeyChar >= 'А' && e.KeyChar <= 'Я') ||
                (e.KeyChar >= 'а' && e.KeyChar <= 'я') ||
                e.KeyChar == 'Ё' || e.KeyChar == 'ё')
                return;

            e.Handled = true;
        }

        // Метод для загрузки изображения в pictureBox из Resources
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
                MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}");
                pictureBox1.Image = null;
            }
        }

        // Метод проверки типа файла
        private bool IsFileTypeValid(string filePath)
        {
            string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };
            string fileExtension = Path.GetExtension(filePath).ToLower();

            return allowedExtensions.Contains(fileExtension);
        }

        // Метод проверки размера файла
        private bool IsFileSizeValid(string filePath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                long maxSize = 2 * 1024 * 1024; // 2 МБ в байтах

                if (fileInfo.Length > maxSize)
                {
                    MessageBox.Show($"Размер файла превышает допустимый лимит 2 МБ.",
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
                        // Сохраняем путь к выбранному файлу
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

                        // Загружаем изображение из выбранного файла
                        newProductImage = Image.FromFile(originalImageFilePath);
                        pictureBox1.Image = newProductImage;

                        // Обновляем состояние кнопок после изменения изображения
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

        // Проверка на уникальность артикула
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

        private string GetNextArticle()
        {
            string query = "SELECT Article FROM Dishes ORDER BY Article DESC LIMIT 1";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        object result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            string lastArticle = result.ToString();

                            // Преобразуем строку в число, увеличиваем на 1 и форматируем обратно
                            if (int.TryParse(lastArticle, out int lastNumber))
                            {
                                int nextNumber = lastNumber + 1;
                                return nextNumber.ToString("D6"); // D6 означает 6 цифр с ведущими нулями
                            }
                        }

                        // Если нет записей или ошибка парсинга, начинаем с 000001
                        return "000001";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка получения последнего артикула: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return "000001";
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Получаем данные из полей ввода
            string article = GetNextArticle();
            string weight = textBox2.Text.Trim();
            string price = textBox3.Text.Trim();
            string nameDish = textBox4.Text.Trim();
            string compound = textBox5.Text.Trim();

            // Получаем выбранные значения из ComboBox
            int idEvent = comboBox1.SelectedIndex + 1;
            int idCategory = comboBox2.SelectedIndex + 1;

            // Получаем путь к изображению
            string photoPath = pictureBox1.Image != null ? SaveImageToFolder(pictureBox1.Image) : "";

            // ВАЛИДАЦИЯ ДАННЫХ (улучшенная)
            if (string.IsNullOrEmpty(nameDish))
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

            if (string.IsNullOrEmpty(weight))
            {
                MessageBox.Show("Введите вес блюда", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox2.Focus();
                return;
            }

            if (string.IsNullOrEmpty(price))
            {
                MessageBox.Show("Введите цену блюда", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox3.Focus();
                return;
            }

            // Проверка числовых значений с учетом разных форматов разделителей
            // Используем CultureInfo.InvariantCulture для корректного парсинга с точкой
            if (!decimal.TryParse(weight, System.Globalization.NumberStyles.Any,
                                 System.Globalization.CultureInfo.InvariantCulture,
                                 out decimal weightValue) || weightValue <= 0)
            {
                MessageBox.Show("Введите корректный вес блюда (положительное число, например: 123.45)", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox2.Focus();
                return;
            }

            if (!decimal.TryParse(price, System.Globalization.NumberStyles.Any,
                                 System.Globalization.CultureInfo.InvariantCulture,
                                 out decimal priceValue) || priceValue <= 0)
            {
                MessageBox.Show("Введите корректную цену блюда (положительное число, например: 123.45)", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox3.Focus();
                return;
            }

            // Дополнительная проверка формата (опционально)
            if (weight.Contains("."))
            {
                string[] weightParts = weight.Split('.');
                if (weightParts.Length > 1 && weightParts[1].Length != 2)
                {
                    MessageBox.Show("Введите вес в формате: до 7 цифр до точки и ровно 2 цифры после точки", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textBox2.Focus();
                    return;
                }
            }

            if (price.Contains("."))
            {
                string[] priceParts = price.Split('.');
                if (priceParts.Length > 1 && priceParts[1].Length != 2)
                {
                    MessageBox.Show("Введите цену в формате: до 10 цифр до точки и ровно 2 цифры после точки", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textBox3.Focus();
                    return;
                }
            }

            // Проверка максимальных значений
            if (weightValue > 9999999.99m)
            {
                MessageBox.Show("Максимальный вес: 9999999.99", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox2.Focus();
                return;
            }

            if (priceValue > 9999999999.99m)
            {
                MessageBox.Show("Максимальная цена: 9999999999.99", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox3.Focus();
                return;
            }

            // Улучшенная проверка дубликатов (обязательные поля)
            if (IsDishExists(nameDish, compound, idCategory, idEvent))
            {
                MessageBox.Show("Такое блюдо уже существует в базе данных.\n\n" +
                               "Измените название, состав или выберите другую категорию/мероприятие.",
                               "Ошибка",
                               MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Уникальность артикула (дополнительная проверка)
            if (IsArticleExists(article))
            {
                MessageBox.Show("Блюдо с таким артикулом уже существует", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Добавление в базу данных
            string query = @"INSERT INTO Dishes (Article, IdEvent, IdCategory, Name, Compound, Weight, Price, Photo) 
     VALUES (@article, @idEvent, @idCategory, @nameDishes, @compoundDishes, @weight, @price, @photo)";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        // Сохраняем значения как decimal для базы данных
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
                            MessageBox.Show("Блюдо успешно добавлено", "Успех",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);
                            ClearForm();
                            FillDataGridView();
                            ClearAllFields();
                        }
                        else
                        {
                            MessageBox.Show("Не удалось добавить блюдо", "Ошибка",
                                          MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                catch (FormatException)
                {
                    MessageBox.Show("Неверный формат данных", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (MySqlException ex)
                {
                    // Обработка ошибок MySQL (например, нарушение уникальности)
                    if (ex.Number == 1062) // Ошибка дублирования ключа в MySQL
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

        // Метод проверки на дублирование блюда
        private bool IsDishExists(string nameDish, string compound, int idCategory, int idEvent)
        {
            // Проверяем по комбинации обязательных полей
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
                    return true; // В случае ошибки считаем, что блюдо существует
                }
            }
        }

        // Метод для проверки дубликатов при редактировании
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

        // Метод для сохранения изображения в папку Resources
        private string SaveImageToFolder(Image image)
        {
            try
            {
                if (image == null)
                    return "";

                // Создаем папку Resources если ее нет
                string resourcesFolder = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
                if (!Directory.Exists(resourcesFolder))
                {
                    Directory.CreateDirectory(resourcesFolder);
                }

                // Генерируем уникальное имя файла с использованием GUID
                string fileName = $"dish_{Guid.NewGuid():N}.jpg";
                string fullPath = Path.Combine(resourcesFolder, fileName);

                // Создаем КОПИЮ изображения для сохранения
                using (Bitmap bmp = new Bitmap(image.Width, image.Height))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.DrawImage(image, 0, 0, image.Width, image.Height);
                    }

                    // Сохраняем КОПИЮ изображения
                    bmp.Save(fullPath, System.Drawing.Imaging.ImageFormat.Jpeg);
                }

                return fileName; // Возвращаем только имя файла
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения изображения: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                return "";
            }
        }

        // Метод очистки формы
        private void ClearForm()
        {
            textBox1.Clear();
            textBox2.Clear();
            textBox3.Clear();
            textBox4.Clear();
            textBox5.Clear();
            pictureBox1.Image = null;
            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;

            // Сбрасываем путь к файлу
            originalImageFilePath = null;
            newProductImage = null;

            // Сбрасываем сохраненные данные
            selectedRowData = null;
            originalName = "";
            originalCompound = "";
            originalWeight = "";
            originalPrice = "";
            originalPhoto = "";
            originalEvent = "";
            originalCategory = "";
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null)
            {
                MessageBox.Show("Выберите блюдо для редактирования", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Получаем ID выбранного блюда и старые данные
            int selectedId = Convert.ToInt32(dataGridView1.CurrentRow.Cells["Article"].Value);
            DataGridViewRow selectedRow = dataGridView1.CurrentRow;

            // Получаем старые значения из выбранной строки
            string oldCategory = selectedRow.Cells["Category"].Value?.ToString() ?? "";
            string oldEvent = selectedRow.Cells["Event"].Value?.ToString() ?? "";
            string oldPhoto = GetCurrentPhotoPath(selectedId);

            // Новые значения из полей ввода
            string name = textBox4.Text.Trim();
            string compound = textBox5.Text.Trim();
            string weightText = textBox2.Text.Trim();
            string priceText = textBox3.Text.Trim();

            // ПРАВИЛЬНОЕ получение ID категории и события
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

            // УПРОЩЕННАЯ РАБОТА С ИЗОБРАЖЕНИЕМ
            string photo = oldPhoto; // По умолчанию используем старое фото

            // Если в pictureBox есть новое изображение - сохраняем его
            if (pictureBox1.Image != null)
            {
                // Всегда сохраняем новое изображение при редактировании, если оно есть
                string newPhotoPath = SaveImageToFolder(pictureBox1.Image);
                if (!string.IsNullOrEmpty(newPhotoPath))
                {
                    photo = newPhotoPath;

                    // Удаляем старое изображение, если оно существует и не используется другими записями
                    if (!string.IsNullOrEmpty(oldPhoto))
                    {
                        DeleteOldImageIfNotUsed(oldPhoto, selectedId);
                    }
                }
            }
            // Если pictureBox пустой, но старое фото было - значит фото удалили
            else if (!string.IsNullOrEmpty(oldPhoto))
            {
                photo = ""; // Очищаем фото
                DeleteOldImageIfNotUsed(oldPhoto, selectedId);
            }

            // ВАЛИДАЦИЯ ДАННЫХ
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

            // Проверка на существование (исключая текущее блюдо)
            if (IsAnotherDishExistsForEdit(name, compound, idCategory, idEvent, selectedId))
            {
                MessageBox.Show("Такое блюдо уже существует", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // ОБНОВЛЕНИЕ в базе данных
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
                            MessageBox.Show("Блюдо успешно обновлено", "Успех",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);

                            // Обновляем сохраненные данные
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

        // Метод для удаления старых изображений
        private void DeleteOldImageIfNotUsed(string imageFileName, int currentDishId)
        {
            try
            {
                // Проверяем, используется ли это изображение другими блюдами
                string query = "SELECT COUNT(*) FROM Dishes WHERE Photo = @photo AND Article != @currentDishId";

                using (MySqlConnection con = new MySqlConnection(conString))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@photo", imageFileName);
                        cmd.Parameters.AddWithValue("@currentDishId", currentDishId);

                        int count = Convert.ToInt32(cmd.ExecuteScalar());

                        // Если изображение не используется другими блюдами, удаляем файл
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
                // Не блокируем выполнение при ошибке удаления файла
                Console.WriteLine($"Ошибка при удалении файла: {ex.Message}");
            }
        }

        // Улучшенный метод получения ID события
        private int GetEventIdFromName(string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                // Если событие не указано, пытаемся получить из выбранной строки
                if (dataGridView1.CurrentRow != null)
                {
                    string oldEvent = dataGridView1.CurrentRow.Cells["Event"].Value?.ToString() ?? "";
                    eventName = oldEvent;
                }

                if (string.IsNullOrEmpty(eventName))
                    return 0;
            }

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
                    MessageBox.Show($"Ошибка получения ID события: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return 0;
                }
            }
        }

        // Улучшенный метод получения ID категории
        private int GetCategoryIdFromName(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName))
            {
                // Если категория не указана, пытаемся получить из выбранной строки
                if (dataGridView1.CurrentRow != null)
                {
                    string oldCategory = dataGridView1.CurrentRow.Cells["Category"].Value?.ToString() ?? "";
                    categoryName = oldCategory;
                }

                if (string.IsNullOrEmpty(categoryName))
                    return 0;
            }

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
                    MessageBox.Show($"Ошибка получения ID категории: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return 0;
                }
            }
        }

        // Получение текущего пути фото из базы данных
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
                    MessageBox.Show($"Ошибка получения фото: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return "";
                }
            }
        }

        // Метод для проверки, изменились ли данные
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

            // Проверяем текстовые поля
            bool textChanged = currentName != originalName ||
                              currentCompound != originalCompound ||
                              currentWeight != originalWeight ||
                              currentPrice != originalPrice ||
                              currentEvent != originalEvent ||
                              currentCategory != originalCategory;

            // Проверяем изображение
            bool imageChanged = false;

            if (pictureBox1.Image == null && !string.IsNullOrEmpty(originalPhoto))
            {
                // Было фото, стало пусто
                imageChanged = true;
            }
            else if (pictureBox1.Image != null && string.IsNullOrEmpty(originalPhoto))
            {
                // Было пусто, стало фото
                imageChanged = true;
            }
            else if (pictureBox1.Image != null && !string.IsNullOrEmpty(originalPhoto))
            {
                // Есть новое изображение (из файла или другого источника)
                if (!string.IsNullOrEmpty(originalImageFilePath) || newProductImage != null)
                {
                    imageChanged = true;
                }
            }

            return textChanged || imageChanged;
        }

        // Метод для сохранения исходных данных
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

            // Проверяем только обязательные поля
            bool hasValidData = !string.IsNullOrWhiteSpace(currentName) &&
                               !string.IsNullOrWhiteSpace(currentCompound) &&
                               !string.IsNullOrWhiteSpace(currentWeight) &&
                               !string.IsNullOrWhiteSpace(currentPrice) &&
                                  comboBox1.SelectedIndex >= 0 &&
                                  comboBox2.SelectedIndex >= 0;

            // Проверяем, изменились ли данные
            bool dataChanged = HasDataChanged();

            button2.Enabled = hasValidData;

            // Кнопка редактирования доступна когда выбрана запись, есть валидные данные И данные изменились
            button3.Enabled = (dataGridView1.CurrentRow != null) && hasValidData && dataChanged;

            // Кнопка удаления доступна только когда выбрана запись
            button5.Enabled = (dataGridView1.CurrentRow != null);
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow != null && dataGridView1.CurrentRow.Index >= 0)
            {
                try
                {
                    // Сбрасываем путь к файлу при выборе новой записи
                    originalImageFilePath = null;
                    newProductImage = null;

                    // Заполняем поля данными из выбранной строки
                    DataGridViewRow selectedRow = dataGridView1.CurrentRow;

                    // Сохраняем ссылку на выбранную строку
                    selectedRowData = selectedRow;

                    // Основные данные
                    textBox1.Text = selectedRow.Cells["Article"].Value?.ToString() ?? "";
                    textBox4.Text = selectedRow.Cells["Name"].Value?.ToString() ?? "";
                    textBox5.Text = selectedRow.Cells["Compound"].Value?.ToString() ?? "";
                    textBox2.Text = selectedRow.Cells["Weight"].Value?.ToString() ?? "";
                    textBox3.Text = selectedRow.Cells["Price"].Value?.ToString() ?? "";

                    // Устанавливаем категорию и мероприятие
                    string categoryName = selectedRow.Cells["Category"].Value?.ToString() ?? "";
                    string eventName = selectedRow.Cells["Event"].Value?.ToString() ?? "";

                    // Устанавливаем выбранные значения в комбобоксы
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

                    // Сохраняем оригинальные данные для сравнения
                    SaveOriginalData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при заполнении полей: {ex.Message}");
                }

                // Обновляем состояние кнопок
                UpdateButtonsState();
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            // Обновляем состояние кнопок
            UpdateButtonsState();
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            // Обновляем состояние кнопок
            UpdateButtonsState();
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            // Обновляем состояние кнопок
            UpdateButtonsState();
        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            // Обновляем состояние кнопок
            UpdateButtonsState();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            // Обновляем состояние кнопок
            UpdateButtonsState();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Обновляем состояние кнопок
            UpdateButtonsState();
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Обновляем состояние кнопок
            UpdateButtonsState();
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
            comboBox1.SelectedIndex = -1;
            comboBox2.SelectedIndex = -1;
            UpdateButtonsState();
        }

        private void Menu_Load(object sender, EventArgs e)
        {
            // Очищаем все поля при загрузке формы
            ClearAllFields();
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
                        if (count > 0)
                        {
                            return true;
                        }
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка проверки использования блюда: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return true;
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

            // Подтверждение удаления
            DialogResult result = MessageBox.Show(
                $"Вы уверены, что хотите удалить блюдо с этим артикулом \"{selectedIdString}\"?",
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

            // Удаление из базы данных
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
                            MessageBox.Show("Блюдо успешно удалено", "Успех",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);
                            textBox1.Clear();
                            FillDataGridView();
                            ClearAllFields();
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
    }
}