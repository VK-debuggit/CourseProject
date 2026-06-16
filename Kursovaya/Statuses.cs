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
    public partial class Statuses : Form
    {
        string conString = $"host={Properties.Settings.Default.host};uid={Properties.Settings.Default.uid};pwd={Properties.Settings.Default.pwd};database={Properties.Settings.Default.database};";
        private int rowCount = 0;
        private int? _lastInsertedStatusId = null;

        public Statuses()
        {
            InitializeComponent();

            FillDataGridView();

            button1.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button2.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button3.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button4.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            textBox1.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
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

        //Обработчик кнопки возврата в справочники
        private void button4_Click(object sender, EventArgs e)
        {
            allowClose = true;
            this.Visible = false;
            Directories directories = new Directories();
            directories.ShowDialog();
            this.Close();
        }

        //Обработчик закрытия формы
        private void Statuses_FormClosing(object sender, FormClosingEventArgs e)
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

        //Загрузка данных статусов в DataGridView
        void FillDataGridView()
        {
            string SelectQuery = @"SELECT IDstatus, Status FROM CafeActivities.Status ORDER BY Status ASC;";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                con.Open();

                using (MySqlCommand cmd = new MySqlCommand(SelectQuery, con))
                using (MySqlDataReader rdr = cmd.ExecuteReader())
                {
                    dataGridView1.Rows.Clear();
                    dataGridView1.Columns.Clear();

                    dataGridView1.Columns.Add("IDstatus", "Id");
                    dataGridView1.Columns["IDstatus"].Visible = false;
                    dataGridView1.Columns.Add("Status", "Статус");

                    var statuses = new List<(int Id, string Name)>();
                    rowCount = 0;

                    while (rdr.Read())
                    {
                        int statusId = Convert.ToInt32(rdr[0]);
                        string statusName = rdr[1].ToString();
                        statuses.Add((statusId, statusName));
                        rowCount++;
                    }

                    if (_lastInsertedStatusId.HasValue)
                    {
                        var newStatus = statuses.FirstOrDefault(s => s.Id == _lastInsertedStatusId.Value);
                        if (newStatus.Id != 0)
                        {
                            statuses.Remove(newStatus);
                            statuses.Insert(0, newStatus);
                        }
                    }

                    foreach (var status in statuses)
                    {
                        dataGridView1.Rows.Add(status.Id, status.Name);
                    }

                    label5.Text = rowCount.ToString();

                    if (rowCount == 0)
                    {
                        MessageBox.Show("Данные не найдены", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    _lastInsertedStatusId = null;
                    ClearAllFields();
                }
            }
        }

        //Ограничение ввода в текстовое поле
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
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

                return;
            }

            if ((e.KeyChar >= 'А' && e.KeyChar <= 'Я') ||
                (e.KeyChar >= 'а' && e.KeyChar <= 'я') ||
                e.KeyChar == 'Ё' || e.KeyChar == 'ё')
                return;

            e.Handled = true;
        }

        //Проверка существования статуса в базе данных
        private bool IsStatusExists(string statusName)
        {
            string query = "SELECT COUNT(*) FROM Status WHERE Status = @status;";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@status", statusName.Trim());

                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка проверки статуса: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return true;
                }
            }
        }

        //Проверка существования статуса исключая текущую запись
        private bool IsStatusExistsExceptCurrent(int statusId, string statusName)
        {
            string query = "SELECT COUNT(*) FROM Status WHERE Status = @status AND IDstatus != @statusId;";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@status", statusName.Trim());
                        cmd.Parameters.AddWithValue("@statusId", statusId);

                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка проверки статуса: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return true;
                }
            }
        }

        //Обработчик кнопки добавления статуса
        private void button1_Click(object sender, EventArgs e)
        {
            string statusName = textBox1.Text.Trim();

            if (IsStatusExists(statusName))
            {
                MessageBox.Show("Статус заказа с таким наименованием уже существует", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(statusName))
            {
                MessageBox.Show("Заполните поле статуса заказа", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string query = "INSERT INTO Status (Status) VALUES (@status); SELECT LAST_INSERT_ID();";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@status", statusName);
                        int newId = Convert.ToInt32(cmd.ExecuteScalar());

                        _lastInsertedStatusId = newId;

                        MessageBox.Show("Статус заказа успешно добавлен", "Успех",
                                      MessageBoxButtons.OK, MessageBoxIcon.Information);
                        textBox1.Clear();
                        FillDataGridView();

                        if (dataGridView1.Rows.Count > 0)
                        {
                            dataGridView1.Rows[0].Selected = true;
                            dataGridView1.FirstDisplayedScrollingRowIndex = 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка добавления статуса заказа: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        //Обработчик кнопки обновления статуса
        private void button2_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null)
            {
                MessageBox.Show("Выберите статус заказа для редактирования", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int selectedId = Convert.ToInt32(dataGridView1.CurrentRow.Cells["IDstatus"].Value);
            string newStatusName = textBox1.Text.Trim();

            if (IsStatusExistsExceptCurrent(selectedId, newStatusName))
            {
                MessageBox.Show("Статус заказа с таким наименованием уже существует", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string query = "UPDATE Status SET Status = @Status WHERE IDstatus = @selectedId";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Status", newStatusName);
                        cmd.Parameters.AddWithValue("@selectedId", selectedId);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            _lastInsertedStatusId = selectedId;

                            MessageBox.Show("Статус заказа успешно обновлен", "Успех",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);
                            textBox1.Clear();
                            FillDataGridView();

                            if (dataGridView1.Rows.Count > 0)
                            {
                                dataGridView1.Rows[0].Selected = true;
                                dataGridView1.FirstDisplayedScrollingRowIndex = 0;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка обновления статуса заказа: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        //Обновление состояния кнопок
        void UpdateButtonsState()
        {
            button1.Enabled = !string.IsNullOrWhiteSpace(textBox1.Text);
            string currentText = textBox1.Text.Trim();
            bool hasText = !string.IsNullOrWhiteSpace(currentText);

            if (dataGridView1.CurrentRow != null && hasText)
            {
                string originalStatus = dataGridView1.CurrentRow.Cells["Status"].Value?.ToString() ?? "";
                button2.Enabled = (currentText != originalStatus);
            }
            else
            {
                button2.Enabled = false;
            }

            button3.Enabled = (dataGridView1.CurrentRow != null);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            UpdateButtonsState();
        }

        //Обработчик изменения выделения в DataGridView
        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow != null && dataGridView1.CurrentRow.Index >= 0)
            {
                try
                {
                    DataGridViewRow selectedRow = dataGridView1.CurrentRow;
                    textBox1.Text = selectedRow.Cells["Status"].Value?.ToString() ?? "";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при заполнении полей: {ex.Message}");
                }

                UpdateButtonsState();
            }
        }

        //Обработчик кнопки удаления статуса
        private void button3_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null)
            {
                MessageBox.Show("Выберите статус заказа для удаления", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int selectedId = Convert.ToInt32(dataGridView1.CurrentRow.Cells["IDstatus"].Value);
            string statusName = dataGridView1.CurrentRow.Cells["Status"].Value.ToString();

            DialogResult result = MessageBox.Show(
                $"Вы уверены, что хотите удалить статус \"{statusName}\"?",
                "Подтверждение удаления",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            if (IsStatusInUse(selectedId))
            {
                MessageBox.Show("Невозможно удалить статус заказа, так как он используется в других таблицах",
                              "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string query = "DELETE FROM Status WHERE IDstatus = @statusId";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@statusId", selectedId);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Статус заказа успешно удален", "Успех",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);
                            textBox1.Clear();
                            FillDataGridView();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления статуса заказа: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        //Проверка использования статуса в других таблицах
        private bool IsStatusInUse(int statusId)
        {
            string checkQueries = @"SELECT COUNT(*) FROM Orders WHERE IdStatus = @statusId;";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();

                    using (MySqlCommand cmd = new MySqlCommand(checkQueries, con))
                    {
                        cmd.Parameters.AddWithValue("@statusId", statusId);
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
                    MessageBox.Show($"Ошибка проверки использования статуса заказа: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return true;
                }
            }
        }

        //Очистка всех полей формы
        private void ClearAllFields()
        {
            dataGridView1.ClearSelection();
            dataGridView1.CurrentCell = null;
            textBox1.Text = "";
            UpdateButtonsState();
        }

        //Обработчик загрузки формы
        private void Statuses_Load(object sender, EventArgs e)
        {
            ClearAllFields();
            _lastInsertedStatusId = null;
        }

        //Установка русской раскладки при установке курсора в текстовое поле
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