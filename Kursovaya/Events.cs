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
        private Timer inactivityTimer;
        private int inactivityTimeout;
        private int rowCount = 0;
        private int? _lastInsertedEventId = null; // Хранит ID последнего добавленного мероприятия

        public Events()
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

        private void button3_Click(object sender, EventArgs e)
        {
            inactivityTimer.Stop();
            allowClose = true;
            this.Visible = false;
            Directories directories = new Directories();
            directories.ShowDialog();
            this.Close();
        }

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

                    // Временный список для хранения всех записей
                    var events = new List<(int Id, string Name)>();
                    rowCount = 0;

                    while (rdr.Read())
                    {
                        int eventId = Convert.ToInt32(rdr[0]);
                        string eventName = rdr[1].ToString();
                        events.Add((eventId, eventName));
                        rowCount++;
                    }

                    // Если есть новая запись, перемещаем её в начало
                    if (_lastInsertedEventId.HasValue)
                    {
                        var newEvent = events.FirstOrDefault(e => e.Id == _lastInsertedEventId.Value);
                        if (newEvent.Id != 0)
                        {
                            events.Remove(newEvent);
                            events.Insert(0, newEvent);
                        }
                    }

                    // Добавляем в DataGridView
                    foreach (var eventItem in events)
                    {
                        dataGridView1.Rows.Add(eventItem.Id, eventItem.Name);
                    }

                    label5.Text = rowCount.ToString();

                    // Показываем информацию о загруженных данных
                    if (rowCount == 0)
                    {
                        MessageBox.Show("Данные не найдены", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    // Сбрасываем ID после отображения
                    _lastInsertedEventId = null;

                    // Очищаем выделение и поля
                    ClearAllFields();
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
                    return true; // В случае ошибки считаем, что мероприятие существует
                }
            }
        }

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

        private void button1_Click(object sender, EventArgs e)
        {
            string eventName = textBox1.Text.Trim();

            // Валидация данных
            if (string.IsNullOrEmpty(eventName))
            {
                MessageBox.Show("Заполните поле мероприятия", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Проверка на существование
            if (IsEventExists(eventName))
            {
                MessageBox.Show("Мероприятие с таким наименованием уже существует", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Добавление в базу данных с получением ID новой записи
            string query = "INSERT INTO Events (Event) VALUES (@event); SELECT LAST_INSERT_ID();";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@event", eventName);
                        // Получаем ID только что добавленного мероприятия
                        int newId = Convert.ToInt32(cmd.ExecuteScalar());

                        // Сохраняем ID новой записи
                        _lastInsertedEventId = newId;

                        MessageBox.Show("Мероприятие успешно добавлено", "Успех",
                                      MessageBoxButtons.OK, MessageBoxIcon.Information);
                        textBox1.Clear();
                        FillDataGridView(); // Обновляем DataGridView

                        // Выделяем и показываем первую строку (новое мероприятие)
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

            // Проверка на существование (исключая текущее мероприятие)
            if (IsEventExistsExceptCurrent(selectedId, newEventName))
            {
                MessageBox.Show("Мероприятие с таким наименованием уже существует", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Обновление в базе данных
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
                            FillDataGridView(); // Обновляем DataGridView
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
                string originalStatus = dataGridView1.CurrentRow.Cells["Event"].Value?.ToString() ?? "";
                button2.Enabled = (currentText != originalStatus);
            }
            else
            {
                button2.Enabled = false;
            }

            // Кнопка удаления доступна только когда выбрана запись
            button4.Enabled = (dataGridView1.CurrentRow != null);
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
                    textBox1.Text = selectedRow.Cells["Event"].Value?.ToString() ?? "";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при заполнении полей: {ex.Message}");
                }

                // Обновляем состояние кнопок
                UpdateButtonsState();
            }
        }

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

            // Подтверждение удаления
            DialogResult result = MessageBox.Show(
                $"Вы уверены, что хотите удалить мероприятие \"{eventName}\"?",
                "Подтверждение удаления",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            // Проверка на использование мероприятия в других таблицах
            if (IsEventInUse(selectedId))
            {
                MessageBox.Show("Невозможно удалить мероприятие, так как оно используется в других таблицах",
                              "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Удаление из базы данных
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
                            FillDataGridView(); // Обновляем DataGridView
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

        private void ClearAllFields()
        {
            dataGridView1.ClearSelection();
            dataGridView1.CurrentCell = null;
            textBox1.Text = "";
            UpdateButtonsState();
        }

        private void Events_Load(object sender, EventArgs e)
        {
            // Очищаем все поля при загрузке формы
            ClearAllFields();
            _lastInsertedEventId = null;
        }
    }
}