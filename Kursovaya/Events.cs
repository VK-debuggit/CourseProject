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
    public partial class Events : Form
    {
        string conString = $"host={Properties.Settings.Default.host};uid={Properties.Settings.Default.uid};pwd={Properties.Settings.Default.pwd};database={Properties.Settings.Default.database};";
        private int rowCount = 0;
        private int? _lastInsertedEventId = null;

        public Events()
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
        private void button3_Click(object sender, EventArgs e)
        {
            allowClose = true;
            this.Visible = false;
            Directories directories = new Directories();
            directories.ShowDialog();
            this.Close();
        }

        //Обработчик закрытия формы
        private void Events_FormClosing(object sender, FormClosingEventArgs e)
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

        //Загрузка данных мероприятий в DataGridView
        void FillDataGridView()
        {
            string SelectQuery = @"SELECT IDevent, `Event` FROM CafeActivities.Events ORDER BY `Event` ASC;";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                con.Open();

                using (MySqlCommand cmd = new MySqlCommand(SelectQuery, con))
                using (MySqlDataReader rdr = cmd.ExecuteReader())
                {
                    dataGridView1.Rows.Clear();
                    dataGridView1.Columns.Clear();

                    dataGridView1.Columns.Add("IDevent", "Id");
                    dataGridView1.Columns["IDevent"].Visible = false;
                    dataGridView1.Columns.Add("Event", "Мероприятие");

                    var events = new List<(int Id, string Name)>();
                    rowCount = 0;

                    while (rdr.Read())
                    {
                        int eventId = Convert.ToInt32(rdr[0]);
                        string eventName = rdr[1].ToString();
                        events.Add((eventId, eventName));
                        rowCount++;
                    }

                    if (_lastInsertedEventId.HasValue)
                    {
                        var newEvent = events.FirstOrDefault(e => e.Id == _lastInsertedEventId.Value);
                        if (newEvent.Id != 0)
                        {
                            events.Remove(newEvent);
                            events.Insert(0, newEvent);
                        }
                    }

                    foreach (var eventItem in events)
                    {
                        dataGridView1.Rows.Add(eventItem.Id, eventItem.Name);
                    }

                    label5.Text = rowCount.ToString();

                    if (rowCount == 0)
                    {
                        MessageBox.Show("Данные не найдены", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    _lastInsertedEventId = null;
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

        //Проверка существования мероприятия в базе данных
        private bool IsEventExists(string eventName)
        {
            string query = "SELECT COUNT(*) FROM Events WHERE Event = @event;";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@event", eventName.Trim());

                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка проверки мероприятия: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return true;
                }
            }
        }

        //Проверка существования мероприятия исключая текущую запись
        private bool IsEventExistsExceptCurrent(int eventId, string eventName)
        {
            string query = "SELECT COUNT(*) FROM Events WHERE Event = @event AND IDevent != @eventId;";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@event", eventName.Trim());
                        cmd.Parameters.AddWithValue("@eventId", eventId);

                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка проверки мероприятия: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return true;
                }
            }
        }

        //Обработчик кнопки добавления мероприятия
        private void button1_Click(object sender, EventArgs e)
        {
            string eventName = textBox1.Text.Trim();

            if (string.IsNullOrEmpty(eventName))
            {
                MessageBox.Show("Заполните поле мероприятия", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (IsEventExists(eventName))
            {
                MessageBox.Show("Мероприятие с таким наименованием уже существует", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string query = "INSERT INTO Events (Event) VALUES (@event); SELECT LAST_INSERT_ID();";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@event", eventName);
                        int newId = Convert.ToInt32(cmd.ExecuteScalar());

                        _lastInsertedEventId = newId;

                        MessageBox.Show("Мероприятие успешно добавлено", "Успех",
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
                    MessageBox.Show($"Ошибка добавления мероприятия: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        //Обработчик кнопки обновления мероприятия
        private void button2_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null)
            {
                MessageBox.Show("Выберите мероприятие для редактирования", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int selectedId = Convert.ToInt32(dataGridView1.CurrentRow.Cells["IDevent"].Value);
            string newEventName = textBox1.Text.Trim();

            if (IsEventExistsExceptCurrent(selectedId, newEventName))
            {
                MessageBox.Show("Мероприятие с таким наименованием уже существует", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string query = "UPDATE Events SET Event = @event WHERE IDevent = @eventId";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@event", newEventName);
                        cmd.Parameters.AddWithValue("@eventId", selectedId);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Мероприятие успешно обновлено", "Успех",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);
                            textBox1.Clear();
                            FillDataGridView();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка обновления мероприятия: {ex.Message}", "Ошибка",
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
                string originalStatus = dataGridView1.CurrentRow.Cells["Event"].Value?.ToString() ?? "";
                button2.Enabled = (currentText != originalStatus);
            }
            else
            {
                button2.Enabled = false;
            }

            button4.Enabled = (dataGridView1.CurrentRow != null);
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
                    textBox1.Text = selectedRow.Cells["Event"].Value?.ToString() ?? "";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при заполнении полей: {ex.Message}");
                }

                UpdateButtonsState();
            }
        }

        //Обработчик кнопки удаления мероприятия
        private void button4_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null)
            {
                MessageBox.Show("Выберите мероприятие для удаления", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int selectedId = Convert.ToInt32(dataGridView1.CurrentRow.Cells["IDevent"].Value);
            string eventName = dataGridView1.CurrentRow.Cells["Event"].Value.ToString();

            DialogResult result = MessageBox.Show(
                $"Вы уверены, что хотите удалить мероприятие \"{eventName}\"?",
                "Подтверждение удаления",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            if (IsEventInUse(selectedId))
            {
                MessageBox.Show("Невозможно удалить мероприятие, так как оно используется в других таблицах",
                              "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string query = "DELETE FROM Events WHERE IDevent = @eventId";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@eventId", selectedId);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Мероприятие успешно удалено", "Успех",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);
                            textBox1.Clear();
                            FillDataGridView();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления мероприятия: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        //Проверка использования мероприятия в других таблицах
        private bool IsEventInUse(int eventId)
        {
            string checkQueries = @"SELECT COUNT(*) FROM Orders WHERE IdEvent = @eventId;";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();

                    using (MySqlCommand cmd = new MySqlCommand(checkQueries, con))
                    {
                        cmd.Parameters.AddWithValue("@eventId", eventId);
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
                    MessageBox.Show($"Ошибка проверки использования мероприятия: {ex.Message}", "Ошибка",
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
        private void Events_Load(object sender, EventArgs e)
        {
            ClearAllFields();
            _lastInsertedEventId = null;
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