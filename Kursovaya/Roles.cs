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
    public partial class Roles : Form
    {
        string conString = $"host={Properties.Settings.Default.host};uid={Properties.Settings.Default.uid};pwd={Properties.Settings.Default.pwd};database={Properties.Settings.Default.database};";
        private Timer inactivityTimer;
        private int inactivityTimeout;
        private int rowCount = 0;

        public Roles()
        {
            InitializeComponent();

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
            dataGridView1.BackgroundColor = System.Drawing.Color.FromArgb(255, 221, 153);
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dataGridView1.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            textBox1.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            string fullname = Properties.Settings.Default.userName;
            string formattedname = fullname;

            string[] parts = fullname.Split(' ');

            label5.Text = rowCount.ToString();

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

        private void Roles_FormClosing(object sender, FormClosingEventArgs e)
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

        private void button4_Click(object sender, EventArgs e)
        {
            allowClose = true;
            this.Visible = false;
            Directories directories = new Directories();
            directories.ShowDialog();
            this.Close();
        }

        void FillDataGridView()
        {
            string SelectQuery = @"SELECT IDrole, `Role` FROM CafeActivities.Roles;";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                con.Open();

                using (MySqlCommand cmd = new MySqlCommand(SelectQuery, con))
                using (MySqlDataReader rdr = cmd.ExecuteReader())
                {
                    dataGridView1.Rows.Clear();
                    dataGridView1.Columns.Clear();

                    dataGridView1.Columns.Add("IDrole", "Id");
                    dataGridView1.Columns["IDrole"].Visible = false;
                    dataGridView1.Columns.Add("Role", "Роль");

                    rowCount = 0;

                    while (rdr.Read())
                    {
                        int rowIndex = dataGridView1.Rows.Add(
                            rdr[0].ToString(),
                            rdr[1].ToString()
                        );

                        rowCount++;
                    }

                    label5.Text = rowCount.ToString();

                    // Показываем информацию о загруженных данных
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

        private bool IsRolesExists(string roleName)
        {
            string query = "SELECT COUNT(*) FROM Roles WHERE Role = @role;";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@role", roleName.Trim());

                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка проверки роли: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return true; 
                }
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            string roleName = textBox1.Text.Trim();

            // Проверка на существование
            if (IsRolesExists(roleName))
            {
                MessageBox.Show("Роль с таким наименованием уже существует", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Валидация данных
            if (string.IsNullOrEmpty(roleName))
            {
                MessageBox.Show("Заполните поле роли", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Добавление в базу данных
            string query = "INSERT INTO Roles (Role) VALUES (@role)";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@role", roleName);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Роль успешно добавлена", "Успех",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);
                            textBox1.Clear();
                            FillDataGridView();
                            ClearAllFields();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка добавления роли: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null)
            {
                MessageBox.Show("Выберите роль для редактирования", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int selectedId = Convert.ToInt32(dataGridView1.CurrentRow.Cells["IDrole"].Value);
            string newRoleName = textBox1.Text.Trim();

            // Проверка на существование (исключая текущий статус)
            if (IsRolesExists(newRoleName))
            {
                MessageBox.Show("Роль с таким наименованием уже существует", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Обновление в базе данных
            string query = "UPDATE Roles SET Role = @role WHERE IDrole = @selectedId";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@role", newRoleName);
                        cmd.Parameters.AddWithValue("@selectedId", selectedId);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Роль успешно обновлена", "Успех",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);
                            textBox1.Clear();
                            FillDataGridView();
                            ClearAllFields();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка обновления роли: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            UpdateButtonsState();
        }

        void UpdateButtonsState()
        {
            // Включаем кнопку только если TextBox не пустой
            button1.Enabled = !string.IsNullOrWhiteSpace(textBox1.Text);
            string currentText = textBox1.Text.Trim();
            bool hasText = !string.IsNullOrWhiteSpace(currentText);

            if (dataGridView1.CurrentRow != null && hasText)
            {
                string originalStatus = dataGridView1.CurrentRow.Cells["Role"].Value?.ToString() ?? "";
                button2.Enabled = (currentText != originalStatus);
            }
            else
            {
                button2.Enabled = false;
            }

            // Кнопка удаления доступна только когда выбрана запись
            button3.Enabled = (dataGridView1.CurrentRow != null);
        }

        private bool IsRoleInUse(int statusId)
        {
            string checkQueries = @"SELECT COUNT(*) FROM Users WHERE IdRole = @roleId;";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();

                    using (MySqlCommand cmd = new MySqlCommand(checkQueries, con))
                    {
                        cmd.Parameters.AddWithValue("@roleId", statusId);
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
                    MessageBox.Show($"Ошибка проверки использования роли: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return true;
                }
            }
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow != null && dataGridView1.CurrentRow.Index >= 0)
            {
                try
                {
                    // Заполняем поля данными из выбранной строки
                    DataGridViewRow selectedRow = dataGridView1.CurrentRow;

                    // Основные данные
                    textBox1.Text = selectedRow.Cells["Role"].Value?.ToString() ?? "";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при заполнении полей: {ex.Message}");
                }

                // Обновляем состояние кнопок
                UpdateButtonsState();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null)
            {
                MessageBox.Show("Выберите роль для удаления", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int selectedId = Convert.ToInt32(dataGridView1.CurrentRow.Cells["IDrole"].Value);
            string statusName = dataGridView1.CurrentRow.Cells["Role"].Value.ToString();

            // Подтверждение удаления
            DialogResult result = MessageBox.Show(
                $"Вы уверены, что хотите удалить роль \"{statusName}\"?",
                "Подтверждение удаления",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            // Проверка на использование статуса в других таблицах (опционально)
            if (IsRoleInUse(selectedId))
            {
                MessageBox.Show("Невозможно удалить роль, так как она используется в других таблицах",
                              "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Удаление из базы данных
            string query = "DELETE FROM Roles WHERE IDrole = @roleId";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@roleId", selectedId);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Роль успешно удалена", "Успех",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);
                            textBox1.Clear();
                            FillDataGridView();
                            ClearAllFields();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления роли: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ClearAllFields()
        {
            dataGridView1.ClearSelection();
            dataGridView1.CurrentCell = null;
            textBox1.Text = "";
            UpdateButtonsState();
        }

        private void Roles_Load(object sender, EventArgs e)
        {
            // Очищаем все поля при загрузке формы
            ClearAllFields();
        }
    }
}
