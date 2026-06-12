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
        private int selectedEventId = -1;

        private int currentSelectedProductRow = -1;
        private int currentSelectedCartRow = -1;

        public MakingAnOrder()
        {
            InitializeComponent();

            FillFilterEvent();
            FillFilterCategory();

            // Подключаем обновленную версию события
            dateTimePicker2.ValueChanged += dateTimePicker2_ValueChanged_AntiClick;
            FillFilterShedule();

            FillDataGridView();

            dataGridView1.CellClick += dataGridView1_CellClick;
            dataGridView2.CellClick += dataGridView2_CellClick;
            button2.Click += button2_Click;
            numericUpDown1.ValueChanged += numericUpDown1_ValueChanged;
            comboBox5.SelectedIndexChanged += comboBox5_SelectedIndexChanged;

            button2.Enabled = false;
            button3.Enabled = false;
            button5.Enabled = false;

            dateTimePicker2.Value = DateTime.Now.AddDays(14);

            dateTimePicker1.MinDate = DateTime.Today;
            dateTimePicker1.MaxDate = DateTime.Today;
            dateTimePicker2.MinDate = DateTime.Today.AddDays(1);
            dateTimePicker2.Value = DateTime.Today.AddDays(1);
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

            // Заполняем label28 свободными датами на текущий месяц выбранной даты
            UpdateAvailableDatesLabel(dateTimePicker2.Value);
        }

        // НОВЫЙ МЕТОД: Обновляет label28 со свободными датами на указанный месяц
        private void UpdateAvailableDatesLabel(DateTime date)
        {
            // Определяем первый и последний день выбранного месяца
            DateTime firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            DateTime lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            // Ограничиваем максимальной датой из dateTimePicker2
            if (lastDayOfMonth > dateTimePicker2.MaxDate)
                lastDayOfMonth = dateTimePicker2.MaxDate;

            List<int> availableDays = new List<int>();

            // Проверяем каждую дату месяца
            for (DateTime currentDate = firstDayOfMonth; currentDate <= lastDayOfMonth; currentDate = currentDate.AddDays(1))
            {
                if (currentDate >= dateTimePicker2.MinDate && IsDateAvailable(currentDate))
                {
                    availableDays.Add(currentDate.Day);
                }
            }

            // Обновляем label28
            if (availableDays.Count == 0)
            {
                label28.Text = $"❌ В {GetMonthName(date.Month)} нет свободных дат для записи";
                label28.ForeColor = System.Drawing.Color.Black;
                return;
            }

            string datesText = FormatDaysForDisplay(availableDays);

            label28.Text = $"📅 Свободные даты в {GetMonthName(date.Month)}: {datesText}";
            label28.ForeColor = System.Drawing.Color.Black;
        }

        // Форматирует список дней для отображения
        private string FormatDaysForDisplay(List<int> days)
        {
            if (days.Count == 0) return "нет";
            if (days.Count <= 10)
            {
                return string.Join(", ", days);
            }

            // Если много дат, показываем начало и конец
            return $"{days.First()}, {days.First() + 1}, {days.First() + 2}...{days.Last() - 2}, {days.Last() - 1}, {days.Last()}";
        }

        // Возвращает название месяца в родительном падеже
        private string GetMonthName(int month)
        {
            string[] monthNames = { "", "январе", "феврале", "марте", "апреле", "мае", "июне",
                                    "июле", "августе", "сентябре", "октябре", "ноябре", "декабре" };
            return monthNames[month];
        }

        private void dateTimePicker2_ValueChanged_AntiClick(object sender, EventArgs e)
        {
            DateTime selectedDate = dateTimePicker2.Value.Date;

            // Проверяем, свободна ли выбранная пользователем дата
            if (!IsDateAvailable(selectedDate))
            {
                // Вычисляем ближайший день, где есть свободные окошки
                DateTime validDate = GetNearestAvailableDate(selectedDate);

                // Временно отключаем событие, чтобы избежать рекурсии
                dateTimePicker2.ValueChanged -= dateTimePicker2_ValueChanged_AntiClick;
                dateTimePicker2.Value = validDate;
                dateTimePicker2.ValueChanged += dateTimePicker2_ValueChanged_AntiClick;

                // Обновляем выбранную дату для дальнейшего использования
                selectedDate = validDate;
            }

            // Обновляем label28 для месяца выбранной даты
            UpdateAvailableDatesLabel(selectedDate);

            // Сюда управление дойдет ТОЛЬКО если дата имеет доступные окошки
            FillFilterShedule();
        }

        // Проверка: есть ли хотя бы один свободный временной слот на дату
        private bool IsDateAvailable(DateTime date)
        {
            // Проверяем, есть ли хотя бы один свободный временной слот на эту дату
            List<string> allTimeSlots = GetAllTimeSlots();
            if (allTimeSlots.Count == 0) return false;

            Dictionary<string, TimeSpan> startTimes = new Dictionary<string, TimeSpan>();
            Dictionary<string, TimeSpan> endTimes = new Dictionary<string, TimeSpan>();

            foreach (var slot in allTimeSlots)
            {
                string[] parts = slot.Split(new[] { " - " }, StringSplitOptions.None);
                if (parts.Length >= 2)
                {
                    startTimes[slot] = ParseTimeSpan(parts[0]);
                    endTimes[slot] = ParseTimeSpan(parts[1]);
                }
            }

            List<string> availableSlots = GetAvailableTimeSlotsForDate(date, allTimeSlots, startTimes, endTimes);
            return availableSlots.Count > 0;
        }

        // Поиск ближайшей доступной даты (вперед)
        private DateTime GetNearestAvailableDate(DateTime startDate)
        {
            // Если сама выбранная дата доступна, возвращаем её
            if (IsDateAvailable(startDate)) return startDate;

            // Если занята, проверяем следующие 60 дней
            for (int i = 1; i <= 60; i++)
            {
                DateTime nextDate = startDate.AddDays(i);
                if (nextDate > dateTimePicker2.MaxDate) break;

                if (IsDateAvailable(nextDate)) return nextDate;
            }

            // Если не нашли вперед, пробуем искать назад (на случай если выбрали слишком далеко)
            for (int i = 1; i <= 14; i++)
            {
                DateTime prevDate = startDate.AddDays(-i);
                if (prevDate < dateTimePicker2.MinDate) break;

                if (IsDateAvailable(prevDate)) return prevDate;
            }

            // Если вообще всё занято, возвращаем текущую дату
            return startDate;
        }

        // Получаем все временные слоты из БД (без проверки занятости)
        private List<string> GetAllTimeSlots()
        {
            List<string> allTimeSlots = new List<string>();

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand(@"SELECT StartTime, EndTime FROM CafeActivities.Schedule ORDER BY StartTime;", con);
                MySqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    TimeSpan startTime = (TimeSpan)rdr["StartTime"];
                    TimeSpan endTime = (TimeSpan)rdr["EndTime"];
                    string formattedTime = $"{startTime:hh\\:mm} - {endTime:hh\\:mm}";
                    allTimeSlots.Add(formattedTime);
                }
                rdr.Close();
            }

            return allTimeSlots;
        }

        private List<string> GetAvailableTimeSlotsForDate(DateTime date, List<string> allTimeSlots,
                                                          Dictionary<string, TimeSpan> startTimes,
                                                          Dictionary<string, TimeSpan> endTimes)
        {
            List<string> availableTimeSlots = new List<string>(allTimeSlots);
            string selectedDate = date.ToString("yyyy-MM-dd");

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                con.Open();

                string query = @"SELECT 
                                    s.StartTime, 
                                    s.EndTime 
                                 FROM CafeActivities.Orders o
                                 LEFT JOIN CafeActivities.Schedule s ON o.IdSchedule = s.IDschedule
                                 WHERE o.DateEvent = @selectedDate 
                                 AND o.IdStatus != 4";

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

                        foreach (var occupiedSlot in occupiedTimeSlots)
                        {
                            string[] occupiedParts = occupiedSlot.Split(new[] { " - " }, StringSplitOptions.None);
                            if (occupiedParts.Length >= 2)
                            {
                                TimeSpan occupiedStart = ParseTimeSpan(occupiedParts[0]);
                                TimeSpan occupiedEnd = ParseTimeSpan(occupiedParts[1]);

                                for (int i = availableTimeSlots.Count - 1; i >= 0; i--)
                                {
                                    string currentSlot = availableTimeSlots[i];
                                    string[] currentParts = currentSlot.Split(new[] { " - " }, StringSplitOptions.None);
                                    if (currentParts.Length >= 2)
                                    {
                                        TimeSpan slotStart = ParseTimeSpan(currentParts[0]);
                                        TimeSpan slotEnd = ParseTimeSpan(currentParts[1]);

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
            if (!string.IsNullOrEmpty(selectedClientPhone) && !string.IsNullOrEmpty(selectedClientName))
            {
                label5.Text = selectedClientPhone;
                label8.Text = selectedClientName;
            }
            else
            {
                label5.Text = "(не выбрано)";
                label8.Text = "(не выбрано)";
            }

            UpdateButton5State();
        }

        private void UpdateButton5State()
        {
            bool hasItemsInCart = dataView2 != null && dataView2.Rows.Count > 0;
            button5.Enabled = hasItemsInCart;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            List<string> missingFields = new List<string>();

            if (string.IsNullOrEmpty(selectedClientPhone) || selectedClientPhone == "(не выбрано)" || string.IsNullOrEmpty(selectedClientName) || selectedClientName == "(не выбрано)")
            {
                missingFields.Add("• Клиент (выберите или создайте клиента)");
            }

            if (dataView2 == null || dataView2.Rows.Count == 0)
            {
                missingFields.Add("• Товары в корзине (добавьте товары)");
            }

            if (comboBox5.SelectedIndex <= 0 || comboBox5.SelectedItem?.ToString() == "Все мероприятия")
            {
                missingFields.Add("• Мероприятие (выберите конкретное мероприятие)");
            }

            if (comboBox4.Items.Count == 0)
            {
                missingFields.Add("• Время проведения (на выбранную дату нет свободного времени)");
            }

            if (comboBox4.SelectedItem == null && comboBox4.Items.Count > 0)
            {
                missingFields.Add("• Время проведения (выберите время из списка)");
            }

            if (missingFields.Count > 0)
            {
                string errorMessage = "Для оформления заказа необходимо заполнить следующие поля:\n\n" +
                                      string.Join("\n", missingFields) +
                                      "\n\nПожалуйста, заполните их и повторите попытку.";

                MessageBox.Show(errorMessage, "Не заполнены обязательные поля",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (comboBox4.Items.Count > 0 && !IsTimeSlotAvailable(comboBox4.Text))
            {
                FillFilterShedule();
                return;
            }

            int totalAmount = CalculateTotalAmount();
            int prepayment = CalculatePrepayment(totalAmount);

            int scheduleId = GetScheduleId(comboBox4.Text);
            if (scheduleId == -1)
            {
                MessageBox.Show("Ошибка при определении времени", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            OrderData orderData = new OrderData
            {
                NumberOrder = label4.Text,
                NumberPhone = selectedClientPhone,
                NameClient = selectedClientName,
                DateOrder = dateTimePicker1.Value.ToString("yyyy-MM-dd"),
                Date = dateTimePicker2.Value.ToString("yyyy-MM-dd"),
                Time = comboBox4.Text,
                Category = label24.Text,
                Event = comboBox5.SelectedItem.ToString(),
                Weight = label26.Text,
                Dec = textBox2.Text,
                Photo = pictureBox1.Image,
                TotalAmount = totalAmount,
                Prepayment = prepayment
            };

            DataTable cartCopy = dataView2.Copy();

            this.Visible = false;
            ViewingAnOrderForMeneger viewingAnOrderForMeneger = new ViewingAnOrderForMeneger(cartCopy, orderData);
            viewingAnOrderForMeneger.ShowDialog();
            this.Close();
        }

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

        private int CalculatePrepayment(int totalAmount)
        {
            return (int)Math.Round(totalAmount * 0.10m);
        }

        private bool IsTimeSlotAvailable(string timeSlot)
        {
            string selectedDate = dateTimePicker2.Value.ToString("yyyy-MM-dd");

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                con.Open();

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

                string query = @"SELECT COUNT(*) FROM CafeActivities.Orders 
                                WHERE DateEvent = @selectedDate 
                                AND IdSchedule = @scheduleId
                                AND IdStatus != 4";

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

            if (CreatingAClient.ClientWasSelected && !string.IsNullOrEmpty(CreatingAClient.SelectedClientPhone))
            {
                selectedClientName = CreatingAClient.SelectedClientName;
                selectedClientPhone = CreatingAClient.SelectedClientPhone;
                UpdateClientInfo();
                CreatingAClient.ClientWasSelected = false;
                CreatingAClient.SelectedClientName = "";
                CreatingAClient.SelectedClientPhone = "";
            }
            else
            {
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
            using (MySqlConnection con = new MySqlConnection(conString))
            {
                con.Open();

                MySqlCommand cmd = new MySqlCommand(@"SELECT * FROM CafeActivities.Schedule ORDER BY StartTime;", con);
                MySqlDataReader rdr = cmd.ExecuteReader();

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
                rdr.Close();

                List<string> availableTimeSlots = GetAvailableTimeSlotsForDate(dateTimePicker2.Value, allTimeSlots, startTimes, endTimes);

                comboBox4.Items.Clear();
                foreach (var timeSlot in availableTimeSlots)
                {
                    comboBox4.Items.Add(timeSlot);
                }

                if (comboBox4.Items.Count > 0)
                    comboBox4.SelectedIndex = 0;
            }
        }

        private TimeSpan ParseTimeSpan(string timeString)
        {
            timeString = timeString.Trim();

            if (TimeSpan.TryParse(timeString, out TimeSpan result))
            {
                return result;
            }

            if (!timeString.Contains(":"))
            {
                timeString += ":00";
                if (TimeSpan.TryParse(timeString, out result))
                {
                    return result;
                }
            }

            return TimeSpan.Zero;
        }

        private bool DoTimeSlotsOverlap(TimeSpan start1, TimeSpan end1, TimeSpan start2, TimeSpan end2)
        {
            return (start1 < end2 && end1 > start2);
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

            List<string> conditions = new List<string>();

            if (comboBox3.SelectedIndex > 0 && comboBox3.SelectedItem.ToString() != "Все категории")
            {
                conditions.Add($"d.Category = '{MySqlHelper.EscapeString(comboBox3.SelectedItem.ToString())}'");
            }

            if (comboBox5.SelectedIndex > 0 && comboBox5.SelectedItem.ToString() != "Все мероприятия")
            {
                conditions.Add($"c.Event = '{MySqlHelper.EscapeString(comboBox5.SelectedItem.ToString())}'");
            }

            if (!string.IsNullOrEmpty(where))
            {
                conditions.Add($"p.Name LIKE '{MySqlHelper.EscapeString(where)}%'");
            }

            if (conditions.Count > 0)
            {
                conStr += " WHERE " + string.Join(" AND ", conditions);
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
                    dataGridView1.Columns["Article"].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                    dataGridView1.Columns.Add("Event", "Мероприятие");
                    dataGridView1.Columns["Event"].Visible = false;
                    dataGridView1.Columns.Add("Category", "Категория");
                    dataGridView1.Columns["Category"].Visible = false;
                    dataGridView1.Columns.Add("Name", "Наименование");
                    dataGridView1.Columns["Name"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                    dataGridView1.Columns.Add("Compound", "Описание");
                    dataGridView1.Columns["Compound"].Visible = false;
                    dataGridView1.Columns.Add("Weight", "Вес");
                    dataGridView1.Columns["Weight"].Visible = false;
                    dataGridView1.Columns.Add("Price", "Цена");
                    dataGridView1.Columns["Price"].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
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

            currentSelectedProductRow = -1;
            button2.Enabled = false;
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
                // Сбрасываем цвет ВСЕХ строк в ассортименте
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    row.DefaultCellStyle.BackColor = Color.White;
                }

                currentSelectedProductRow = e.RowIndex;

                // Выделяем новую строку цветом
                dataGridView1.Rows[currentSelectedProductRow].DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
                dataGridView1.Rows[currentSelectedProductRow].DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(217, 152, 22);

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
                else {
                    // Заглушка, если нет фото или файл не найден
                    string placeholderPath = Path.Combine(imagesFolder, "picture.png");
                    if (File.Exists(placeholderPath))
                    {
                        using (var fs = new FileStream(placeholderPath, FileMode.Open, FileAccess.Read))
                        {
                            pictureBox1.Image = Image.FromStream(fs);
                        }
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
            if (currentSelectedProductRow >= 0)
            {
                DataGridViewRow selectedRow = dataGridView1.Rows[currentSelectedProductRow];

                string article = selectedRow.Cells["Article"].Value.ToString();
                string name = selectedRow.Cells["Name"].Value.ToString();
                decimal price = Convert.ToDecimal(selectedRow.Cells["Price"].Value);

                // Проверяем, есть ли уже такой товар в корзине
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
                    // Если товар уже есть, просто обновляем информацию (но НЕ выделяем)
                    MessageBox.Show("Товар уже добавлен в корзину", "Информация",
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                // Сбрасываем выделение в таблице ассортимента
                if (currentSelectedProductRow >= 0 && currentSelectedProductRow < dataGridView1.Rows.Count)
                {
                    dataGridView1.Rows[currentSelectedProductRow].DefaultCellStyle.BackColor = Color.White;
                }

                currentSelectedProductRow = -1;
                button2.Enabled = false;

                // Очищаем информацию о товаре
                ClearProductDetails();

                // Снимаем выделение в таблице ассортимента
                dataGridView1.ClearSelection();

                // НЕ ВЫДЕЛЯЕМ добавленный товар в корзине
                // Просто обновляем корзину без выделения
                dataGridView2.ClearSelection();

                // Сбрасываем состояние корзины
                currentSelectedCartRow = -1;
                button3.Enabled = false;
                numericUpDown1.Enabled = false;
                numericUpDown1.Value = 0;
            }
        }

        // Метод для очистки информации о товаре
        private void ClearProductDetails()
        {
            label24.Text = "";
            label22.Text = "";
            label26.Text = "";
            textBox2.Text = "";
            pictureBox1.Image = null;
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
            UpdateButton5State();
        }

        void MakingAnOrder_Load(object sender, EventArgs e)
        {
            InitializeDataGridView2();
            InitializeNumericUpDown();

            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.ClearSelection();
            dataGridView1.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            dataGridView1.DefaultCellStyle.SelectionForeColor = dataGridView1.DefaultCellStyle.ForeColor;
            dataGridView2.ClearSelection();
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
            dataView2 = new DataTable();
            dataView2.Columns.Add("Article", typeof(string));
            dataView2.Columns.Add("Name", typeof(string));
            dataView2.Columns.Add("Price", typeof(decimal));
            dataView2.Columns.Add("Quantity", typeof(int));
            dataView2.Columns.Add("Total", typeof(decimal));

            dataGridView2.DataSource = dataView2;

            // АВТОМАТИЧЕСКОЕ ЗАПОЛНЕНИЕ
            dataGridView2.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView2.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dataGridView2.DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            // УСТАНАВЛИВАЕМ ПРОПОРЦИИ
            dataGridView2.Columns["Article"].HeaderText = "Артикул";
            dataGridView2.Columns["Article"].FillWeight = 17;  // 17% ширины

            dataGridView2.Columns["Name"].HeaderText = "Наименование";
            dataGridView2.Columns["Name"].FillWeight = 40;     // 40% ширины

            dataGridView2.Columns["Price"].HeaderText = "Цена";
            dataGridView2.Columns["Price"].FillWeight = 12;    // 12% ширины

            dataGridView2.Columns["Quantity"].HeaderText = "Кол-во";
            dataGridView2.Columns["Quantity"].FillWeight = 14; // 14% ширины

            dataGridView2.Columns["Total"].HeaderText = "Сумма";
            dataGridView2.Columns["Total"].FillWeight = 14;    // 14% ширины

            dataGridView2.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView2.ClearSelection();
            dataGridView2.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            dataGridView2.DefaultCellStyle.SelectionForeColor = Color.Black;

            dataGridView2.RowTemplate.Height = 40;

            button2.Enabled = false;
            button3.Enabled = false;
            numericUpDown1.Enabled = false;
        }

        private void dataGridView2_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // Проверяем, что клик был по существующей строке, а не по заголовку или пустой области
            if (e.RowIndex >= 0 && e.RowIndex < dataGridView2.Rows.Count)
            {
                // Дополнительная проверка: строка не должна быть новой (пустой) строкой
                if (dataGridView2.Rows[e.RowIndex].IsNewRow)
                    return;

                // Сбрасываем цвет ВСЕХ строк в корзине
                foreach (DataGridViewRow row in dataGridView2.Rows)
                {
                    if (row.Cells.Count > 0) // Проверяем, что строка не повреждена
                        row.DefaultCellStyle.BackColor = Color.White;
                }

                // Устанавливаем новую выделенную строку
                currentSelectedCartRow = e.RowIndex;

                // Выделяем новую строку цветом
                dataGridView2.Rows[currentSelectedCartRow].DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);

                // Получаем выбранную строку через DataTable (тоже с проверкой)
                if (e.RowIndex < dataView2.Rows.Count)
                {
                    DataRow selectedRow = dataView2.Rows[e.RowIndex];
                    int quantity = Convert.ToInt32(selectedRow["Quantity"]);

                    numericUpDown1.Value = quantity;
                    numericUpDown1.Enabled = true;
                    button3.Enabled = true;
                }
            }
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if (currentSelectedCartRow >= 0 && currentSelectedCartRow < dataView2.Rows.Count)
            {
                // Проверяем, что строка существует в dataGridView2
                if (currentSelectedCartRow >= dataGridView2.Rows.Count)
                {
                    currentSelectedCartRow = -1;
                    return;
                }

                DataRow selectedRow = dataView2.Rows[currentSelectedCartRow];
                string article = selectedRow["Article"].ToString();
                decimal price = Convert.ToDecimal(selectedRow["Price"]);
                int newQuantity = (int)numericUpDown1.Value;

                if (newQuantity == 0)
                {
                    // Удаляем строку
                    dataView2.Rows.Remove(selectedRow);

                    // Сбрасываем цвет удаленной строки
                    if (currentSelectedCartRow < dataGridView2.Rows.Count && dataGridView2.Rows[currentSelectedCartRow].Cells.Count > 0)
                    {
                        dataGridView2.Rows[currentSelectedCartRow].DefaultCellStyle.BackColor = Color.White;
                    }

                    currentSelectedCartRow = -1;
                    numericUpDown1.Enabled = false;
                    numericUpDown1.Value = 0;
                    button3.Enabled = false;

                    // Очищаем выделение в DataGridView
                    dataGridView2.ClearSelection();
                }
                else
                {
                    // Обновляем количество и сумму
                    selectedRow["Quantity"] = newQuantity;
                    selectedRow["Total"] = newQuantity * price;

                    // Обновляем отображение в DataGridView
                    if (currentSelectedCartRow < dataGridView2.Rows.Count && dataGridView2.Rows[currentSelectedCartRow].Cells.Count > 0)
                    {
                        dataGridView2.Rows[currentSelectedCartRow].Cells["Quantity"].Value = newQuantity;
                        dataGridView2.Rows[currentSelectedCartRow].Cells["Total"].Value = newQuantity * price;

                        // Сохраняем выделение (цвет не меняем)
                        dataGridView2.Rows[currentSelectedCartRow].DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
                    }
                }

                UpdateCartSummary();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (currentSelectedCartRow >= 0 && currentSelectedCartRow < dataView2.Rows.Count)
            {
                // Проверяем, что строка существует в dataGridView2
                if (currentSelectedCartRow < dataGridView2.Rows.Count && dataGridView2.Rows[currentSelectedCartRow].Cells.Count > 0)
                {
                    dataGridView2.Rows[currentSelectedCartRow].DefaultCellStyle.BackColor = Color.White;
                }

                // Удаляем строку из DataTable
                dataView2.Rows.RemoveAt(currentSelectedCartRow);

                // Сбрасываем выделение
                currentSelectedCartRow = -1;
                button3.Enabled = false;
                numericUpDown1.Enabled = false;
                numericUpDown1.Value = 0;

                // Очищаем выделение в DataGridView
                dataGridView2.ClearSelection();

                UpdateCartSummary();
            }
            else
            {
                MessageBox.Show("Выберите товар для удаления", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private int CalculateTotalAmount()
        {
            if (dataView2 == null || dataView2.Rows.Count == 0)
                return 0;

            int total = 0;
            foreach (DataRow row in dataView2.Rows)
            {
                total += Convert.ToInt32(row["Total"]);
            }
            return total;
        }

        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox5.SelectedIndex > 0)
            {
                selectedEventId = GetEventId(comboBox5.SelectedItem.ToString());
            }
            else
            {
                selectedEventId = -1;
            }

            FillDataGridView(textBox1.Text);
        }

        private void textBox1_Enter(object sender, EventArgs e)
        {
            // Получаем доступный список языков и устанавливаем нужный
            foreach (InputLanguage lang in InputLanguage.InstalledInputLanguages)
            {
                // Ищем русский язык
                if (lang.Culture.TwoLetterISOLanguageName == "ru")
                {
                    InputLanguage.CurrentInputLanguage = lang;
                    break;
                }
            }
        }
    }
}