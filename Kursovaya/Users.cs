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

        private void button4_Click(object sender, EventArgs e)
        {
            allowClose = true;
            this.Visible = false;
            MainFormAdmin mainFormAdmin = new MainFormAdmin();
            mainFormAdmin.ShowDialog();
            this.Close();
        }

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

        void FillDataGridView()
        {
            string SelectQuery = @"SELECT 
                                    p.IDuser, 
                                    p.FullName, 
                                    p.Login, 
                                    p.Password, 
                                    c.`Role` as `Role`
                                FROM CafeActivities.Users p 
                                LEFT JOIN Roles c ON p.IDrole = c.IDrole;";

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

                    rowCount = 0;
                    while (rdr.Read())
                    {
                        int rowIndex = dataGridView1.Rows.Add(
                            rdr[0].ToString(),
                            rdr[1].ToString(),
                            rdr[2].ToString(),
                            rdr[3].ToString(),
                            rdr[4].ToString()
                        );

                        rowCount++;
                    }

                    label8.Text = rowCount.ToString();

                    if (rowCount == 0)
                    {
                        MessageBox.Show("Данные не найдены", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
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

        void FillFilter()
        {
            MySqlConnection con = new MySqlConnection(conString);
            con.Open();

            MySqlCommand cmd = new MySqlCommand(@"SELECT * FROM CafeActivities.Roles;", con);
            MySqlDataReader rdr = cmd.ExecuteReader();

            Filter.Items.Clear();

            // Добавляем "Все роли" как первый элемент
            Filter.Items.Add("Все роли");

            while (rdr.Read())
            {
                Filter.Items.Add(rdr[1].ToString());
            }

            // Устанавливаем "Все роли" по умолчанию
            Filter.SelectedIndex = 0;

            con.Close();
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

        private void textBox3_KeyPress(object sender, KeyPressEventArgs e)
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

    if (string.IsNullOrEmpty(password))
    {
        errorMessage += "• Заполните поле пароля\n";
        hasError = true;
    }

    // Проверка выбора роли - нельзя выбрать "Все роли"
    if (Filter.SelectedIndex <= 0) // 0 - это "Все роли"
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
    
    // Получаем имя выбранной роли
    string roleName = Filter.SelectedItem.ToString();
    
    // Получаем ID роли по имени
    int roleId = GetRoleIdByName(roleName);
    if (roleId <= 0)
    {
        MessageBox.Show("Ошибка получения ID роли", "Ошибка",
                      MessageBoxButtons.OK, MessageBoxIcon.Error);
        return;
    }

    string query = @"INSERT INTO Users (FullName, Login, Password, IDRole) 
                     VALUES (@fullName, @login, @password, @idrole)";

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

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Пользователь успешно добавлен", "Успех",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);

                            dataGridView1.SelectionChanged -= dataGridView1_SelectionChanged;

                            FillDataGridView();

                            dataGridView1.ClearSelection();

                            textBox1.Clear();
                            textBox2.Clear();
                            textBox3.Clear();

                            // Сбрасываем на "Все роли"
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
                    MessageBox.Show($"Ошибка добавления пользователя: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
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
                            Filter.SelectedIndex = 0; // "Все роли"
                    }
                    else
                    {
                        Filter.SelectedIndex = 0; // "Все роли"
                    }

                    textBox3.Clear();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при заполнении полей: {ex.Message}");
                }
            }

            // Обновляем состояние кнопок после изменения выбора
            UpdateButtonsState();
        }

        void UpdateButtonsState()
        {
            string FIOText = textBox1.Text.Trim();
            string loginText = textBox2.Text.Trim();
            string passwordText = textBox3.Text.Trim();

            // Проверяем, все ли обязательные поля заполнены
            bool allTextFieldsFilled = (!string.IsNullOrWhiteSpace(FIOText) &&
                                       !string.IsNullOrWhiteSpace(loginText) &&
                                       !string.IsNullOrWhiteSpace(passwordText));

            bool isRowSelected = (dataGridView1.CurrentRow != null &&
                                 dataGridView1.CurrentRow.Index >= 0);

            // Для добавления пользователя - все поля должны быть заполнены и выбрана конкретная роль (не "Все роли")
            bool canAddUser = allTextFieldsFilled &&
                             Filter.SelectedIndex > 0;// Конкретная роль выбрана

            // Кнопка "Добавить" активна
            button1.Enabled = canAddUser;

            // Для редактирования пользователя
            if (allTextFieldsFilled && Filter.SelectedIndex > 0 && isRowSelected)
            {
                string originalLogin = dataGridView1.CurrentRow.Cells["Login"].Value?.ToString() ?? "";
                string originalFIO = dataGridView1.CurrentRow.Cells["FullName"].Value?.ToString() ?? "";
                string originalRole = dataGridView1.CurrentRow.Cells["Role"].Value?.ToString() ?? "";
                string originalHashedPassword = dataGridView1.CurrentRow.Cells["Password"].Value?.ToString() ?? "";

                string hashedPassword = GetHashPass(passwordText);
                string selectedRole = Filter.SelectedItem?.ToString() ?? "";

                bool hasChanges = (loginText != originalLogin) ||
                                 (FIOText != originalFIO) ||
                                 (selectedRole != originalRole) ||
                                 (hashedPassword != originalHashedPassword);

                button2.Enabled = hasChanges;
            }
            else
            {
                button2.Enabled = false;
            }

            button3.Enabled = isRowSelected;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            UpdateButtonsState();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            UpdateButtonsState();
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            UpdateButtonsState();
        }

        private void Filter_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateButtonsState();
        }

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

        // Метод для получения ID роли по индексу в ComboBox (без учета "Все роли")
        private int GetRoleIdByIndex(int roleIndex)
        {
            if (roleIndex < 0)
                return 0;

            string query = "SELECT IDrole FROM Roles ORDER BY IDrole LIMIT 1 OFFSET @roleIndex";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@roleIndex", roleIndex);
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

        // Метод для получения ID роли по имени
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

            string FIO = textBox1.Text.Trim();
            string login = textBox2.Text.Trim();
            string password = textBox3.Text.Trim();

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

            if (string.IsNullOrEmpty(password))
            {
                errorMessage += "• Введите пароль пользователя\n";
                hasError = true;
            }

            // Проверка выбора роли - нельзя выбрать "Все роли"
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

            // Получаем ID роли по имени
            string roleName = Filter.SelectedItem.ToString();
            int roleId = GetRoleIdByName(roleName);

            if (roleId <= 0)
            {
                MessageBox.Show("Ошибка получения ID роли", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string hashedPassword = GetHashPass(password);

            string query = @"UPDATE Users 
                    SET FullName = @fullName, 
                        Login = @login, 
                        Password = @password,
                        IDrole = @idRole
                    WHERE IDuser = @selectedId";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@fullName", FIO);
                        cmd.Parameters.AddWithValue("@login", login);
                        cmd.Parameters.AddWithValue("@password", hashedPassword);
                        cmd.Parameters.AddWithValue("@idRole", roleId);
                        cmd.Parameters.AddWithValue("@selectedId", selectedId);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Пользователь успешно обновлен", "Успех",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);

                            dataGridView1.SelectionChanged -= dataGridView1_SelectionChanged;
                            FillDataGridView();
                            dataGridView1.ClearSelection();
                            textBox1.Clear();
                            textBox2.Clear();
                            textBox3.Clear();

                            // Сбрасываем на "Все роли"
                            if (Filter.Items.Count > 0)
                            {
                                Filter.SelectedIndex = 0;
                            }

                            dataGridView1.SelectionChanged += dataGridView1_SelectionChanged;
                            UpdateButtonsState();
                            ClearAllFields();
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

        private void ClearAllFields()
        {
            dataGridView1.ClearSelection();
            dataGridView1.CurrentCell = null;
            textBox1.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
            // Устанавливаем "Все роли" по умолчанию
            if (Filter.Items.Count > 0)
            {
                Filter.SelectedIndex = 0;
            }
            UpdateButtonsState();
        }

        private void Users_Load(object sender, EventArgs e)
        {
            // Очищаем все поля при загрузке формы
            ClearAllFields();
        }

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

                            // Сбрасываем на "Все роли"
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
    }
}