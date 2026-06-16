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
    public partial class Users : Form
    {
        string conString = $"host={Properties.Settings.Default.host};uid={Properties.Settings.Default.uid};pwd={Properties.Settings.Default.pwd};database={Properties.Settings.Default.database};";
        private int rowCount = 0;
        private int? _lastInsertedUserId = null;

        public Users()
        {
            InitializeComponent();

            FillDataGridView();
            FillFilter();

            button1.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button2.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button3.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button4.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            dataGridView1.BackgroundColor = System.Drawing.Color.FromArgb(255, 221, 153);
            textBox1.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            textBox2.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            textBox3.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            Filter.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            dataGridView1.BackgroundColor = System.Drawing.Color.FromArgb(255, 221, 153);
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dataGridView1.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            }
            dataGridView1.Columns[dataGridView1.Columns.Count - 1].AutoSizeMode =
                DataGridViewAutoSizeColumnMode.Fill;

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

            UpdateButtonsState();
        }

        private bool allowClose = false;

        //Обработчик кнопки возврата в главное меню
        private void button4_Click(object sender, EventArgs e)
        {
            allowClose = true;
            this.Visible = false;
            MainFormAdmin mainFormAdmin = new MainFormAdmin();
            mainFormAdmin.ShowDialog();
            this.Close();
        }

        //Обработчик закрытия формы
        private void Users_FormClosing(object sender, FormClosingEventArgs e)
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

        //Загрузка данных пользователей в DataGridView
        void FillDataGridView()
        {
            string SelectQuery = @"SELECT 
                            p.IDuser, 
                            p.FullName, 
                            p.Login, 
                            p.Password, 
                            c.`Role` as `Role`
                        FROM CafeActivities.Users p 
                        LEFT JOIN Roles c ON p.IDrole = c.IDrole
                        ORDER BY p.FullName ASC;";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                con.Open();

                using (MySqlCommand cmd = new MySqlCommand(SelectQuery, con))
                using (MySqlDataReader rdr = cmd.ExecuteReader())
                {
                    dataGridView1.Rows.Clear();
                    dataGridView1.Columns.Clear();

                    dataGridView1.Columns.Add("IDuser", "Id");
                    dataGridView1.Columns["IDuser"].Visible = false;
                    dataGridView1.Columns.Add("FullName", "ФИО");
                    dataGridView1.Columns.Add("Login", "Логин");
                    dataGridView1.Columns.Add("Password", "Пароль");
                    dataGridView1.Columns["Password"].Visible = false;
                    dataGridView1.Columns.Add("Role", "Роль");

                    var users = new List<(int Id, string FullName, string Login, string Password, string Role)>();
                    rowCount = 0;

                    while (rdr.Read())
                    {
                        int userId = Convert.ToInt32(rdr[0]);
                        string fullName = rdr[1].ToString();
                        string login = rdr[2].ToString();
                        string password = rdr[3].ToString();
                        string role = rdr[4].ToString();
                        users.Add((userId, fullName, login, password, role));
                        rowCount++;
                    }

                    if (_lastInsertedUserId.HasValue)
                    {
                        var lastUser = users.FirstOrDefault(u => u.Id == _lastInsertedUserId.Value);
                        if (lastUser.Id != 0)
                        {
                            users.Remove(lastUser);
                            users.Insert(0, lastUser);
                        }
                    }

                    foreach (var user in users)
                    {
                        dataGridView1.Rows.Add(user.Id, user.FullName, user.Login, user.Password, user.Role);
                    }

                    label8.Text = rowCount.ToString();

                    if (rowCount == 0)
                    {
                        MessageBox.Show("Данные не найдены", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    _lastInsertedUserId = null;
                }
            }
        }

        //Ограничение ввода в поле ФИО (только русские буквы)
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox tb = (TextBox)sender;

            if (char.IsControl(e.KeyChar))
                return;

            if (e.KeyChar == ' ' && tb.Text.Length == 0)
            {
                e.Handled = true;
                return;
            }

            if (e.KeyChar == '-' && tb.Text.Length == 0)
            {
                e.Handled = true;
                return;
            }

            if (e.KeyChar == ' ' && tb.Text.Length > 0 && tb.Text[tb.Text.Length - 1] == ' ')
            {
                e.Handled = true;
                return;
            }

            if (e.KeyChar == '-' && tb.Text.Length > 0 && tb.Text[tb.Text.Length - 1] == '-')
            {
                e.Handled = true;
                return;
            }

            if (tb.Text.Length > 0)
            {
                char lastChar = tb.Text[tb.Text.Length - 1];
                if ((lastChar == ' ' && e.KeyChar == '-') || (lastChar == '-' && e.KeyChar == ' '))
                {
                    e.Handled = true;
                    return;
                }
            }

            if (e.KeyChar == ' ' || e.KeyChar == '-')
            {
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

        //Загрузка ролей в выпадающий список
        void FillFilter()
        {
            MySqlConnection con = new MySqlConnection(conString);
            con.Open();

            MySqlCommand cmd = new MySqlCommand(@"SELECT * FROM CafeActivities.Roles;", con);
            MySqlDataReader rdr = cmd.ExecuteReader();

            Filter.Items.Clear();

            Filter.Items.Add("Все роли");

            while (rdr.Read())
            {
                Filter.Items.Add(rdr[1].ToString());
            }

            Filter.SelectedIndex = 0;

            con.Close();
        }

        //Ограничение ввода в поле логина
        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox tb = (TextBox)sender;

            if (char.IsControl(e.KeyChar))
                return;

            if (tb.Text.Length >= 32 && !char.IsControl(e.KeyChar))
            {
                e.Handled = true;
                return;
            }

            if ((e.KeyChar >= 'a' && e.KeyChar <= 'z') || (e.KeyChar >= 'A' && e.KeyChar <= 'Z'))
            {
                e.Handled = false;
                return;
            }

            if (char.IsDigit(e.KeyChar))
            {
                e.Handled = false;
                return;
            }

            char[] allowedSpecialChars = { '!', '@', '#', '$', '%', '^', '&', '*', '(', ')',
                                  '-', '_', '=', '+', '[', ']', '{', '}', ';', ':',
                                  ',', '.', '<', '>', '/', '?', '|', '\\', '~', '`' };

            if (allowedSpecialChars.Contains(e.KeyChar))
            {
                e.Handled = false;
                return;
            }

            e.Handled = true;
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

        //Обработчик кнопки добавления пользователя
        private void button1_Click(object sender, EventArgs e)
        {
            string userName = textBox1.Text.Trim();
            string loginName = textBox2.Text.Trim();
            string password = textBox3.Text.Trim();

            bool hasError = false;
            string errorMessage = "";

            if (string.IsNullOrEmpty(userName))
            {
                errorMessage += "• Заполните поле ФИО\n";
                hasError = true;
            }

            if (string.IsNullOrEmpty(loginName))
            {
                errorMessage += "• Заполните поле логина\n";
                hasError = true;
            }
            else if (loginName.Length < 4)
            {
                errorMessage += "• Логин должен содержать минимум 4 символа\n";
                hasError = true;
            }

            if (string.IsNullOrEmpty(password))
            {
                errorMessage += "• Заполните поле пароля\n";
                hasError = true;
            }
            else if (password.Length < 8)
            {
                errorMessage += "• Пароль должен содержать минимум 8 символов\n";
                hasError = true;
            }

            if (Filter.SelectedIndex <= 0)
            {
                errorMessage += "• Выберите конкретную роль пользователя\n";
                hasError = true;
            }

            if (hasError)
            {
                MessageBox.Show(errorMessage, "Ошибка",
                               MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (IsUserExists(loginName))
            {
                MessageBox.Show("Пользователь с таким логином уже существует", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string hashPassword = GetHashPass(password);
            string roleName = Filter.SelectedItem.ToString();
            int roleId = GetRoleIdByName(roleName);

            if (roleId <= 0)
            {
                MessageBox.Show("Ошибка получения ID роли", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string query = @"INSERT INTO Users (FullName, Login, Password, IDRole) 
                             VALUES (@fullName, @login, @password, @idrole);
                             SELECT LAST_INSERT_ID();";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@fullName", userName);
                        cmd.Parameters.AddWithValue("@login", loginName);
                        cmd.Parameters.AddWithValue("@password", hashPassword);
                        cmd.Parameters.AddWithValue("@idrole", roleId);

                        int newId = Convert.ToInt32(cmd.ExecuteScalar());

                        _lastInsertedUserId = newId;

                        MessageBox.Show("Пользователь успешно добавлен", "Успех",
                                      MessageBoxButtons.OK, MessageBoxIcon.Information);

                        dataGridView1.SelectionChanged -= dataGridView1_SelectionChanged;

                        FillDataGridView();

                        dataGridView1.ClearSelection();

                        textBox1.Clear();
                        textBox2.Clear();
                        textBox3.Clear();

                        if (Filter.Items.Count > 0)
                        {
                            Filter.SelectedIndex = 0;
                        }

                        dataGridView1.SelectionChanged += dataGridView1_SelectionChanged;
                        UpdateButtonsState();
                        ClearAllFields();
                    }
                }
                catch (Exception ex)
                {
                    dataGridView1.SelectionChanged += dataGridView1_SelectionChanged;
                    MessageBox.Show($"Ошибка добавления пользователя: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        //Проверка существования пользователя
        private bool IsUserExists(string loginName)
        {
            string query = "SELECT COUNT(*) FROM Users WHERE Login = @login;";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@login", loginName.Trim());

                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка проверки пользователя: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return true;
                }
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

                    textBox1.Text = selectedRow.Cells["FullName"].Value?.ToString() ?? "";
                    textBox2.Text = selectedRow.Cells["Login"].Value?.ToString() ?? "";

                    string roleName = selectedRow.Cells["Role"].Value?.ToString() ?? "";

                    if (!string.IsNullOrEmpty(roleName))
                    {
                        int roleIndex = Filter.FindStringExact(roleName);
                        if (roleIndex >= 0)
                            Filter.SelectedIndex = roleIndex;
                        else
                            Filter.SelectedIndex = 0;
                    }
                    else
                    {
                        Filter.SelectedIndex = 0;
                    }

                    textBox3.Clear();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при заполнении полей: {ex.Message}");
                }
            }

            UpdateButtonsState();
        }

        //Обновление состояния кнопок
        void UpdateButtonsState()
        {
            string FIOText = textBox1.Text.Trim();
            string loginText = textBox2.Text.Trim();
            string passwordText = textBox3.Text.Trim();

            bool isFIOValid = !string.IsNullOrWhiteSpace(FIOText);
            bool isLoginValid = !string.IsNullOrWhiteSpace(loginText) &&
                                loginText.Length >= 4 &&
                                loginText.Length <= 32;
            bool isPasswordValidForAdd = !string.IsNullOrWhiteSpace(passwordText) &&
                                          passwordText.Length >= 8 &&
                                          passwordText.Length <= 64;

            bool allTextFieldsFilled = (isFIOValid && isLoginValid && isPasswordValidForAdd);
            bool isRowSelected = (dataGridView1.CurrentRow != null && dataGridView1.CurrentRow.Index >= 0);

            button1.Enabled = allTextFieldsFilled && Filter.SelectedIndex > 0;

            if (isRowSelected && Filter.SelectedIndex > 0)
            {
                string originalLogin = dataGridView1.CurrentRow.Cells["Login"].Value?.ToString() ?? "";
                string originalFIO = dataGridView1.CurrentRow.Cells["FullName"].Value?.ToString() ?? "";
                string originalRole = dataGridView1.CurrentRow.Cells["Role"].Value?.ToString() ?? "";
                string originalPasswordHash = dataGridView1.CurrentRow.Cells["Password"].Value?.ToString() ?? "";

                string selectedRole = Filter.SelectedItem?.ToString() ?? "";

                bool fioChanged = FIOText != originalFIO && !string.IsNullOrEmpty(FIOText);
                bool loginChanged = loginText != originalLogin &&
                                   !string.IsNullOrEmpty(loginText) &&
                                   loginText.Length >= 4 &&
                                   loginText.Length <= 32;
                bool roleChanged = selectedRole != originalRole && Filter.SelectedIndex > 0;

                bool passwordChanged = false;
                if (!string.IsNullOrEmpty(passwordText) &&
                    passwordText.Length >= 8 &&
                    passwordText.Length <= 64)
                {
                    string hashedPassword = GetHashPass(passwordText);
                    passwordChanged = hashedPassword != originalPasswordHash;
                }

                button2.Enabled = fioChanged || loginChanged || roleChanged || passwordChanged;
            }
            else
            {
                button2.Enabled = false;
            }

            button3.Enabled = isRowSelected;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            textBox1.TextChanged -= textBox1_TextChanged;

            string text = textBox1.Text;
            if (!string.IsNullOrEmpty(text))
            {
                string[] words = text.Split(new char[] { ' ', '-' }, StringSplitOptions.None);
                string result = "";

                for (int i = 0; i < words.Length; i++)
                {
                    if (!string.IsNullOrEmpty(words[i]))
                    {
                        char firstChar = char.ToUpper(words[i][0]);
                        string rest = words[i].Length > 1 ? words[i].Substring(1).ToLower() : "";
                        result += firstChar + rest;
                    }

                    if (i < words.Length - 1)
                    {
                        int separatorIndex = text.IndexOf(words[i], StringComparison.Ordinal) + words[i].Length;
                        if (separatorIndex < text.Length && separatorIndex >= 0)
                        {
                            char separator = text[separatorIndex];
                            if (separator == ' ' || separator == '-')
                            {
                                result += separator;
                            }
                        }
                    }
                }

                if (result != text)
                {
                    textBox1.Text = result;
                    textBox1.SelectionStart = textBox1.Text.Length;
                }
            }

            textBox1.TextChanged += textBox1_TextChanged;

            UpdateButtonsState();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (textBox2.Text.Length > 32)
            {
                textBox2.TextChanged -= textBox2_TextChanged;
                textBox2.Text = textBox2.Text.Substring(0, 32);
                textBox2.SelectionStart = textBox2.Text.Length;
                textBox2.TextChanged += textBox2_TextChanged;
            }

            UpdateButtonsState();
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            if (textBox3.Text.Length > 64)
            {
                textBox3.TextChanged -= textBox3_TextChanged;
                textBox3.Text = textBox3.Text.Substring(0, 64);
                textBox3.SelectionStart = textBox3.Text.Length;
                textBox3.TextChanged += textBox3_TextChanged;
            }

            UpdateButtonsState();
        }

        private void Filter_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateButtonsState();
        }

        //Проверка существования другого пользователя
        private bool IsAnotherUserExists(string login, int currentUserId)
        {
            string query = @"SELECT COUNT(*) FROM Users 
                    WHERE Login = @login 
                    AND IDuser != @currentUserId";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@login", login.Trim());
                        cmd.Parameters.AddWithValue("@currentUserId", currentUserId);

                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка проверки пользователя: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return true;
                }
            }
        }

        //Получение ID роли по имени
        private int GetRoleIdByName(string roleName)
        {
            if (string.IsNullOrEmpty(roleName))
            {
                return 0;
            }

            string query = "SELECT IDrole FROM Roles WHERE Role = @roleName";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@roleName", roleName);
                        object result = cmd.ExecuteScalar();
                        return result != null ? Convert.ToInt32(result) : 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка получения ID роли: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return 0;
                }
            }
        }

        //Обработчик кнопки обновления пользователя
        private void button2_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null)
            {
                MessageBox.Show("Выберите пользователя для редактирования", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int selectedId = Convert.ToInt32(dataGridView1.CurrentRow.Cells["IDuser"].Value);
            string oldLogin = dataGridView1.CurrentRow.Cells["Login"].Value?.ToString() ?? "";
            string oldPasswordHash = dataGridView1.CurrentRow.Cells["Password"].Value?.ToString() ?? "";
            string oldFullName = dataGridView1.CurrentRow.Cells["FullName"].Value?.ToString() ?? "";
            string oldRole = dataGridView1.CurrentRow.Cells["Role"].Value?.ToString() ?? "";

            string FIO = textBox1.Text.Trim();
            string login = textBox2.Text.Trim();
            string password = textBox3.Text.Trim();
            string selectedRole = Filter.SelectedItem?.ToString() ?? "";

            bool hasError = false;
            string errorMessage = "";

            if (string.IsNullOrEmpty(FIO))
            {
                errorMessage += "• Введите ФИО пользователя\n";
                hasError = true;
            }

            if (string.IsNullOrEmpty(login))
            {
                errorMessage += "• Введите логин пользователя\n";
                hasError = true;
            }
            else if (login.Length < 4)
            {
                errorMessage += "• Логин должен содержать минимум 4 символа\n";
                hasError = true;
            }

            if (!string.IsNullOrEmpty(password) && password.Length < 8)
            {
                errorMessage += "• Пароль должен содержать минимум 8 символов\n";
                hasError = true;
            }

            if (Filter.SelectedIndex <= 0)
            {
                errorMessage += "• Выберите конкретную роль пользователя\n";
                hasError = true;
            }

            if (hasError)
            {
                MessageBox.Show(errorMessage, "Ошибка",
                               MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (login != oldLogin && IsAnotherUserExists(login, selectedId))
            {
                MessageBox.Show("Пользователь с таким логином уже существует", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int roleId = GetRoleIdByName(selectedRole);
            if (roleId <= 0)
            {
                MessageBox.Show("Ошибка получения ID роли", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string query = "UPDATE Users SET ";
            List<string> setClauses = new List<string>();
            List<MySqlParameter> parameters = new List<MySqlParameter>();

            if (FIO != oldFullName)
            {
                setClauses.Add("FullName = @fullName");
                parameters.Add(new MySqlParameter("@fullName", FIO));
            }

            if (login != oldLogin)
            {
                setClauses.Add("Login = @login");
                parameters.Add(new MySqlParameter("@login", login));
            }

            if (!string.IsNullOrEmpty(password))
            {
                string hashedPassword = GetHashPass(password);
                if (hashedPassword != oldPasswordHash)
                {
                    setClauses.Add("Password = @password");
                    parameters.Add(new MySqlParameter("@password", hashedPassword));
                }
            }

            if (selectedRole != oldRole)
            {
                setClauses.Add("IDrole = @idRole");
                parameters.Add(new MySqlParameter("@idRole", roleId));
            }

            if (setClauses.Count == 0)
            {
                MessageBox.Show("Нет изменений для сохранения", "Информация",
                              MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            query += string.Join(", ", setClauses);
            query += " WHERE IDuser = @selectedId";
            parameters.Add(new MySqlParameter("@selectedId", selectedId));

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddRange(parameters.ToArray());
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            _lastInsertedUserId = selectedId;

                            MessageBox.Show("Пользователь успешно обновлен", "Успех",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);

                            dataGridView1.SelectionChanged -= dataGridView1_SelectionChanged;
                            FillDataGridView();
                            dataGridView1.ClearSelection();
                            ClearAllFields();
                            dataGridView1.SelectionChanged += dataGridView1_SelectionChanged;
                            UpdateButtonsState();
                        }
                        else
                        {
                            MessageBox.Show("Пользователь не был обновлен", "Информация",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    dataGridView1.SelectionChanged += dataGridView1_SelectionChanged;
                    MessageBox.Show($"Ошибка обновления пользователя: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        //Очистка всех полей формы
        private void ClearAllFields()
        {
            dataGridView1.ClearSelection();
            dataGridView1.CurrentCell = null;
            textBox1.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
            if (Filter.Items.Count > 0)
            {
                Filter.SelectedIndex = 0;
            }
            UpdateButtonsState();
            _lastInsertedUserId = null;
        }

        //Обработчик загрузки формы
        private void Users_Load(object sender, EventArgs e)
        {
            ClearAllFields();
            _lastInsertedUserId = null;

            for (int i = 0; i < dataGridView1.Columns.Count - 1; i++)
            {
                dataGridView1.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            }

            dataGridView1.Columns[dataGridView1.Columns.Count - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        //Обработчик кнопки удаления пользователя
        private void button3_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null)
            {
                MessageBox.Show("Выберите пользователя для удаления", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int selectedId = Convert.ToInt32(dataGridView1.CurrentRow.Cells["IDuser"].Value);
            string userName = dataGridView1.CurrentRow.Cells["FullName"].Value.ToString();

            if (userName == Properties.Settings.Default.userName)
            {
                MessageBox.Show("Невозможно удалить активного пользователя",
                              "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DialogResult result = MessageBox.Show(
                $"Вы уверены, что хотите удалить пользователя \"{userName}\"?",
                "Подтверждение удаления",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            if (IsUserInUse(selectedId))
            {
                MessageBox.Show("Невозможно удалить пользователя, так как он используется в других таблицах",
                              "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string query = "DELETE FROM Users WHERE IDuser = @userId";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@userId", selectedId);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Пользователь успешно удален", "Успех",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);

                            dataGridView1.SelectionChanged -= dataGridView1_SelectionChanged;
                            FillDataGridView();
                            dataGridView1.ClearSelection();
                            textBox1.Clear();
                            textBox2.Clear();
                            textBox3.Clear();

                            if (Filter.Items.Count > 0)
                            {
                                Filter.SelectedIndex = 0;
                            }

                            dataGridView1.SelectionChanged += dataGridView1_SelectionChanged;
                            UpdateButtonsState();
                            ClearAllFields();
                        }
                    }
                }
                catch (Exception ex)
                {
                    dataGridView1.SelectionChanged += dataGridView1_SelectionChanged;
                    MessageBox.Show($"Ошибка удаления пользователя: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        //Проверка использования пользователя в других таблицах
        private bool IsUserInUse(int userId)
        {
            string checkQueries = @"SELECT COUNT(*) FROM Orders WHERE IdUser = @userId;";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();

                    using (MySqlCommand cmd = new MySqlCommand(checkQueries, con))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
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
                    MessageBox.Show($"Ошибка проверки использования пользователей: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return true;
                }
            }
        }

        //Установка русской раскладки в поле ФИО
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

        //Установка английской раскладки в поле логина
        private void textBox2_Enter(object sender, EventArgs e)
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

        //Установка английской раскладки в поле пароля
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