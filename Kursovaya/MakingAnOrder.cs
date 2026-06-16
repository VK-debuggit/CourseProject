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

            UpdateAvailableDatesLabel(dateTimePicker2.Value);
        }

        //Обновление метки со свободными датами
        private void UpdateAvailableDatesLabel(DateTime date)
        {
            DateTime firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            DateTime lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            if (lastDayOfMonth > dateTimePicker2.MaxDate)
                lastDayOfMonth = dateTimePicker2.MaxDate;

            List<int> availableDays = new List<int>();

            for (DateTime currentDate = firstDayOfMonth; currentDate <= lastDayOfMonth; currentDate = currentDate.AddDays(1))
            {
                if (currentDate >= dateTimePicker2.MinDate && IsDateAvailable(currentDate))
                {
                    availableDays.Add(currentDate.Day);
                }
            }

            if (availableDays.Count == 0)
            {
                label28.Text = $"❌ В {GetMonthName(date.Month)} нет свободных дат для записи";
                label28.ForeColor = System.Drawing.Color.Black;
                return;
            }

            string datesText = FormatDaysForDisplay(availableDays);
            label28.Text = $"📅 Свободные даты в {GetMonthName(date.Month)}:\n{datesText}";
            label28.ForeColor = System.Drawing.Color.Black;
        }

        //Форматирование списка дней для отображения
        private string FormatDaysForDisplay(List<int> days)
        {
            if (days.Count == 0) return "нет";

            int numbersPerLine = 15;

            List<string> lines = new List<string>();

            for (int i = 0; i < days.Count; i += numbersPerLine)
            {
                var lineDays = days.Skip(i).Take(numbersPerLine);
                lines.Add(string.Join(", ", lineDays));
            }

            return string.Join(Environment.NewLine, lines);
        }

        //Получение названия месяца в родительном падеже
        private string GetMonthName(int month)
        {
            string[] monthNames = { "", "январе", "феврале", "марте", "апреле", "мае", "июне",
                                    "июле", "августе", "сентябре", "октябре", "ноябре", "декабре" };
            return monthNames[month];
        }

        //Обработчик изменения даты проведения
        private void dateTimePicker2_ValueChanged_AntiClick(object sender, EventArgs e)
        {
            DateTime selectedDate = dateTimePicker2.Value.Date;

            if (!IsDateAvailable(selectedDate))
            {
                DateTime validDate = GetNearestAvailableDate(selectedDate);

                dateTimePicker2.ValueChanged -= dateTimePicker2_ValueChanged_AntiClick;
                dateTimePicker2.Value = validDate;
                dateTimePicker2.ValueChanged += dateTimePicker2_ValueChanged_AntiClick;

                selectedDate = validDate;
            }

            UpdateAvailableDatesLabel(selectedDate);

            FillFilterShedule();
        }

        //Проверка наличия свободных слотов на дату
        private bool IsDateAvailable(DateTime date)
        {
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

        //Поиск ближайшей доступной даты
        private DateTime GetNearestAvailableDate(DateTime startDate)
        {
            if (IsDateAvailable(startDate)) return startDate;

            for (int i = 1; i <= 60; i++)
            {
                DateTime nextDate = startDate.AddDays(i);
                if (nextDate > dateTimePicker2.MaxDate) break;

                if (IsDateAvailable(nextDate)) return nextDate;
            }

            for (int i = 1; i <= 14; i++)
            {
                DateTime prevDate = startDate.AddDays(-i);
                if (prevDate < dateTimePicker2.MinDate) break;

                if (IsDateAvailable(prevDate)) return prevDate;
            }

            return startDate;
        }

        //Получение всех временных слотов из расписания
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

        //Получение доступных временных слотов на дату
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

        //Обработчик кнопки возврата в главное меню
        private void button4_Click(object sender, EventArgs e)
        {
            allowClose = true;
            this.Visible = false;
            MainFormMeneger mainFormMeneger = new MainFormMeneger();
            mainFormMeneger.ShowDialog();
            this.Close();
        }

        //Обработчик закрытия формы
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

        //Обновление информации о клиенте
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

        //Обновление состояния кнопки оформления заказа
        private void UpdateButton5State()
        {
            bool hasItemsInCart = dataView2 != null && dataView2.Rows.Count > 0;
            button5.Enabled = hasItemsInCart;
        }

        //Обработчик кнопки оформления заказа
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

        //Получение ID мероприятия по названию
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

        //Получение ID расписания по временному слоту
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

        //Расчет предоплаты
        private int CalculatePrepayment(int totalAmount)
        {
            return (int)Math.Round(totalAmount * 0.10m);
        }

        //Проверка доступности временного слота
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

        //Обработчик кнопки выбора/создания клиента
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

        //Загрузка мероприятий в выпадающий список
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

        //Загрузка категорий в выпадающий список
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

        //Загрузка доступных временных слотов
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

        //Преобразование строки времени
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

        //Проверка пересечения временных слотов
        private bool DoTimeSlotsOverlap(TimeSpan start1, TimeSpan end1, TimeSpan start2, TimeSpan end2)
        {
            return (start1 < end2 && end1 > start2);
        }

        //Ограничение ввода в поле поиска
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

        //Получение следующего номера заказа
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

        //Загрузка данных в таблицу ассортимента
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

        //Обработчик изменения выбранной категории
        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            FillDataGridView();
        }

        //Обработчик изменения текста поиска
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            FillDataGridView(textBox1.Text);
        }

        //Обработчик клика по ячейке таблицы ассортимента
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    row.DefaultCellStyle.BackColor = Color.White;
                }

                currentSelectedProductRow = e.RowIndex;

                dataGridView1.Rows[currentSelectedProductRow].DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
                dataGridView1.Rows[currentSelectedProductRow].DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(217, 152, 22);

                button2.Enabled = true;
                LoadProductDetails(e.RowIndex);
            }
        }

        //Загрузка деталей товара
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

        //Загрузка полной информации о товаре
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

        //Отображение деталей товара
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

        //Загрузка изображения товара
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
                else
                {
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

        //Обработчик кнопки добавления товара в корзину
        private void button2_Click(object sender, EventArgs e)
        {
            if (currentSelectedProductRow >= 0)
            {
                DataGridViewRow selectedRow = dataGridView1.Rows[currentSelectedProductRow];

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
                    MessageBox.Show("Товар уже добавлен в корзину", "Информация",
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                if (currentSelectedProductRow >= 0 && currentSelectedProductRow < dataGridView1.Rows.Count)
                {
                    dataGridView1.Rows[currentSelectedProductRow].DefaultCellStyle.BackColor = Color.White;
                }

                currentSelectedProductRow = -1;
                button2.Enabled = false;

                ClearProductDetails();

                dataGridView1.ClearSelection();

                dataGridView2.ClearSelection();

                currentSelectedCartRow = -1;
                button3.Enabled = false;
                numericUpDown1.Enabled = false;
                numericUpDown1.Value = 0;
            }
        }

        //Очистка информации о товаре
        private void ClearProductDetails()
        {
            label24.Text = "";
            label22.Text = "";
            label26.Text = "";
            textBox2.Text = "";
            pictureBox1.Image = null;
        }

        //Обновление итогов корзины
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

        //Обработчик загрузки формы
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

        //Инициализация NumericUpDown
        private void InitializeNumericUpDown()
        {
            numericUpDown1.Minimum = 0;
            numericUpDown1.Maximum = 100;
            numericUpDown1.Value = 0;
            numericUpDown1.Enabled = false;
        }

        //Инициализация таблицы корзины
        void InitializeDataGridView2()
        {
            dataView2 = new DataTable();
            dataView2.Columns.Add("Article", typeof(string));
            dataView2.Columns.Add("Name", typeof(string));
            dataView2.Columns.Add("Price", typeof(decimal));
            dataView2.Columns.Add("Quantity", typeof(int));
            dataView2.Columns.Add("Total", typeof(decimal));

            dataGridView2.DataSource = dataView2;

            dataGridView2.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView2.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dataGridView2.DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            dataGridView2.Columns["Article"].HeaderText = "Артикул";
            dataGridView2.Columns["Article"].FillWeight = 17;

            dataGridView2.Columns["Name"].HeaderText = "Наименование";
            dataGridView2.Columns["Name"].FillWeight = 40;

            dataGridView2.Columns["Price"].HeaderText = "Цена";
            dataGridView2.Columns["Price"].FillWeight = 12;

            dataGridView2.Columns["Quantity"].HeaderText = "Кол-во";
            dataGridView2.Columns["Quantity"].FillWeight = 14;

            dataGridView2.Columns["Total"].HeaderText = "Сумма";
            dataGridView2.Columns["Total"].FillWeight = 14;

            dataGridView2.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView2.ClearSelection();
            dataGridView2.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            dataGridView2.DefaultCellStyle.SelectionForeColor = Color.Black;

            dataGridView2.RowTemplate.Height = 40;

            button2.Enabled = false;
            button3.Enabled = false;
            numericUpDown1.Enabled = false;
        }

        //Обработчик клика по ячейке корзины
        private void dataGridView2_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < dataGridView2.Rows.Count)
            {
                if (dataGridView2.Rows[e.RowIndex].IsNewRow)
                    return;

                foreach (DataGridViewRow row in dataGridView2.Rows)
                {
                    if (row.Cells.Count > 0)
                        row.DefaultCellStyle.BackColor = Color.White;
                }

                currentSelectedCartRow = e.RowIndex;

                dataGridView2.Rows[currentSelectedCartRow].DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);

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

        //Обработчик изменения количества товара
        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if (currentSelectedCartRow >= 0 && currentSelectedCartRow < dataView2.Rows.Count)
            {
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
                    dataView2.Rows.Remove(selectedRow);

                    if (currentSelectedCartRow < dataGridView2.Rows.Count && dataGridView2.Rows[currentSelectedCartRow].Cells.Count > 0)
                    {
                        dataGridView2.Rows[currentSelectedCartRow].DefaultCellStyle.BackColor = Color.White;
                    }

                    currentSelectedCartRow = -1;
                    numericUpDown1.Enabled = false;
                    numericUpDown1.Value = 0;
                    button3.Enabled = false;

                    dataGridView2.ClearSelection();
                }
                else
                {
                    selectedRow["Quantity"] = newQuantity;
                    selectedRow["Total"] = newQuantity * price;

                    if (currentSelectedCartRow < dataGridView2.Rows.Count && dataGridView2.Rows[currentSelectedCartRow].Cells.Count > 0)
                    {
                        dataGridView2.Rows[currentSelectedCartRow].Cells["Quantity"].Value = newQuantity;
                        dataGridView2.Rows[currentSelectedCartRow].Cells["Total"].Value = newQuantity * price;

                        dataGridView2.Rows[currentSelectedCartRow].DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
                    }
                }

                UpdateCartSummary();
            }
        }

        //Обработчик кнопки удаления товара из корзины
        private void button3_Click(object sender, EventArgs e)
        {
            if (currentSelectedCartRow >= 0 && currentSelectedCartRow < dataView2.Rows.Count)
            {
                if (currentSelectedCartRow < dataGridView2.Rows.Count && dataGridView2.Rows[currentSelectedCartRow].Cells.Count > 0)
                {
                    dataGridView2.Rows[currentSelectedCartRow].DefaultCellStyle.BackColor = Color.White;
                }

                dataView2.Rows.RemoveAt(currentSelectedCartRow);

                currentSelectedCartRow = -1;
                button3.Enabled = false;
                numericUpDown1.Enabled = false;
                numericUpDown1.Value = 0;

                dataGridView2.ClearSelection();

                UpdateCartSummary();
            }
            else
            {
                MessageBox.Show("Выберите товар для удаления", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        //Расчет общей суммы заказа
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

        //Обработчик изменения выбранного мероприятия
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

        //Установка русской раскладки в поле поиска
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