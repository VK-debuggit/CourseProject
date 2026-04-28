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
        private int? _lastInsertedClientId = null; // Хранит ID последнего добавленного клиента

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
            button5.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
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

        private void button5_Click(object sender, EventArgs e)
        {
            ClearSearchField();
        }

        private void ClearSearchField()
        {
            searchTimer.Stop();
            maskedTextBox2.Text = "";
            _lastInsertedClientId = null; // Сбрасываем ID при очистке поиска
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

        private void SetCursorToFirstEmptyPosition(MaskedTextBox maskedTextBox)
        {
            if (!string.IsNullOrWhiteSpace(maskedTextBox.Text) &&
                !maskedTextBox.Text.Contains(maskedTextBox.PromptChar.ToString()))
            {
                maskedTextBox.SelectionStart = maskedTextBox.Text.Length;
                return;
            }

            for (int i = 0; i < maskedTextBox.Text.Length; i++)
            {
                if (maskedTextBox.Text[i] == maskedTextBox.PromptChar)
                {
                    maskedTextBox.SelectionStart = i;
                    maskedTextBox.SelectionLength = 0;
                    return;
                }
            }

            maskedTextBox.SelectionStart = maskedTextBox.Text.Length;
        }

        private void maskedTextBox2_TextChanged(object sender, EventArgs e)
        {
            searchTimer.Stop();
            searchTimer.Start();
        }

        private void SearchTimer_Tick(object sender, EventArgs e)
        {
            searchTimer.Stop();

            string searchText = maskedTextBox2.Text;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                _lastInsertedClientId = null; // Сбрасываем ID при очистке поиска
                FillDataGridView();
                return;
            }

            string digitsOnly = new string(searchText.Where(char.IsDigit).ToArray());

            if (digitsOnly.Length > 0)
            {
                isSearching = true;
                FillDataGridViewWithSearch(digitsOnly);
                isSearching = false;
            }
            else
            {
                FillDataGridView();
            }
        }

        private bool allowClose = false;

        private void button4_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите клиента из списка", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataGridViewRow selectedRow = dataGridView1.SelectedRows[0];

            if (selectedRow == null || selectedRow.Cells["Name"].Value == null)
            {
                MessageBox.Show("Выберите клиента из списка", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SelectedClientName = selectedRow.Cells["Name"].Value.ToString();
            SelectedClientPhone = selectedRow.Cells["NumberPhone"].Value?.ToString() ?? "";
            ClientWasSelected = true;

            inactivityTimer.Stop();
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

            if (!string.IsNullOrEmpty(where))
            {
                string digitsOnly = new string(where.Where(char.IsDigit).ToArray());
                if (!string.IsNullOrEmpty(digitsOnly))
                {
                    conditions.Add($"(NumberPhone LIKE '%{digitsOnly}%')");
                }
            }

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

                    // Временный список для хранения всех записей
                    var clients = new List<(int Id, string Name, string Phone)>();
                    rowCount = 0;

                    while (rdr.Read())
                    {
                        int clientId = Convert.ToInt32(rdr[0]);
                        string clientName = rdr[1].ToString();
                        string clientPhone = rdr[2].ToString();
                        clients.Add((clientId, clientName, clientPhone));
                        rowCount++;
                    }

                    // Если есть новая запись, перемещаем её в начало
                    if (_lastInsertedClientId.HasValue)
                    {
                        var newClient = clients.FirstOrDefault(c => c.Id == _lastInsertedClientId.Value);
                        if (newClient.Id != 0)
                        {
                            clients.Remove(newClient);
                            clients.Insert(0, newClient);
                        }
                        // Сбрасываем ID после использования
                        _lastInsertedClientId = null;
                    }

                    // Добавляем в DataGridView
                    foreach (var client in clients)
                    {
                        dataGridView1.Rows.Add(client.Id, client.Name, client.Phone);
                    }

                    label5.Text = rowCount.ToString();

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

                string newText = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(tb.Text.ToLower());

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

            string digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());
            return digitsOnly.Length < 10;
        }

        private string FormatFIO(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return fullName;

            string[] parts = fullName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
                return fullName;

            string lastName = parts[0];
            string initials = "";

            if (parts.Length > 1)
            {
                string firstName = parts[1];
                if (firstName.Contains('-'))
                {
                    string[] firstNames = firstName.Split('-');
                    initials = string.Join("", firstNames.Select(n => n[0] + "."));
                }
                else
                {
                    initials = firstName[0] + ".";
                }
            }

            if (parts.Length > 2)
            {
                string patronymic = parts[2];
                if (patronymic.Contains('-'))
                {
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

            string digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());

            if (digitsOnly.Length == 11 && (digitsOnly[0] == '7' || digitsOnly[0] == '8'))
            {
                return $"+7({digitsOnly.Substring(1, 3)}){digitsOnly.Substring(4, 3)}-{digitsOnly.Substring(7, 2)}-{digitsOnly.Substring(9, 2)}";
            }
            else if (digitsOnly.Length == 10)
            {
                return $"+7({digitsOnly.Substring(0, 3)}){digitsOnly.Substring(3, 3)}-{digitsOnly.Substring(6, 2)}-{digitsOnly.Substring(8, 2)}";
            }

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

            string formattedPhone = FormatPhoneNumber(numberPhone);

            if (IsClientExists(formattedPhone))
            {
                MessageBox.Show("Клиент с таким телефоном уже существует", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(nameUser))
            {
                MessageBox.Show("Заполните поле ФИО", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string query = "INSERT INTO Clients (Name, NumberPhone) VALUES (@name, @numberPhone); SELECT LAST_INSERT_ID();";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        string formattedName = FormatFIO(nameUser);

                        cmd.Parameters.AddWithValue("@name", formattedName);
                        cmd.Parameters.AddWithValue("@numberPhone", formattedPhone);

                        // Получаем ID только что добавленного клиента
                        int newId = Convert.ToInt32(cmd.ExecuteScalar());

                        // Сохраняем ID новой записи
                        _lastInsertedClientId = newId;

                        MessageBox.Show("Клиент успешно добавлен", "Успех",
                                      MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ClearAllFields();
                        FillDataGridView();

                        // Выделяем и показываем первую строку (нового клиента)
                        if (dataGridView1.Rows.Count > 0)
                        {
                            dataGridView1.Rows[0].Selected = true;
                            dataGridView1.FirstDisplayedScrollingRowIndex = 0;
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

            string formattedPhone = FormatPhoneNumber(newClientNumber);

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
                        string formattedName = FormatFIO(newClientName);

                        cmd.Parameters.AddWithValue("@clientName", formattedName);
                        cmd.Parameters.AddWithValue("@clientNumber", formattedPhone);
                        cmd.Parameters.AddWithValue("@selectedId", selectedId);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            // Сбрасываем ID последнего добавленного клиента при редактировании
                            _lastInsertedClientId = null;

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

            DialogResult result = MessageBox.Show(
                $"Вы уверены, что хотите удалить клиента с телефоном \"{clientNumber}\"?",
                "Подтверждение удаления",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            if (IsClientInUse(selectedId))
            {
                MessageBox.Show("Невозможно удалить клиента, так как он используется в других таблицах",
                              "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

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
                            // Сбрасываем ID последнего добавленного клиента при удалении
                            _lastInsertedClientId = null;

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
            if (!isSearching)
                UpdateButtonsState();
        }

        private void maskedTextBox1_TextChanged(object sender, EventArgs e)
        {
            if (!isSearching)
                UpdateButtonsState();
        }

        void UpdateButtonsState()
        {
            string currentTextName = textBox1.Text.Trim();
            string currentTextNumber = maskedTextBox1.Text.Trim();

            bool hasValidName = !string.IsNullOrWhiteSpace(currentTextName);
            bool hasValidNumber = !IsEmptyPhoneNumber(currentTextNumber);
            bool hasSelection = dataGridView1.SelectedRows.Count > 0;

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
                    dataChanged = true;
                }

                button2.Enabled = dataChanged;
            }
            else
            {
                button2.Enabled = false;
            }

            button3.Enabled = hasSelection;
            button4.Enabled = hasSelection;
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
            if (isSearching)
                return;

            if (dataGridView1.SelectedRows.Count > 0)
            {
                DataGridViewRow selectedRow = dataGridView1.SelectedRows[0];
                if (selectedRow != null && selectedRow.Index >= 0)
                {
                    textBox1.Text = selectedRow.Cells["Name"].Value?.ToString() ?? "";
                    maskedTextBox1.Text = selectedRow.Cells["NumberPhone"].Value?.ToString() ?? "";

                    originalName = textBox1.Text;
                    originalPhone = maskedTextBox1.Text;
                }

                button2.Enabled = true;
                button3.Enabled = true;
            }
            else
            {
                textBox1.Text = "";
                maskedTextBox1.Text = "";
                originalName = "";
                originalPhone = "";
            }

            UpdateButtonsState();
        }

        private void CreatingAClient_Load(object sender, EventArgs e)
        {
            ClearAllFields();
            _lastInsertedClientId = null;
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
                                dataGridView1.Rows.Add(
                                    rdr[0].ToString(),
                                    rdr[1].ToString(),
                                    rdr[2].ToString()
                                );
                                rowCount++;
                            }

                            label5.Text = rowCount.ToString();
                            dataGridView1.ClearSelection();
                            dataGridView1.CurrentCell = null;
                            originalName = "";
                            originalPhone = "";

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
                    FillDataGridView();
                }
            }
        }

        private void ClearDataFieldsWithoutEvents()
        {
            textBox1.TextChanged -= textBox1_TextChanged;
            maskedTextBox1.TextChanged -= maskedTextBox1_TextChanged;

            textBox1.Text = "";
            maskedTextBox1.Text = "";
            originalName = "";
            originalPhone = "";

            textBox1.TextChanged += textBox1_TextChanged;
            maskedTextBox1.TextChanged += maskedTextBox1_TextChanged;
        }

        private void ClearAllFields()
        {
            ClearDataFieldsWithoutEvents();
            dataGridView1.ClearSelection();
            dataGridView1.CurrentCell = null;
            UpdateButtonsState();
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                ClearAllFields();
            }
        }
    }
}