using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kursovaya
{
    public partial class CreatingAClient : Form
    {
        string conString = $"host={Properties.Settings.Default.host};uid={Properties.Settings.Default.uid};pwd={Properties.Settings.Default.pwd};database={Properties.Settings.Default.database};";
        private Timer inactivityTimer;
        private int inactivityTimeout;
        private int rowCount = 0;
        private string originalName = "";
        private string originalPhone = "";
        private System.Windows.Forms.Timer searchTimer;
        private bool isSearching = false; // Флаг для отслеживания поиска

        // Статические поля для передачи данных
        public static string SelectedClientName { get; set; } = "";
        public static string SelectedClientPhone { get; set; } = "";
        public static bool ClientWasSelected { get; set; } = false;

        public CreatingAClient()
        {
            InitializeComponent();

            // Инициализация таймера для задержки поиска
            searchTimer = new System.Windows.Forms.Timer();
            searchTimer.Interval = 500; // 500 мс задержка
            searchTimer.Tick += SearchTimer_Tick;

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

            button1.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button2.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button3.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button4.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button5.BackColor = System.Drawing.Color.FromArgb(217, 152, 22); // Цвет для кнопки очистки
            textBox1.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            maskedTextBox1.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            maskedTextBox2.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
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

            // Настройка MaskedTextBox для ввода данных
            maskedTextBox1.Mask = "+7 (000) 000-00-00";
            maskedTextBox1.PromptChar = '_';
            maskedTextBox1.TextMaskFormat = MaskFormat.IncludePromptAndLiterals;

            // Настройка MaskedTextBox для поиска
            maskedTextBox2.Mask = "+7 (000) 000-00-00";
            maskedTextBox2.PromptChar = '_';
            maskedTextBox2.TextMaskFormat = MaskFormat.IncludePromptAndLiterals;

            // Добавляем обработчик события Enter для автоматической установки курсора
            maskedTextBox1.Enter += maskedTextBox1_Enter;
            maskedTextBox1.Click += maskedTextBox1_Click;

            // Обработчики для поля поиска
            maskedTextBox2.TextChanged += maskedTextBox2_TextChanged;
            maskedTextBox2.Enter += maskedTextBox2_Enter;
            maskedTextBox2.Click += maskedTextBox2_Click;

            // Обработчик для кнопки очистки поиска
            button5.Click += button5_Click;
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

        // Обработчик для кнопки очистки поля поиска
        private void button5_Click(object sender, EventArgs e)
        {
            ClearSearchField();
        }

        // Метод для очистки поля поиска
        private void ClearSearchField()
        {
            // Останавливаем таймер поиска
            searchTimer.Stop();

            // Очищаем поле поиска
            maskedTextBox2.Text = "";

            // Показываем все записи
            FillDataGridView();
        }

        private void maskedTextBox1_Enter(object sender, EventArgs e)
        {
            SetCursorToFirstEmptyPosition(maskedTextBox1);
        }

        private void maskedTextBox1_Click(object sender, EventArgs e)
        {
            SetCursorToFirstEmptyPosition(maskedTextBox1);
        }

        private void maskedTextBox2_Enter(object sender, EventArgs e)
        {
            SetCursorToFirstEmptyPosition(maskedTextBox2);
        }

        private void maskedTextBox2_Click(object sender, EventArgs e)
        {
            SetCursorToFirstEmptyPosition(maskedTextBox2);
        }

        // Метод для установки курсора на первую пустую позицию в маске
        private void SetCursorToFirstEmptyPosition(MaskedTextBox maskedTextBox)
        {
            // Проверяем, есть ли уже введенный номер
            if (!string.IsNullOrWhiteSpace(maskedTextBox.Text) &&
                !maskedTextBox.Text.Contains(maskedTextBox.PromptChar.ToString()))
            {
                // Если номер уже введен полностью, ставим курсор в конец
                maskedTextBox.SelectionStart = maskedTextBox.Text.Length;
                return;
            }

            // Ищем первую позицию с символом-заполнителем (PromptChar)
            for (int i = 0; i < maskedTextBox.Text.Length; i++)
            {
                if (maskedTextBox.Text[i] == maskedTextBox.PromptChar)
                {
                    maskedTextBox.SelectionStart = i;
                    maskedTextBox.SelectionLength = 0;
                    return;
                }
            }

            // Если не нашли пустых позиций, ставим курсор в конец
            maskedTextBox.SelectionStart = maskedTextBox.Text.Length;
        }

        // Обработчик изменения текста в поле поиска
        private void maskedTextBox2_TextChanged(object sender, EventArgs e)
        {
            // Останавливаем предыдущий таймер
            searchTimer.Stop();

            // Запускаем новый таймер
            searchTimer.Start();
        }

        private void SearchTimer_Tick(object sender, EventArgs e)
        {
            searchTimer.Stop();

            string searchText = maskedTextBox2.Text;

            // Если поле пустое - показываем все
            if (string.IsNullOrWhiteSpace(searchText))
            {
                FillDataGridView();
                return;
            }

            // Убираем символы маски, оставляем только цифры
            string digitsOnly = new string(searchText.Where(char.IsDigit).ToArray());

            // Если есть хотя бы 1 цифра - ищем
            if (digitsOnly.Length > 0)
            {
                isSearching = true; // Устанавливаем флаг поиска
                FillDataGridViewWithSearch(digitsOnly);
                isSearching = false; // Сбрасываем флаг
            }
            else
            {
                FillDataGridView();
            }
        }

        private bool allowClose = false;

        private void button4_Click(object sender, EventArgs e)
        {
            // Проверяем, выбрана ли строка
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите клиента из списка", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Получаем выбранную строку
            DataGridViewRow selectedRow = dataGridView1.SelectedRows[0];

            // Проверяем, что строка не пустая
            if (selectedRow == null || selectedRow.Cells["Name"].Value == null)
            {
                MessageBox.Show("Выберите клиента из списка", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Сохраняем выбранные данные
            SelectedClientName = selectedRow.Cells["Name"].Value.ToString();
            SelectedClientPhone = selectedRow.Cells["NumberPhone"].Value?.ToString() ?? "";
            ClientWasSelected = true;

            inactivityTimer.Stop();
            // Закрываем форму
            allowClose = true;
            this.Close();
        }

        private void CreatingAClient_FormClosing(object sender, FormClosingEventArgs e)
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

        void FillDataGridView(string where = "")
        {
            string SelectQuery = @"SELECT IDclient, Name, NumberPhone FROM CafeActivities.Clients ORDER BY Name ASC";

            List<string> conditions = new List<string>();

            // Добавляем условие поиска 
            if (!string.IsNullOrEmpty(where))
            {
                // Убираем все нецифровые символы для поиска
                string digitsOnly = new string(where.Where(char.IsDigit).ToArray());
                if (!string.IsNullOrEmpty(digitsOnly))
                {
                    conditions.Add($"(NumberPhone LIKE '%{digitsOnly}%')");
                }
            }

            // Объединяем все условия
            if (conditions.Count > 0)
            {
                SelectQuery += " WHERE " + string.Join(" AND ", conditions);
            }

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                con.Open();

                using (MySqlCommand cmd = new MySqlCommand(SelectQuery, con))
                using (MySqlDataReader rdr = cmd.ExecuteReader())
                {
                    dataGridView1.Rows.Clear();
                    dataGridView1.Columns.Clear();

                    dataGridView1.Columns.Add("IDclient", "Id");
                    dataGridView1.Columns["IDclient"].Visible = false;
                    dataGridView1.Columns.Add("Name", "ФИО");
                    dataGridView1.Columns.Add("NumberPhone", "Номер телефона");

                    rowCount = 0;
                    while (rdr.Read())
                    {
                        int rowIndex = dataGridView1.Rows.Add(
                            rdr[0].ToString(),
                            rdr[1].ToString(),
                            rdr[2].ToString()
                        );

                        rowCount++;
                    }

                    label5.Text = rowCount.ToString();

                    // Показываем информацию о загруженных данных только если был поиск
                    if (rowCount == 0 && !string.IsNullOrEmpty(where))
                    {
                        MessageBox.Show("Данные не найдены", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    dataGridView1.ClearSelection();
                    dataGridView1.CurrentCell = null;

                    ClearDataFieldsWithoutEvents();
                }
            }
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox tb = (TextBox)sender;

            if (char.IsControl(e.KeyChar))
                return;

            if (!string.IsNullOrEmpty(tb.Text))
            {
                int cursorPos = tb.SelectionStart;

                // Делаем каждое слово с заглавной буквы
                string newText = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(tb.Text.ToLower());

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

        private void maskedTextBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void maskedTextBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private bool IsClientExists(string numberPhone)
        {
            string query = "SELECT COUNT(*) FROM Clients WHERE NumberPhone = @numberPhone;";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@numberPhone", numberPhone.Trim());

                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка проверки клиента: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return true;
                }
            }
        }

        private bool IsEmptyPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return true;

            // Удаляем все нецифровые символы
            string digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());

            // Проверяем минимальное количество цифр
            return digitsOnly.Length < 10;
        }

        private string FormatFIO(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return fullName;

            string[] parts = fullName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
                return fullName;

            // Обрабатываем фамилию (может быть двойной через дефис)
            string lastName = parts[0];

            string initials = "";

            // Обрабатываем имя
            if (parts.Length > 1)
            {
                string firstName = parts[1];
                // Проверяем, не является ли имя двойным (через дефис)
                if (firstName.Contains('-'))
                {
                    // Для двойных имен берем первые буквы каждой части
                    string[] firstNames = firstName.Split('-');
                    initials = string.Join("", firstNames.Select(n => n[0] + "."));
                }
                else
                {
                    initials = firstName[0] + ".";
                }
            }

            // Обрабатываем отчество
            if (parts.Length > 2)
            {
                string patronymic = parts[2];
                if (patronymic.Contains('-'))
                {
                    // Для двойных отчеств берем первые буквы каждой части
                    string[] patronymics = patronymic.Split('-');
                    initials += string.Join("", patronymics.Select(p => p[0] + "."));
                }
                else
                {
                    initials += patronymic[0] + ".";
                }
            }

            return $"{lastName} {initials}".Trim();
        }

        private string FormatPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return phoneNumber;

            // Убираем все символы, кроме цифр
            string digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());

            // Если номер начинается с 7 или 8 и имеет 11 цифр
            if (digitsOnly.Length == 11 && (digitsOnly[0] == '7' || digitsOnly[0] == '8'))
            {
                // Форматируем как +7(XXX)XXX-XX-XX
                return $"+7({digitsOnly.Substring(1, 3)}){digitsOnly.Substring(4, 3)}-{digitsOnly.Substring(7, 2)}-{digitsOnly.Substring(9, 2)}";
            }
            // Если номер имеет 10 цифр (без кода страны)
            else if (digitsOnly.Length == 10)
            {
                // Форматируем как +7(XXX)XXX-XX-XX
                return $"+7({digitsOnly.Substring(0, 3)}){digitsOnly.Substring(3, 3)}-{digitsOnly.Substring(6, 2)}-{digitsOnly.Substring(8, 2)}";
            }

            // Если формат не распознан, возвращаем как есть
            return phoneNumber;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string nameUser = textBox1.Text.Trim();
            string numberPhone = maskedTextBox1.Text.Trim();

            if (IsEmptyPhoneNumber(numberPhone))
            {
                MessageBox.Show("Заполните поле телефона", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Преобразуем телефон в нужный формат
            string formattedPhone = FormatPhoneNumber(numberPhone);

            // Проверка на существование
            if (IsClientExists(formattedPhone))
            {
                MessageBox.Show("Клиент с таким телефоном уже существует", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Валидация данных
            if (string.IsNullOrEmpty(nameUser))
            {
                MessageBox.Show("Заполните поле ФИО", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string query = "INSERT INTO Clients (Name, NumberPhone) VALUES (@name, @numberPhone)";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        // Преобразуем ФИО в нужный формат
                        string formattedName = FormatFIO(nameUser);

                        cmd.Parameters.AddWithValue("@name", formattedName);
                        cmd.Parameters.AddWithValue("@numberPhone", formattedPhone);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Клиент успешно добавлен", "Успех",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);
                            ClearAllFields();
                            FillDataGridView();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка добавления клиента: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите клиента для редактирования", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataGridViewRow selectedRow = dataGridView1.SelectedRows[0];
            int selectedId = Convert.ToInt32(selectedRow.Cells["IDclient"].Value);
            string newClientName = textBox1.Text.Trim();
            string newClientNumber = maskedTextBox1.Text.Trim();

            if (IsEmptyPhoneNumber(newClientNumber))
            {
                MessageBox.Show("Заполните поле телефона", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(newClientName))
            {
                MessageBox.Show("Заполните поле ФИО", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Преобразуем телефон в нужный формат
            string formattedPhone = FormatPhoneNumber(newClientNumber);

            // Проверка на существование (исключая текущего клиента)
            if (IsAnotherClientExists(formattedPhone, selectedId))
            {
                MessageBox.Show("Клиент с таким телефоном уже существует", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string query = "UPDATE Clients SET Name = @clientName, NumberPhone = @clientNumber WHERE IDclient = @selectedId;";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        // Преобразуем ФИО в нужный формат
                        string formattedName = FormatFIO(newClientName);

                        cmd.Parameters.AddWithValue("@clientName", formattedName);
                        cmd.Parameters.AddWithValue("@clientNumber", formattedPhone);
                        cmd.Parameters.AddWithValue("@selectedId", selectedId);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Клиент успешно обновлен", "Успех",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);
                            ClearAllFields();
                            FillDataGridView();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка обновления клиента: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private bool IsAnotherClientExists(string numberPhone, int currentClientId)
        {
            string query = "SELECT COUNT(*) FROM Clients WHERE NumberPhone = @numberPhone AND IDclient != @currentClientId;";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@numberPhone", numberPhone.Trim());
                        cmd.Parameters.AddWithValue("@currentClientId", currentClientId);

                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка проверки клиента: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return true;
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите клиента для удаления", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataGridViewRow selectedRow = dataGridView1.SelectedRows[0];
            int selectedId = Convert.ToInt32(selectedRow.Cells["IDclient"].Value);
            string clientNumber = selectedRow.Cells["NumberPhone"].Value.ToString();

            // Подтверждение удаления
            DialogResult result = MessageBox.Show(
                $"Вы уверены, что хотите удалить клиента с телефоном \"{clientNumber}\"?",
                "Подтверждение удаления",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            // Проверка на использование статуса в других таблицах (опционально)
            if (IsClientInUse(selectedId))
            {
                MessageBox.Show("Невозможно удалить клиента, так как он используется в других таблицах",
                              "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Удаление из базы данных
            string query = "DELETE FROM Clients WHERE IDclient = @clientId";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@clientId", selectedId);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Клиент успешно удален", "Успех",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);
                            ClearAllFields();
                            FillDataGridView();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления клиента: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (!isSearching) // Не обновляем кнопки во время поиска
                UpdateButtonsState();
        }

        private void maskedTextBox1_TextChanged(object sender, EventArgs e)
        {
            if (!isSearching) // Не обновляем кнопки во время поиска
                UpdateButtonsState();
        }

        void UpdateButtonsState()
        {
            string currentTextName = textBox1.Text.Trim();
            string currentTextNumber = maskedTextBox1.Text.Trim();

            bool hasValidName = !string.IsNullOrWhiteSpace(currentTextName);
            bool hasValidNumber = !IsEmptyPhoneNumber(currentTextNumber);
            bool hasSelection = dataGridView1.SelectedRows.Count > 0;

            // Кнопка добавления доступна только когда есть валидные данные
            button1.Enabled = hasValidName && hasValidNumber;

            if (hasSelection && hasValidName && hasValidNumber)
            {
                bool dataChanged = false;

                if (!string.IsNullOrEmpty(originalName) || !string.IsNullOrEmpty(originalPhone))
                {
                    dataChanged = (currentTextName != originalName || currentTextNumber != originalPhone);
                }
                else
                {
                    dataChanged = true; // Если оригинальные данные не сохранены, считаем что изменились
                }

                button2.Enabled = dataChanged;
            }
            else
            {
                button2.Enabled = false;
            }

            // Кнопка удаления доступна только когда есть выделение
            button3.Enabled = hasSelection;

            // Кнопка "Вернуться в заказ" доступна только когда есть выделение
            button4.Enabled = hasSelection;

            // Кнопка "Очистить поиск" всегда активна (если нужна очистка)
            button5.Enabled = true;
        }

        private bool IsClientInUse(int clientId)
        {
            string checkQuery = @"SELECT COUNT(*) FROM Orders WHERE IDclient = @clientId;";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();

                    using (MySqlCommand cmd = new MySqlCommand(checkQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@clientId", clientId);

                        int count = Convert.ToInt32(cmd.ExecuteScalar());

                        return count > 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка проверки использования клиента: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return true;
                }
            }
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            // Если идет поиск - не обрабатываем изменение выделения
            if (isSearching)
                return;

            // Если есть выделенная строка - заполняем поля, если нет - очищаем
            if (dataGridView1.SelectedRows.Count > 0)
            {
                DataGridViewRow selectedRow = dataGridView1.SelectedRows[0];
                if (selectedRow != null && selectedRow.Index >= 0)
                {
                    textBox1.Text = selectedRow.Cells["Name"].Value?.ToString() ?? "";
                    maskedTextBox1.Text = selectedRow.Cells["NumberPhone"].Value?.ToString() ?? "";

                    // Сохраняем оригинальные данные для сравнения
                    originalName = textBox1.Text;
                    originalPhone = maskedTextBox1.Text;
                }

                button2.Enabled = true;
                button3.Enabled = true;
            }
            else
            {
                // Если нет выделенной строки, очищаем поля ввода данных
                textBox1.Text = "";
                maskedTextBox1.Text = "";
                originalName = "";
                originalPhone = "";
            }

            // Обновляем состояние кнопок
            UpdateButtonsState();
        }

        private void CreatingAClient_Load(object sender, EventArgs e)
        {
            // Очищаем все поля при загрузке формы
            ClearAllFields();
        }

        void FillDataGridViewWithSearch(string searchDigits)
        {
            string SelectQuery = @"SELECT IDclient, Name, NumberPhone FROM CafeActivities.Clients 
                                  WHERE REPLACE(REPLACE(REPLACE(REPLACE(NumberPhone, '+', ''), '(', ''), ')', ''), '-', '') 
                                  LIKE CONCAT('%', @searchDigits, '%') 
                                  ORDER BY Name";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();

                    using (MySqlCommand cmd = new MySqlCommand(SelectQuery, con))
                    {
                        // Используем параметризованный запрос для безопасности
                        cmd.Parameters.AddWithValue("@searchDigits", searchDigits);

                        using (MySqlDataReader rdr = cmd.ExecuteReader())
                        {
                            dataGridView1.Rows.Clear();
                            dataGridView1.Columns.Clear();

                            dataGridView1.Columns.Add("IDclient", "Id");
                            dataGridView1.Columns["IDclient"].Visible = false;
                            dataGridView1.Columns.Add("Name", "ФИО");
                            dataGridView1.Columns.Add("NumberPhone", "Номер телефона");

                            rowCount = 0;
                            while (rdr.Read())
                            {
                                int rowIndex = dataGridView1.Rows.Add(
                                    rdr[0].ToString(),
                                    rdr[1].ToString(),
                                    rdr[2].ToString()
                                );

                                rowCount++;
                            }

                            label5.Text = rowCount.ToString();

                            // Только снимаем выделение, но НЕ очищаем поля ввода данных
                            dataGridView1.ClearSelection();
                            dataGridView1.CurrentCell = null;

                            // Очищаем оригинальные данные, но не поля ввода
                            originalName = "";
                            originalPhone = "";

                            // Если ничего не найдено
                            if (rowCount == 0 && searchDigits.Length > 0)
                            {
                                MessageBox.Show("Клиенты не найдены", "Информация",
                                              MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка поиска: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    // В случае ошибки показываем все записи
                    FillDataGridView();
                }
            }
        }

        // Метод для очистки полей данных без вызова событий
        private void ClearDataFieldsWithoutEvents()
        {
            // Отключаем события на время очистки
            textBox1.TextChanged -= textBox1_TextChanged;
            maskedTextBox1.TextChanged -= maskedTextBox1_TextChanged;

            textBox1.Text = "";
            maskedTextBox1.Text = "";
            originalName = "";
            originalPhone = "";

            // Включаем события обратно
            textBox1.TextChanged += textBox1_TextChanged;
            maskedTextBox1.TextChanged += maskedTextBox1_TextChanged;
        }

        // Новый метод для очистки всех полей и снятия выделения
        private void ClearAllFields()
        {
            ClearDataFieldsWithoutEvents();
            dataGridView1.ClearSelection();
            dataGridView1.CurrentCell = null;
            UpdateButtonsState();
        }

        // Обработчик клика по DataGridView
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                ClearAllFields();
            }
        }
    }
}