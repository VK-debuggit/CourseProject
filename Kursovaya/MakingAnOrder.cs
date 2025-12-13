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
    public partial class MakingAnOrder : Form
    {
        string conString = $"host={Properties.Settings.Default.host};uid={Properties.Settings.Default.uid};pwd={Properties.Settings.Default.pwd};database={Properties.Settings.Default.database};";
        private Timer inactivityTimer;
        private int inactivityTimeout;
        private int rowCount = 0;
        private DataTable dataView2 = new DataTable();
        private int selectedProductRowIndex = -1;
        private int selectedIndex = -1;
        private string selectedClientName = "";
        private string selectedClientPhone = "";
        private int selectedEventId = -1; // ID выбранного мероприятия

        public MakingAnOrder()
        {
            InitializeComponent();

            FillFilterEvent();
            FillFilterCategory();

            dateTimePicker2.ValueChanged += dateTimePicker2_ValueChanged;
            FillFilterShedule();

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

            // Подключение обработчиков
            dataGridView1.CellClick += dataGridView1_CellClick;
            dataGridView2.CellClick += dataGridView2_CellClick;
            button2.Click += button2_Click;
            numericUpDown1.ValueChanged += numericUpDown1_ValueChanged;
            comboBox5.SelectedIndexChanged += comboBox5_SelectedIndexChanged;

            // Изначально кнопки неактивны
            button2.Enabled = false;
            button3.Enabled = false;
            button5.Enabled = false; // Кнопка "В заказ" неактивна пока не выбран клиент

            dateTimePicker2.Value = DateTime.Now.AddDays(14);

            dateTimePicker1.MinDate = DateTime.Today;
            dateTimePicker1.MaxDate = DateTime.Today;
            dateTimePicker2.MinDate = DateTime.Today;
            dateTimePicker2.MaxDate = DateTime.Today.AddMonths(6);

            button1.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button2.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button3.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button4.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button5.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            textBox2.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            comboBox3.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            comboBox4.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            comboBox5.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            textBox1.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            dataGridView1.BackgroundColor = System.Drawing.Color.FromArgb(255, 221, 153);
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dataGridView1.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            dataGridView2.BackgroundColor = System.Drawing.Color.FromArgb(255, 221, 153);
            dataGridView2.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dataGridView2.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            dateTimePicker1.CalendarMonthBackground = System.Drawing.Color.FromArgb(255, 221, 153);
            dateTimePicker2.CalendarMonthBackground = System.Drawing.Color.FromArgb(255, 221, 153);

            label4.Text = FindNumberOrder().ToString();
            numericUpDown1.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);

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

        private void button4_Click(object sender, EventArgs e)
        {
            allowClose = true;
            this.Visible = false;
            MainFormMeneger mainFormMeneger = new MainFormMeneger();
            mainFormMeneger.ShowDialog();
            this.Close();
        }

        private void MakingAnOrder_FormClosing(object sender, FormClosingEventArgs e)
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

        private void UpdateClientInfo()
        {
            // Показываем информацию о клиенте в label5 (телефон) и label8 (имя)
            if (!string.IsNullOrEmpty(selectedClientPhone) && !string.IsNullOrEmpty(selectedClientName))
            {
                label5.Text = selectedClientPhone;
                label8.Text = selectedClientName;

                // Активируем кнопку "В заказ"
                button5.Enabled = true;
            }
            else
            {
                label5.Text = "(не выбрано)";
                label8.Text = "(не выбрано)";
                button5.Enabled = false;
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            // Проверяем, что выбран клиент
            if (string.IsNullOrEmpty(selectedClientPhone) || selectedClientPhone == "(не выбрано)" || selectedClientName == "(не выбрано)")
            {
                MessageBox.Show("Сначала выберите клиента", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Проверяем, что есть товары в корзине
            if (dataView2 == null || dataView2.Rows.Count == 0)
            {
                MessageBox.Show("Корзина пуста", "Информация",
                              MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Проверяем, что выбрано время
            if (comboBox4.SelectedItem == null || comboBox4.Items.Count == 0)
            {
                MessageBox.Show("Выберите время мероприятия или выберите другую дату", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Проверяем, что выбрано мероприятие
            if (comboBox5.SelectedIndex == 0) // "Все мероприятия" выбрано
            {
                MessageBox.Show("Выберите конкретное мероприятие для заказа", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Дополнительная проверка: убедимся, что выбранное время все еще доступно
            if (!IsTimeSlotAvailable(comboBox4.Text))
            {
                MessageBox.Show("Выбранное время стало недоступно. Пожалуйста, выберите другое время.",
                              "Время занято", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                FillFilterShedule(); // Обновляем список доступного времени
                return;
            }

            // Рассчитываем общую сумму и предоплату
            decimal totalAmount = CalculateTotalAmount();
            decimal prepayment = CalculatePrepayment(totalAmount);

            // Получаем IdSchedule для выбранного времени
            int scheduleId = GetScheduleId(comboBox4.Text);
            if (scheduleId == -1)
            {
                MessageBox.Show("Ошибка при определении времени", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Собираем данные для передачи
            OrderData orderData = new OrderData
            {
                NumberOrder = label4.Text,
                NumberPhone = selectedClientPhone,
                NameClient = selectedClientName,
                DateOrder = dateTimePicker1.Value.ToString("yyyy-MM-dd"),
                Date = dateTimePicker2.Value.ToString("yyyy-MM-dd"),
                Time = comboBox4.Text,
                Category = label24.Text,
                Event = comboBox5.SelectedItem.ToString(), // Используем выбранное мероприятие
                Weight = label26.Text,
                Dec = textBox2.Text,
                Photo = pictureBox1.Image,
                TotalAmount = totalAmount,
                Prepayment = prepayment // 10% предоплата
            };

            // Создаем копию корзины для передачи
            DataTable cartCopy = dataView2.Copy();

            this.Visible = false;
            ViewingAnOrderForMeneger viewingAnOrderForMeneger = new ViewingAnOrderForMeneger(cartCopy, orderData);
            viewingAnOrderForMeneger.ShowDialog();
            this.Close();
        }

        // Метод для получения Id мероприятия по названию
        private int GetEventId(string eventName)
        {
            using (MySqlConnection con = new MySqlConnection(conString))
            {
                con.Open();

                string query = "SELECT IDevent FROM CafeActivities.Events WHERE Event = @eventName";

                using (MySqlCommand cmd = new MySqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@eventName", eventName);

                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        return Convert.ToInt32(result);
                    }
                }
            }

            return -1;
        }

        // Метод для получения Id расписания по времени
        private int GetScheduleId(string timeSlot)
        {
            string[] timeParts = timeSlot.Split(new[] { " - " }, StringSplitOptions.None);
            if (timeParts.Length < 2) return -1;

            TimeSpan startTime = ParseTimeSpan(timeParts[0]);
            TimeSpan endTime = ParseTimeSpan(timeParts[1]);

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                con.Open();

                string query = @"SELECT IDschedule FROM CafeActivities.Schedule 
                                WHERE StartTime = @startTime AND EndTime = @endTime";

                using (MySqlCommand cmd = new MySqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@startTime", startTime);
                    cmd.Parameters.AddWithValue("@endTime", endTime);

                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        return Convert.ToInt32(result);
                    }
                }
            }

            return -1;
        }

        // Метод для расчета предоплаты (10% от общей суммы)
        private decimal CalculatePrepayment(decimal totalAmount)
        {
            return Math.Round(totalAmount * 0.10m, 2);
        }

        private bool IsTimeSlotAvailable(string timeSlot)
        {
            string selectedDate = dateTimePicker2.Value.ToString("yyyy-MM-dd");

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                con.Open();

                // Получаем ID расписания для выбранного времени
                string[] timeParts = timeSlot.Split(new[] { " - " }, StringSplitOptions.None);
                if (timeParts.Length < 2) return true;

                TimeSpan startTime = ParseTimeSpan(timeParts[0]);
                TimeSpan endTime = ParseTimeSpan(timeParts[1]);

                string getScheduleIdQuery = @"SELECT IDschedule FROM CafeActivities.Schedule 
                                              WHERE StartTime = @startTime AND EndTime = @endTime";

                int scheduleId = -1;
                using (MySqlCommand cmd = new MySqlCommand(getScheduleIdQuery, con))
                {
                    cmd.Parameters.AddWithValue("@startTime", startTime);
                    cmd.Parameters.AddWithValue("@endTime", endTime);

                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        scheduleId = Convert.ToInt32(result);
                    }
                }

                if (scheduleId == -1) return true;

                // Проверяем, занято ли это время на выбранную дату
                string query = @"SELECT COUNT(*) FROM CafeActivities.Orders 
                                WHERE DateEvent = @selectedDate 
                                AND IdSchedule = @scheduleId
                                AND IdStatus != 4"; // 4 - Id для статуса "Отменен"

                using (MySqlCommand cmd = new MySqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@selectedDate", selectedDate);
                    cmd.Parameters.AddWithValue("@scheduleId", scheduleId);

                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count == 0;
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Visible = false;
            CreatingAClient creatingAClient = new CreatingAClient();
            creatingAClient.ShowDialog();

            // Проверяем, был ли выбран клиент в форме CreatingAClient
            if (CreatingAClient.ClientWasSelected && !string.IsNullOrEmpty(CreatingAClient.SelectedClientPhone))
            {
                selectedClientName = CreatingAClient.SelectedClientName;
                selectedClientPhone = CreatingAClient.SelectedClientPhone;

                // Обновляем информацию о клиенте
                UpdateClientInfo();

                // Сбрасываем флаг
                CreatingAClient.ClientWasSelected = false;
                CreatingAClient.SelectedClientName = "";
                CreatingAClient.SelectedClientPhone = "";
            }
            else
            {
                // Если клиент не выбран, оставляем значения по умолчанию
                label5.Text = "(не выбрано)";
                label8.Text = "(не выбрано)";
                button5.Enabled = false;
            }

            this.Visible = true;
        }

        void FillFilterEvent()
        {
            MySqlConnection con = new MySqlConnection(conString);
            con.Open();

            MySqlCommand cmd = new MySqlCommand(@"SELECT * FROM CafeActivities.Events;", con);
            MySqlDataReader rdr = cmd.ExecuteReader();

            comboBox5.Items.Clear();
            comboBox5.Items.Add("Все мероприятия");

            while (rdr.Read())
            {
                comboBox5.Items.Add(rdr[1].ToString());
            }

            comboBox5.SelectedIndex = 0;

            con.Close();
        }

        void FillFilterCategory()
        {
            MySqlConnection con = new MySqlConnection(conString);
            con.Open();

            MySqlCommand cmd = new MySqlCommand(@"SELECT * FROM CafeActivities.Categories;", con);
            MySqlDataReader rdr = cmd.ExecuteReader();

            comboBox3.Items.Clear();
            comboBox3.Items.Add("Все категории");

            while (rdr.Read())
            {
                comboBox3.Items.Add(rdr[1].ToString());
            }

            comboBox3.SelectedIndex = 0;

            con.Close();
        }

        void FillFilterShedule()
        {
            MySqlConnection con = new MySqlConnection(conString);
            con.Open();

            // Получаем все доступные временные интервалы
            MySqlCommand cmd = new MySqlCommand(@"SELECT * FROM CafeActivities.Schedule ORDER BY StartTime;", con);
            MySqlDataReader rdr = cmd.ExecuteReader();

            // Сохраняем все доступные интервалы для дальнейшего использования
            List<string> allTimeSlots = new List<string>();
            Dictionary<string, TimeSpan> startTimes = new Dictionary<string, TimeSpan>();
            Dictionary<string, TimeSpan> endTimes = new Dictionary<string, TimeSpan>();

            while (rdr.Read())
            {
                TimeSpan startTime = (TimeSpan)rdr["StartTime"];
                TimeSpan endTime = (TimeSpan)rdr["EndTime"];
                string formattedTime = $"{startTime:hh\\:mm} - {endTime:hh\\:mm}";
                allTimeSlots.Add(formattedTime);
                startTimes[formattedTime] = startTime;
                endTimes[formattedTime] = endTime;
            }

            con.Close();

            // Проверяем занятость для выбранной даты
            List<string> availableTimeSlots = GetAvailableTimeSlots(allTimeSlots, startTimes, endTimes);

            // Обновляем comboBox4
            comboBox4.Items.Clear();
            foreach (var timeSlot in availableTimeSlots)
            {
                comboBox4.Items.Add(timeSlot);
            }

            if (comboBox4.Items.Count > 0)
                comboBox4.SelectedIndex = 0;
            else if (comboBox4.Items.Count == 0)
            {
                MessageBox.Show("На выбранную дату нет свободных временных интервалов. Пожалуйста, выберите другую дату.",
                              "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private List<string> GetAvailableTimeSlots(List<string> allTimeSlots,
                                                  Dictionary<string, TimeSpan> startTimes,
                                                  Dictionary<string, TimeSpan> endTimes)
        {
            List<string> availableTimeSlots = new List<string>(allTimeSlots);

            if (dateTimePicker2.Value == null)
                return availableTimeSlots;

            string selectedDate = dateTimePicker2.Value.ToString("yyyy-MM-dd");

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                con.Open();

                // Получаем все занятые временные интервалы на выбранную дату
                string query = @"SELECT 
                                    s.StartTime, 
                                    s.EndTime 
                                 FROM CafeActivities.Orders o
                                 LEFT JOIN CafeActivities.Schedule s ON o.IdSchedule = s.IDschedule
                                 WHERE o.DateEvent = @selectedDate 
                                 AND o.IdStatus != 4"; // 4 - Id для статуса "Отменен"

                using (MySqlCommand cmd = new MySqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@selectedDate", selectedDate);

                    using (MySqlDataReader rdr = cmd.ExecuteReader())
                    {
                        List<string> occupiedTimeSlots = new List<string>();
                        while (rdr.Read())
                        {
                            if (!rdr.IsDBNull(0) && !rdr.IsDBNull(1))
                            {
                                TimeSpan startTime = (TimeSpan)rdr[0];
                                TimeSpan endTime = (TimeSpan)rdr[1];
                                string timeSlot = $"{startTime:hh\\:mm} - {endTime:hh\\:mm}";
                                occupiedTimeSlots.Add(timeSlot);
                            }
                        }

                        // Удаляем занятые интервалы и пересекающиеся с ними
                        foreach (var occupiedSlot in occupiedTimeSlots)
                        {
                            // Парсим занятый временной интервал
                            string[] occupiedParts = occupiedSlot.Split(new[] { " - " }, StringSplitOptions.None);
                            if (occupiedParts.Length >= 2)
                            {
                                TimeSpan occupiedStart = ParseTimeSpan(occupiedParts[0]);
                                TimeSpan occupiedEnd = ParseTimeSpan(occupiedParts[1]);

                                // Удаляем интервалы, которые полностью или частично пересекаются с занятым
                                for (int i = availableTimeSlots.Count - 1; i >= 0; i--)
                                {
                                    string currentSlot = availableTimeSlots[i];

                                    // Парсим текущий интервал
                                    string[] currentParts = currentSlot.Split(new[] { " - " }, StringSplitOptions.None);
                                    if (currentParts.Length >= 2)
                                    {
                                        TimeSpan slotStart = ParseTimeSpan(currentParts[0]);
                                        TimeSpan slotEnd = ParseTimeSpan(currentParts[1]);

                                        // Проверяем пересечение интервалов
                                        if (DoTimeSlotsOverlap(slotStart, slotEnd, occupiedStart, occupiedEnd))
                                        {
                                            availableTimeSlots.RemoveAt(i);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return availableTimeSlots;
        }

        private TimeSpan ParseTimeSpan(string timeString)
        {
            // Удаляем возможные пробелы
            timeString = timeString.Trim();

            // Пытаемся распарсить время
            if (TimeSpan.TryParse(timeString, out TimeSpan result))
            {
                return result;
            }

            // Если не получилось, пробуем добавить ":00" для минут
            if (!timeString.Contains(":"))
            {
                timeString += ":00";
                if (TimeSpan.TryParse(timeString, out result))
                {
                    return result;
                }
            }

            // Если все еще не получается, возвращаем TimeSpan.Zero
            return TimeSpan.Zero;
        }

        private TimeSpan GetStartTimeFromSlot(string timeSlot)
        {
            // Пример формата: "12:00 - 14:00"
            string[] parts = timeSlot.Split(new[] { " - " }, StringSplitOptions.None);
            if (parts.Length >= 1)
            {
                return ParseTimeSpan(parts[0]);
            }
            return TimeSpan.Zero;
        }

        private TimeSpan GetEndTimeFromSlot(string timeSlot)
        {
            // Пример формата: "12:00 - 14:00"
            string[] parts = timeSlot.Split(new[] { " - " }, StringSplitOptions.None);
            if (parts.Length >= 2)
            {
                return ParseTimeSpan(parts[1]);
            }
            return TimeSpan.Zero;
        }

        private bool DoTimeSlotsOverlap(TimeSpan start1, TimeSpan end1, TimeSpan start2, TimeSpan end2)
        {
            // Проверяем пересечение временных интервалов
            // Интервалы пересекаются, если:
            // 1. Начало первого внутри второго интервала
            // 2. Конец первого внутри второго интервала
            // 3. Первый интервал полностью содержит второй
            return (start1 < end2 && end1 > start2);
        }

        private void dateTimePicker2_ValueChanged(object sender, EventArgs e)
        {
            // При изменении даты обновляем доступное время
            FillFilterShedule();
        }

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

        private int FindNumberOrder()
        {
            using (MySqlConnection con = new MySqlConnection(conString))
            {
                con.Open();

                string query = "SELECT MAX(NumberOrder) FROM CafeActivities.Orders;";

                using (MySqlCommand cmd = new MySqlCommand(query, con))
                {
                    object result = cmd.ExecuteScalar();

                    if (result == null || result == DBNull.Value)
                    {
                        return 1;
                    }

                    int maxNumber = Convert.ToInt32(result);
                    return maxNumber + 1;
                }
            }
        }

        void FillDataGridView(string where = "")
        {
            string conStr = @"SELECT 
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
                                    LEFT JOIN Events c ON p.IdEvent = c.IDevent";

            bool hasWhere = false;

            // Фильтр по категории
            if (comboBox3.SelectedIndex > 0)
            {
                if (hasWhere)
                {
                    conStr += $" AND d.Category = '{comboBox3.SelectedItem.ToString()}'";
                }
                else
                {
                    conStr += $" WHERE d.Category = '{comboBox3.SelectedItem.ToString()}'";
                }
                hasWhere = true;
            }

            // Поиск по названию
            if (!string.IsNullOrEmpty(where))
            {
                if (hasWhere)
                {
                    conStr += $" AND p.Name LIKE '%{where}%'";
                }
                else
                {
                    conStr += $" WHERE p.Name LIKE '%{where}%'";
                }
            }

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                con.Open();

                using (MySqlCommand cmd = new MySqlCommand(conStr, con))
                using (MySqlDataReader rdr = cmd.ExecuteReader())
                {
                    dataGridView1.Rows.Clear();
                    dataGridView1.Columns.Clear();

                    DataGridViewImageColumn imageColumn = new DataGridViewImageColumn();
                    imageColumn.Name = "ProductPhoto";
                    imageColumn.HeaderText = "Фото";
                    imageColumn.ImageLayout = DataGridViewImageCellLayout.Zoom;
                    imageColumn.Width = 40;

                    dataGridView1.Columns.Add("Article", "Артикул");
                    dataGridView1.Columns.Add("Event", "Мероприятие");
                    dataGridView1.Columns["Event"].Visible = false;
                    dataGridView1.Columns.Add("Category", "Категория");
                    dataGridView1.Columns["Category"].Visible = false;
                    dataGridView1.Columns.Add("Name", "Наименование");
                    dataGridView1.Columns.Add("Compound", "Описание");
                    dataGridView1.Columns["Compound"].Visible = false;
                    dataGridView1.Columns.Add("Weight", "Вес");
                    dataGridView1.Columns["Weight"].Visible = false;
                    dataGridView1.Columns.Add("Price", "Цена");
                    dataGridView1.Columns.Add(imageColumn);
                    imageColumn.Visible = false;

                    int rowCount = 0;
                    while (rdr.Read())
                    {
                        string imagesFolder = @".\Resources\";
                        string photoFileName = rdr[7].ToString();
                        string fullImagePath = Path.Combine(imagesFolder, photoFileName);
                        Image img = null;

                        if (!string.IsNullOrEmpty(photoFileName) && File.Exists(fullImagePath))
                        {
                            using (var fs = new FileStream(fullImagePath, FileMode.Open, FileAccess.Read))
                            {
                                img = Image.FromStream(fs);
                            }
                        }
                        else
                        {
                            string placeholderPath = Path.Combine(imagesFolder, "picture.png");
                            if (File.Exists(placeholderPath))
                            {
                                using (var fs = new FileStream(placeholderPath, FileMode.Open, FileAccess.Read))
                                {
                                    img = Image.FromStream(fs);
                                }
                            }
                        }

                        int rowIndex = dataGridView1.Rows.Add(
                            rdr[0].ToString(),
                            rdr[1].ToString(),
                            rdr[2].ToString(),
                            rdr[3].ToString(),
                            rdr[4].ToString(),
                            rdr[5].ToString(),
                            rdr[6].ToString(),
                            img
                        );

                        if (string.IsNullOrEmpty(photoFileName))
                        {
                            dataGridView1.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 204, 153);
                        }
                        rowCount++;
                    }

                    if (rowCount == 0)
                    {
                        MessageBox.Show("Данные не найдены", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            FillDataGridView();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            FillDataGridView(textBox1.Text);
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    row.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(255, 221, 153);
                }

                dataGridView1.Rows[e.RowIndex].DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
                selectedProductRowIndex = e.RowIndex;

                button2.Enabled = true;

                LoadProductDetails(e.RowIndex);
            }
        }

        private void LoadProductDetails(int rowIndex)
        {
            try
            {
                if (rowIndex < 0 || rowIndex >= dataGridView1.Rows.Count)
                    return;

                DataGridViewRow selectedRow = dataGridView1.Rows[rowIndex];
                string article = selectedRow.Cells["Article"].Value?.ToString();

                if (string.IsNullOrEmpty(article))
                    return;

                LoadFullProductInfo(article);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки информации о товаре: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadFullProductInfo(string article)
        {
            string query = @"SELECT 
                        p.Article,
                        p.Name,
                        p.Compound,
                        p.Weight,
                        p.Price,
                        p.Photo,
                        c.Event,
                        cat.Category
                     FROM CafeActivities.Dishes p
                     LEFT JOIN CafeActivities.Events c ON p.IdEvent = c.IDevent
                     LEFT JOIN CafeActivities.Categories cat ON p.IdCategory = cat.IDcategory
                     WHERE p.Article = @article";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@article", article);

                    using (MySqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            DisplayProductDetails(rdr);
                        }
                    }
                }
            }
        }

        private void DisplayProductDetails(MySqlDataReader rdr)
        {
            try
            {
                label24.Text = rdr["Category"].ToString();
                label22.Text = rdr["Event"].ToString();
                label26.Text = rdr["Weight"].ToString();
                textBox2.Text = rdr["Compound"].ToString();

                LoadProductImage(rdr["Photo"]?.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отображения деталей товара: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadProductImage(string photoFileName)
        {
            try
            {
                pictureBox1.Image = null;

                if (string.IsNullOrEmpty(photoFileName))
                    return;

                string imagesFolder = @".\Resources\";
                string fullImagePath = Path.Combine(imagesFolder, photoFileName);

                if (File.Exists(fullImagePath))
                {
                    using (var fs = new FileStream(fullImagePath, FileMode.Open, FileAccess.Read))
                    {
                        pictureBox1.Image = Image.FromStream(fs);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                pictureBox1.Image = null;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (selectedProductRowIndex >= 0)
            {
                DataGridViewRow selectedRow = dataGridView1.Rows[selectedProductRowIndex];

                string article = selectedRow.Cells["Article"].Value.ToString();
                string name = selectedRow.Cells["Name"].Value.ToString();
                decimal price = Convert.ToDecimal(selectedRow.Cells["Price"].Value);

                DataRow[] existingRows = dataView2.Select($"Article = '{article}'");

                if (existingRows.Length == 0)
                {
                    DataRow newRow = dataView2.NewRow();
                    newRow["Article"] = article;
                    newRow["Name"] = name;
                    newRow["Price"] = price;
                    newRow["Quantity"] = 1;
                    newRow["Total"] = price;
                    dataView2.Rows.Add(newRow);

                    UpdateCartSummary();
                }
                else
                {
                    for (int i = 0; i < dataGridView2.Rows.Count; i++)
                    {
                        if (dataGridView2.Rows[i].Cells["Article"].Value.ToString() == article)
                        {
                            dataGridView2.ClearSelection();
                            dataGridView2.Rows[i].Selected = true;
                            dataGridView2_CellClick(null, new DataGridViewCellEventArgs(0, i));
                            break;
                        }
                    }
                }

                dataGridView1.Rows[selectedProductRowIndex].DefaultCellStyle.BackColor = Color.White;
                selectedProductRowIndex = -1;
                button2.Enabled = false;
            }
        }

        private void UpdateCartSummary()
        {
            decimal totalAmount = 0;
            int totalItems = 0;

            foreach (DataRow row in dataView2.Rows)
            {
                totalAmount += Convert.ToDecimal(row["Total"]);
                totalItems += Convert.ToInt32(row["Quantity"]);
            }

            label17.Text = totalAmount.ToString("C");
        }

        void MakingAnOrder_Load(object sender, EventArgs e)
        {
            InitializeDataGridView2();
            InitializeNumericUpDown();
        }

        private void InitializeNumericUpDown()
        {
            numericUpDown1.Minimum = 0;
            numericUpDown1.Maximum = 100;
            numericUpDown1.Value = 0;
            numericUpDown1.Enabled = false;
        }

        void InitializeDataGridView2()
        {
            dataView2.Columns.Add("Article", typeof(string));
            dataView2.Columns.Add("Name", typeof(string));
            dataView2.Columns.Add("Price", typeof(decimal));
            dataView2.Columns.Add("Quantity", typeof(int));
            dataView2.Columns.Add("Total", typeof(decimal));

            dataGridView2.DataSource = dataView2;

            dataGridView2.Columns["Article"].HeaderText = "Артикул";
            dataGridView2.Columns["Name"].HeaderText = "Наименование";
            dataGridView2.Columns["Price"].HeaderText = "Цена";
            dataGridView2.Columns["Quantity"].HeaderText = "Количество";
            dataGridView2.Columns["Total"].HeaderText = "Сумма";

            button2.Enabled = false;
        }

        private void dataGridView2_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                foreach (DataGridViewRow row in dataGridView2.Rows)
                {
                    row.DefaultCellStyle.BackColor = Color.White;
                }

                dataGridView2.Rows[e.RowIndex].DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
                selectedProductRowIndex = e.RowIndex;

                button3.Enabled = true;

                DataGridViewRow selectedRow = dataGridView2.Rows[e.RowIndex];
                int quantity = Convert.ToInt32(selectedRow.Cells["Quantity"].Value);
                numericUpDown1.Value = quantity;
                numericUpDown1.Enabled = true;
            }
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if (selectedProductRowIndex >= 0 && dataGridView2.Rows[selectedProductRowIndex] != null)
            {
                DataGridViewRow selectedRow = dataGridView2.Rows[selectedProductRowIndex];
                string article = selectedRow.Cells["Article"].Value.ToString();
                decimal price = Convert.ToDecimal(selectedRow.Cells["Price"].Value);
                int newQuantity = (int)numericUpDown1.Value;

                DataRow[] rows = dataView2.Select($"Article = '{article}'");
                if (rows.Length > 0)
                {
                    if (newQuantity == 0)
                    {
                        rows[0].Delete();

                        selectedProductRowIndex = -1;
                        numericUpDown1.Enabled = false;
                        numericUpDown1.Value = 0;

                        foreach (DataGridViewRow row in dataGridView2.Rows)
                        {
                            row.DefaultCellStyle.BackColor = Color.White;
                        }
                    }
                    else
                    {
                        rows[0]["Quantity"] = newQuantity;
                        rows[0]["Total"] = newQuantity * price;

                        selectedRow.Cells["Quantity"].Value = newQuantity;
                        selectedRow.Cells["Total"].Value = newQuantity * price;
                    }

                    UpdateCartSummary();
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (selectedProductRowIndex >= 0 && dataGridView2.Rows[selectedProductRowIndex] != null)
            {
                DataGridViewRow selectedRow = dataGridView2.Rows[selectedProductRowIndex];
                string article = selectedRow.Cells["Article"].Value.ToString();

                DataRow[] rowsToDelete = dataView2.Select($"Article = '{article}'");
                if (rowsToDelete.Length > 0)
                {
                    rowsToDelete[0].Delete();

                    UpdateCartSummary();

                    selectedProductRowIndex = -1;
                    button3.Enabled = false;

                    if (numericUpDown1 != null)
                    {
                        numericUpDown1.Enabled = false;
                        numericUpDown1.Value = 0;
                    }

                    foreach (DataGridViewRow row in dataGridView2.Rows)
                    {
                        row.DefaultCellStyle.BackColor = Color.White;
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите товар для удаления", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private decimal CalculateTotalAmount()
        {
            if (dataView2 == null || dataView2.Rows.Count == 0)
                return 0;

            decimal total = 0;
            foreach (DataRow row in dataView2.Rows)
            {
                total += Convert.ToDecimal(row["Total"]);
            }
            return total;
        }

        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            // При изменении мероприятия обновляем список товаров
            if (comboBox5.SelectedIndex > 0)
            {
                // Получаем ID выбранного мероприятия
                selectedEventId = GetEventId(comboBox5.SelectedItem.ToString());
            }
            else
            {
                selectedEventId = -1;
            }

            FillDataGridView();
        }
    }
}