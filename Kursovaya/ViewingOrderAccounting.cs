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
    public partial class ViewingOrderAccounting : Form
    {
        string conString = $"host={Properties.Settings.Default.host};uid={Properties.Settings.Default.uid};pwd={Properties.Settings.Default.pwd};database={Properties.Settings.Default.database};";
        private DateTime defaultStartDate;
        private DateTime defaultEndDate;
        private Timer searchTimer;
        private DataTable dataTable;
        private Timer inactivityTimer;
        private int inactivityTimeout;

        private int currentPage = 1;
        private int totalPages = 1;

        public ViewingOrderAccounting()
        {
            InitializeComponent();

            this.WindowState = FormWindowState.Maximized;

            searchTimer = new Timer();
            searchTimer.Interval = 500;
            searchTimer.Tick += SearchTimer_Tick;

            button1.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button2.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button3.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button4.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button5.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            dataGridView1.BackgroundColor = System.Drawing.Color.FromArgb(255, 221, 153);
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dataGridView1.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            textBox1.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            comboBox1.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);

            SetupDateControls();
            SetupUserInfo();
            FillFilterUsers();

            LoadData();
        }

        //Форматирование ФИО
        private string FormatFullName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                return "";

            string[] parts = fullName.Trim().Split(' ');

            if (parts.Length >= 3)
            {
                string lastName = parts[0];
                string firstName = parts[1].Length > 0 ? parts[1].Substring(0, 1) : "";
                string middleName = parts[2].Length > 0 ? parts[2].Substring(0, 1) : "";
                return $"{lastName} {firstName}.{middleName}.";
            }
            else if (parts.Length == 2)
            {
                string lastName = parts[0];
                string firstName = parts[1].Length > 0 ? parts[1].Substring(0, 1) : "";
                return $"{lastName} {firstName}.";
            }

            return fullName;
        }

        //Обработчик кнопки перехода к статистике
        private void button5_Click(object sender, EventArgs e)
        {
            DateTime startDate = dateTimePicker1.Value;
            DateTime endDate = dateTimePicker2.Value;

            string searchOrderNumber = textBox1.Text.Trim();

            string selectedEmployee = "Все сотрудники";
            if (comboBox1.SelectedIndex != 0 && comboBox1.SelectedItem != null)
            {
                selectedEmployee = comboBox1.SelectedItem.ToString();
            }

            List<string> selectedStatuses = new List<string>();
            if (checkBox1.Checked) selectedStatuses.Add(checkBox1.Text);
            if (checkBox2.Checked) selectedStatuses.Add(checkBox2.Text);
            if (checkBox3.Checked) selectedStatuses.Add(checkBox3.Text);

            if (!HasDataForStatistics(startDate, endDate, selectedEmployee, selectedStatuses))
            {
                MessageBox.Show("Данных для статистики за выбранный период не найдено.\nПопробуйте изменить параметры фильтрации.",
                                "Нет данных",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                return;
            }

            this.Visible = false;
            ViewStatistics viewStatistics = new ViewStatistics(startDate, endDate, selectedEmployee, selectedStatuses, searchOrderNumber);
            viewStatistics.ShowDialog();
            this.Close();
        }

        //Проверка наличия данных для статистики
        private bool HasDataForStatistics(DateTime startDate, DateTime endDate, string selectedEmployee, List<string> selectedStatuses)
        {
            try
            {
                List<string> conditions = new List<string>();

                string startDateStr = startDate.ToString("yyyy-MM-dd");
                string endDateStr = endDate.ToString("yyyy-MM-dd");
                conditions.Add($"(o.DateEvent >= '{startDateStr}' AND o.DateEvent <= '{endDateStr}')");

                if (selectedEmployee != "Все сотрудники")
                {
                    conditions.Add($"w.FullName = '{selectedEmployee.Replace("'", "''")}'");
                }

                if (selectedStatuses.Count > 0)
                {
                    List<string> statusConditions = new List<string>();
                    foreach (string status in selectedStatuses)
                    {
                        statusConditions.Add($"s.Status = '{status.Replace("'", "''")}'");
                    }
                    conditions.Add("(" + string.Join(" OR ", statusConditions) + ")");
                }

                string searchText = textBox1.Text.Trim();
                if (!string.IsNullOrEmpty(searchText))
                {
                    conditions.Add($"o.NumberOrder LIKE '{searchText}%'");
                }

                string whereClause = "WHERE " + string.Join(" AND ", conditions);

                string query = $@"
            SELECT COUNT(*) as TotalCount
            FROM CafeActivities.Orders o
            LEFT JOIN CafeActivities.Users w ON o.IdUser = w.IDuser
            LEFT JOIN CafeActivities.Status s ON o.IdStatus = s.IDstatus
            {whereClause}";

                using (MySqlConnection con = new MySqlConnection(conString))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при проверке: {ex.Message}", "Ошибка");
                return true;
            }
        }

        //Маскирование номера телефона
        private string FormatPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 4)
                return phoneNumber;

            string firstDigit = phoneNumber.Substring(0, 1);
            string lastFourDigits = phoneNumber.Substring(phoneNumber.Length - 4);
            string stars = new string('*', phoneNumber.Length - 4);

            return $"{firstDigit}{stars}{lastFourDigits}";
        }

        //Создание элементов пагинации
        void Pagination()
        {
            for (int j = 0, count = this.Controls.Count; j < count; ++j)
            {
                if (this.Controls[j].Name.StartsWith("page") ||
                    this.Controls[j].Name == "btnPrev" ||
                    this.Controls[j].Name == "btnNext")
                {
                    this.Controls.RemoveAt(j);
                    j--;
                    count--;
                }
            }

            totalPages = dataGridView1.Rows.Count / 20;
            if (Convert.ToBoolean(dataGridView1.Rows.Count % 20)) totalPages += 1;
            if (totalPages == 0) totalPages = 1;

            int yPosition = dataGridView1.Bottom + 10;
            int leftMargin = 13;

            Button btnPrev = new Button();
            btnPrev.Name = "btnPrev";
            btnPrev.Text = "◀";
            btnPrev.Font = new Font("Microsoft Sans Serif", 8, FontStyle.Bold);
            btnPrev.Size = new Size(30, 25);
            btnPrev.Location = new Point(leftMargin, yPosition);
            btnPrev.Click += new EventHandler(BtnPrev_Click);
            btnPrev.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            btnPrev.FlatStyle = FlatStyle.Flat;
            btnPrev.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnPrev);

            int x = leftMargin + 35;
            int step = 20;

            LinkLabel[] ll = new LinkLabel[totalPages];
            for (int i = 0; i < totalPages; i++)
            {
                int pageNumber = i + 1;
                ll[i] = new LinkLabel();
                ll[i].Text = Convert.ToString(pageNumber);
                ll[i].Font = new Font("Microsoft Sans Serif", 14, FontStyle.Regular);
                ll[i].Name = "page" + pageNumber;
                ll[i].AutoSize = true;
                ll[i].Location = new Point(x, yPosition);
                ll[i].Click += new EventHandler(LinkLabel_Click);
                ll[i].BackColor = Color.Transparent;

                if (pageNumber == currentPage)
                {
                    ll[i].LinkBehavior = LinkBehavior.NeverUnderline;
                    ll[i].ForeColor = Color.DarkRed;
                    ll[i].Font = new Font(ll[i].Font, FontStyle.Bold);
                }
                else
                {
                    ll[i].LinkBehavior = LinkBehavior.AlwaysUnderline;
                    ll[i].ForeColor = Color.Blue;
                    ll[i].Font = new Font(ll[i].Font, FontStyle.Regular);
                }

                this.Controls.Add(ll[i]);
                x += step;
            }

            Button btnNext = new Button();
            btnNext.Name = "btnNext";
            btnNext.Text = "▶";
            btnNext.Font = new Font("Microsoft Sans Serif", 8, FontStyle.Bold);
            btnNext.Size = new Size(30, 25);
            btnNext.Location = new Point(x, yPosition);
            btnNext.Click += new EventHandler(BtnNext_Click);
            btnNext.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            btnNext.FlatStyle = FlatStyle.Flat;
            btnNext.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnNext);

            ShowPage(currentPage);
            UpdateNavigationButtons();
        }

        //Отображение выбранной страницы
        private void ShowPage(int pageNumber)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageNumber > totalPages) pageNumber = totalPages;

            currentPage = pageNumber;

            int countRows = dataGridView1.Rows.Count;
            int sizePage = 20;
            int start = (pageNumber - 1) * sizePage;
            int stop = Math.Min(start + sizePage - 1, countRows - 1);

            for (int j = 0; j < countRows; ++j)
            {
                dataGridView1.Rows[j].Visible = (j >= start && j <= stop);
            }

            if (dataGridView1.Rows.Count > start)
            {
                dataGridView1.FirstDisplayedScrollingRowIndex = start;
            }

            UpdateRowCount();
        }

        //Обработчик кнопки "Назад"
        private void BtnPrev_Click(object sender, EventArgs e)
        {
            if (currentPage > 1)
            {
                ShowPage(currentPage - 1);
                Pagination();
            }
        }

        //Обработчик кнопки "Вперед"
        private void BtnNext_Click(object sender, EventArgs e)
        {
            if (currentPage < totalPages)
            {
                ShowPage(currentPage + 1);
                Pagination();
            }
        }

        //Обработчик клика по номеру страницы
        private void LinkLabel_Click(object sender, EventArgs e)
        {
            LinkLabel l = sender as LinkLabel;
            if (l != null && int.TryParse(l.Text, out int pageNumber))
            {
                ShowPage(pageNumber);
                Pagination();
            }
        }

        //Обновление состояния кнопок навигации
        private void UpdateNavigationButtons()
        {
            Button btnPrev = this.Controls.Find("btnPrev", false).FirstOrDefault() as Button;
            Button btnNext = this.Controls.Find("btnNext", false).FirstOrDefault() as Button;

            if (btnPrev != null)
            {
                btnPrev.Enabled = (currentPage > 1);
                btnPrev.BackColor = btnPrev.Enabled ?
                    System.Drawing.Color.FromArgb(217, 152, 22) :
                    System.Drawing.Color.FromArgb(200, 200, 200);
                btnPrev.ForeColor = btnPrev.Enabled ? Color.Black : Color.Gray;
            }

            if (btnNext != null)
            {
                btnNext.Enabled = (currentPage < totalPages);
                btnNext.BackColor = btnNext.Enabled ?
                    System.Drawing.Color.FromArgb(217, 152, 22) :
                    System.Drawing.Color.FromArgb(200, 200, 200);
                btnNext.ForeColor = btnNext.Enabled ? Color.Black : Color.Gray;
            }
        }

        //Настройка элементов выбора даты
        private void SetupDateControls()
        {
            DateTime minOrderDate = GetMinOrderDate();
            DateTime maxEventDate = GetMaxEventDate();

            dateTimePicker1.MinDate = minOrderDate;
            dateTimePicker1.MaxDate = maxEventDate;

            dateTimePicker2.MinDate = minOrderDate;
            dateTimePicker2.MaxDate = maxEventDate;

            defaultStartDate = minOrderDate;
            defaultEndDate = maxEventDate;

            dateTimePicker1.Value = defaultStartDate;
            dateTimePicker2.Value = defaultEndDate;
        }

        //Получение минимальной даты оформления заказа
        private DateTime GetMinOrderDate()
        {
            try
            {
                string query = "SELECT MIN(DateOfConclusion) FROM CafeActivities.Orders WHERE DateOfConclusion IS NOT NULL";
                using (MySqlConnection con = new MySqlConnection(conString))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            return Convert.ToDateTime(result);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения минимальной даты: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return DateTime.Today.AddYears(-1);
        }

        //Получение максимальной даты проведения мероприятия
        private DateTime GetMaxEventDate()
        {
            try
            {
                string query = "SELECT MAX(DateEvent) FROM CafeActivities.Orders WHERE DateEvent IS NOT NULL";
                using (MySqlConnection con = new MySqlConnection(conString))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            return Convert.ToDateTime(result);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения максимальной даты: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return DateTime.Today.AddMonths(6);
        }

        //Настройка информации о пользователе
        private void SetupUserInfo()
        {
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

        //Построение запроса для подсчета количества записей
        private string BuildCountQuery()
        {
            StringBuilder query = new StringBuilder();
            query.Append(@"SELECT COUNT(*) 
    FROM CafeActivities.Orders p 
    LEFT JOIN CafeActivities.Clients c ON p.IdClient = c.IDclient 
    LEFT JOIN CafeActivities.Events q ON p.IdEvent = q.IDevent
    LEFT JOIN CafeActivities.Status s ON p.IdStatus = s.IDstatus
    LEFT JOIN CafeActivities.Schedule r ON p.IdSchedule = r.IDschedule
    LEFT JOIN CafeActivities.Users w ON p.IdUser = w.IDuser");

            List<string> conditions = new List<string>();

            bool dateFilterApplied = (dateTimePicker1.Value != defaultStartDate) || (dateTimePicker2.Value != defaultEndDate);

            if (dateFilterApplied)
            {
                if (dateTimePicker1.Value <= dateTimePicker2.Value)
                {
                    string filterStartDate = dateTimePicker1.Value.ToString("yyyy-MM-dd");
                    string filterEndDate = dateTimePicker2.Value.ToString("yyyy-MM-dd");
                    conditions.Add($"(p.DateEvent >= '{filterStartDate}' AND p.DateEvent <= '{filterEndDate}')");
                }
            }

            if (comboBox1.SelectedIndex != 0 && comboBox1.SelectedItem != null)
            {
                conditions.Add($"w.FullName = '{MySqlHelper.EscapeString(comboBox1.SelectedItem.ToString())}'");
            }

            List<string> statusConditions = new List<string>();

            if (checkBox1.Checked)
                statusConditions.Add($"s.Status = '{MySqlHelper.EscapeString(checkBox1.Text)}'");
            if (checkBox2.Checked)
                statusConditions.Add($"s.Status = '{MySqlHelper.EscapeString(checkBox2.Text)}'");
            if (checkBox3.Checked)
                statusConditions.Add($"s.Status = '{MySqlHelper.EscapeString(checkBox3.Text)}'");

            if (statusConditions.Count > 0)
                conditions.Add("(" + string.Join(" OR ", statusConditions) + ")");

            if (conditions.Count > 0)
            {
                query.Append(" WHERE ");
                query.Append(string.Join(" AND ", conditions));
            }

            return query.ToString();
        }

        //Загрузка данных
        private void LoadData()
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                string query = BuildQuery();
                string countQuery = BuildCountQuery();

                using (MySqlConnection con = new MySqlConnection(conString))
                {
                    con.Open();

                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                    {
                        dataTable = new DataTable();
                        adapter.Fill(dataTable);
                    }

                    using (MySqlCommand cmd = new MySqlCommand(countQuery, con))
                    {
                        int totalCount = Convert.ToInt32(cmd.ExecuteScalar());
                        UpdateRowCount(totalCount);
                    }

                    DisplayDataInDataGridView(dataTable);
                }

                currentPage = 1;

                if (this.IsHandleCreated)
                {
                    this.BeginInvoke(new Action(() => Pagination()));
                }
                else
                {
                    this.HandleCreated += (s, e) =>
                    {
                        this.BeginInvoke(new Action(() => Pagination()));
                    };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        //Построение запроса для получения данных
        private string BuildQuery()
        {
            StringBuilder query = new StringBuilder();
            query.Append(@"SELECT 
                p.NumberOrder, 
                c.Name as IdClient,
                p.NumberPhoneClient,
                p.DateOfConclusion,
                p.DateEvent,
                CONCAT(DATE_FORMAT(r.StartTime, '%H:%i'), ' - ', DATE_FORMAT(r.EndTime, '%H:%i')) as IdSchedule,
                s.Status as IdStatus,
                q.Event as IdEvent,
                w.FullName as IdUser,
                p.Price,
                p.DiscountAmount,
                p.PriceAll,
                p.Prepayment
            FROM CafeActivities.Orders p 
            LEFT JOIN CafeActivities.Clients c ON p.IdClient = c.IDclient 
            LEFT JOIN CafeActivities.Events q ON p.IdEvent = q.IDevent
            LEFT JOIN CafeActivities.Status s ON p.IdStatus = s.IDstatus
            LEFT JOIN CafeActivities.Schedule r ON p.IdSchedule = r.IDschedule
            LEFT JOIN CafeActivities.Users w ON p.IdUser = w.IDuser");

            List<string> conditions = new List<string>();

            bool dateFilterApplied = (dateTimePicker1.Value != defaultStartDate) || (dateTimePicker2.Value != defaultEndDate);

            if (dateFilterApplied)
            {
                if (dateTimePicker1.Value <= dateTimePicker2.Value)
                {
                    string filterStartDate = dateTimePicker1.Value.ToString("yyyy-MM-dd");
                    string filterEndDate = dateTimePicker2.Value.ToString("yyyy-MM-dd");
                    conditions.Add($"(p.DateEvent >= '{filterStartDate}' AND p.DateEvent <= '{filterEndDate}')");
                }
                else
                {
                    MessageBox.Show("Дата 'С' не может быть больше даты 'До'", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    dateTimePicker2.Value = dateTimePicker1.Value;
                    return BuildQuery();
                }
            }

            if (comboBox1.SelectedIndex != 0 && comboBox1.SelectedItem != null)
            {
                conditions.Add($"w.FullName = '{MySqlHelper.EscapeString(comboBox1.SelectedItem.ToString())}'");
            }

            List<string> statusConditions = new List<string>();

            if (checkBox1.Checked)
                statusConditions.Add($"s.Status = '{MySqlHelper.EscapeString(checkBox1.Text)}'");
            if (checkBox2.Checked)
                statusConditions.Add($"s.Status = '{MySqlHelper.EscapeString(checkBox2.Text)}'");
            if (checkBox3.Checked)
                statusConditions.Add($"s.Status = '{MySqlHelper.EscapeString(checkBox3.Text)}'");

            if (statusConditions.Count > 0)
                conditions.Add("(" + string.Join(" OR ", statusConditions) + ")");

            if (conditions.Count > 0)
            {
                query.Append(" WHERE ");
                query.Append(string.Join(" AND ", conditions));
            }

            query.Append(" ORDER BY p.DateEvent ASC, p.NumberOrder ASC");
            return query.ToString();
        }

        //Отображение данных в DataGridView
        private void DisplayDataInDataGridView(DataTable tableToDisplay)
        {
            if (tableToDisplay == null) return;

            dataGridView1.Rows.Clear();

            if (dataGridView1.Columns.Count == 0)
            {
                dataGridView1.Columns.Add("NumberOrder", "Номер заказа");
                dataGridView1.Columns.Add("IdClient", "ФИО клиента");
                dataGridView1.Columns.Add("NumberPhoneClient", "Номер телефона клиента");
                dataGridView1.Columns.Add("DateOfConclusion", "Дата оформления");
                dataGridView1.Columns.Add("DateEvent", "Дата проведения");
                dataGridView1.Columns.Add("IdSchedule", "Время проведения");
                dataGridView1.Columns.Add("IdStatus", "Статус");
                dataGridView1.Columns.Add("IdEvent", "Мероприятие");
                dataGridView1.Columns.Add("IdUser", "ФИО сотрудника");
                dataGridView1.Columns.Add("Price", "Цена");
                dataGridView1.Columns.Add("DiscountAmount", "Сумма скидки");
                dataGridView1.Columns.Add("PriceAll", "Полная стоимость");
                dataGridView1.Columns.Add("Prepayment", "Предоплата");
            }

            string searchText = textBox1.Text.Trim();
            DataView dv = new DataView(tableToDisplay);

            if (!string.IsNullOrEmpty(searchText))
                dv.RowFilter = $"CONVERT(NumberOrder, 'System.String') LIKE '{searchText}%'";

            int totalSum = 0;
            foreach (DataRowView rowView in dv)
            {
                DataRow row = rowView.Row;
                int rowIndex = dataGridView1.Rows.Add(
                    row["NumberOrder"],
                    FormatFullName(row["IdClient"].ToString()),
                    FormatPhoneNumber(row["NumberPhoneClient"].ToString()),
                    FormatDate(row["DateOfConclusion"]),
                    FormatDate(row["DateEvent"]),
                    row["IdSchedule"],
                    row["IdStatus"],
                    row["IdEvent"],
                    FormatFullName(row["IdUser"].ToString()),
                    row["Price"],
                    row["DiscountAmount"],
                    row["PriceAll"],
                    row["Prepayment"]
                );

                string status = row["IdStatus"].ToString();

                int priceAll = 0;
                int prepayment = 0;

                if (row["PriceAll"] != null && row["PriceAll"] != DBNull.Value)
                    priceAll = Convert.ToInt32(row["PriceAll"]);
                else
                    priceAll = 0;

                if (row["Prepayment"] != null && row["Prepayment"] != DBNull.Value)
                    prepayment = Convert.ToInt32(row["Prepayment"]);
                else
                    prepayment = 0;

                DataGridViewRow dataGridRow = dataGridView1.Rows[rowIndex];

                switch (status)
                {
                    case "Принят":
                        foreach (DataGridViewCell cell in dataGridRow.Cells)
                        {
                            cell.Style.BackColor = Color.FromArgb(255, 255, 102);
                        }
                        totalSum += prepayment;
                        break;
                    case "Оплачен":
                        foreach (DataGridViewCell cell in dataGridRow.Cells)
                        {
                            cell.Style.BackColor = Color.FromArgb(170, 255, 170);
                        }
                        totalSum += priceAll;
                        break;
                    case "Отменен":
                        foreach (DataGridViewCell cell in dataGridRow.Cells)
                        {
                            cell.Style.BackColor = Color.FromArgb(255, 182, 182);
                        }
                        totalSum += prepayment;
                        break;
                }
            }

            label12.Text = totalSum.ToString("C0");
        }

        //Форматирование даты
        private string FormatDate(object dateValue)
        {
            if (dateValue == null || dateValue == DBNull.Value)
                return "";

            if (DateTime.TryParse(dateValue.ToString(), out DateTime date))
            {
                return date.ToString("dd.MM.yyyy");
            }

            return dateValue.ToString();
        }

        //Обновление счетчика строк
        private void UpdateRowCount(int totalFilteredCount = 0)
        {
            int visibleOnPage = 0;
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                if (dataGridView1.Rows[i].Visible)
                {
                    visibleOnPage++;
                }
            }

            if (totalFilteredCount == 0)
            {
                totalFilteredCount = dataGridView1.Rows.Count;
            }

            label4.Text = $"{visibleOnPage} из {totalFilteredCount}";
        }

        //Обработчик тиков таймера поиска
        private void SearchTimer_Tick(object sender, EventArgs e)
        {
            searchTimer.Stop();
            if (dataTable != null)
            {
                DisplayDataInDataGridView(dataTable);
                currentPage = 1;
                Pagination();
            }
        }

        //Обработчик изменения текста в поле поиска
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBox1.Text))
            {
                string digitsOnly = new string(textBox1.Text.Where(char.IsDigit).ToArray());
                if (textBox1.Text != digitsOnly)
                {
                    textBox1.Text = digitsOnly;
                    textBox1.SelectionStart = textBox1.Text.Length;
                }
            }

            searchTimer.Stop();
            searchTimer.Start();
        }

        //Загрузка сотрудников в выпадающий список
        void FillFilterUsers()
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(conString))
                {
                    con.Open();

                    using (MySqlCommand cmd = new MySqlCommand(@"SELECT * FROM CafeActivities.Users WHERE IdRole = 2;", con))
                    using (MySqlDataReader rdr = cmd.ExecuteReader())
                    {
                        comboBox1.Items.Clear();
                        comboBox1.Items.Add("Все сотрудники");

                        while (rdr.Read())
                            comboBox1.Items.Add(rdr[1].ToString());

                        comboBox1.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке списка сотрудников: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //Обработчик кнопки сброса фильтров
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                textBox1.Text = "";
                FillFilterUsers();

                dateTimePicker1.Value = dateTimePicker1.MinDate;
                dateTimePicker2.Value = dateTimePicker2.MaxDate;

                checkBox1.Checked = false;
                checkBox2.Checked = false;
                checkBox3.Checked = false;

                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сбросе фильтров: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //Обработчик кнопки просмотра заказа
        private void button2_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите заказ для просмотра", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataGridViewRow selectedRow = dataGridView1.SelectedRows[0];
            string orderId = selectedRow.Cells["NumberOrder"].Value?.ToString();

            if (string.IsNullOrEmpty(orderId))
            {
                MessageBox.Show("Не удалось получить номер заказа", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            allowClose = true;
            this.Visible = false;
            ViewingOrderForDirector viewingOrderForDirector = new ViewingOrderForDirector(orderId);
            viewingOrderForDirector.ShowDialog();
            this.Close();
        }

        //Обработчик кнопки возврата в главное меню
        private void button3_Click(object sender, EventArgs e)
        {
            allowClose = true;
            this.Visible = false;
            MainFormDirector mainFormDirector = new MainFormDirector();
            mainFormDirector.ShowDialog();
            this.Close();
        }

        //Обработчик кнопки экспорта в Excel
        private void button4_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0 || (dataGridView1.Rows.Count == 1 && dataGridView1.Rows[0].IsNewRow))
            {
                DialogResult result = MessageBox.Show(
                    "Данных для экспорта в Excel за выбранный период не найдено.\nПопробуйте изменить параметры фильтрации.",
                    "Нет данных",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            ExportToExcel();
        }

        //Включение кнопки просмотра
        private void buttonEnable()
        {
            button2.Enabled = true;
        }

        //Обработчик клика по ячейке DataGridView
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            buttonEnable();
        }

        //Обработчик изменения выбранного сотрудника
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadData();
        }

        //Обработчик изменения даты начала
        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            if (dateTimePicker1.Value > dateTimePicker2.Value)
            {
                dateTimePicker2.Value = dateTimePicker1.Value;
            }
            LoadData();
        }

        //Обработчик изменения даты окончания
        private void dateTimePicker2_ValueChanged(object sender, EventArgs e)
        {
            if (dateTimePicker2.Value < dateTimePicker1.Value)
            {
                dateTimePicker1.Value = dateTimePicker2.Value;
            }
            LoadData();
        }

        //Обработчик изменения состояния чекбоксов статусов
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            LoadData();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            LoadData();
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            LoadData();
        }

        private bool allowClose = false;

        //Обработчик закрытия формы
        private void ViewingOrderAccounting_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.ApplicationExitCall)
                return;

            if (!allowClose)
                e.Cancel = true;
        }

        //Экспорт в Excel
        private void ExportToExcel()
        {
            Microsoft.Office.Interop.Excel.Application excelApp = null;
            Microsoft.Office.Interop.Excel.Workbook workbook = null;

            try
            {
                excelApp = new Microsoft.Office.Interop.Excel.Application();
                excelApp.Visible = true;

                workbook = excelApp.Workbooks.Add();

                Microsoft.Office.Interop.Excel.Worksheet dataWorksheet = workbook.Worksheets[1];
                dataWorksheet.Name = "Данные по заказам";

                Microsoft.Office.Interop.Excel.Worksheet statsWorksheet = workbook.Worksheets.Add();
                statsWorksheet.Name = "Статистика";

                dataWorksheet.Cells[1, 1] = "ОТЧЕТ ПО ЗАКАЗАМ";
                Microsoft.Office.Interop.Excel.Range titleRange = dataWorksheet.Range[dataWorksheet.Cells[1, 1], dataWorksheet.Cells[1, 3]];
                titleRange.Merge();
                titleRange.Font.Bold = true;
                titleRange.Font.Size = 14;
                titleRange.HorizontalAlignment = Microsoft.Office.Interop.Excel.XlHAlign.xlHAlignCenter;

                string periodInfo = $"Период отчета: с {dateTimePicker1.Value:dd.MM.yyyy} по {dateTimePicker2.Value:dd.MM.yyyy}";
                dataWorksheet.Cells[2, 1] = periodInfo;
                Microsoft.Office.Interop.Excel.Range periodRange = dataWorksheet.Range[dataWorksheet.Cells[2, 1], dataWorksheet.Cells[2, 3]];
                periodRange.Merge();
                periodRange.Font.Size = 11;
                periodRange.Font.Italic = true;

                int headerRow = 5;

                for (int i = 0; i < dataGridView1.Columns.Count; i++)
                {
                    if (dataGridView1.Columns[i].Visible)
                    {
                        dataWorksheet.Cells[headerRow, i + 1] = dataGridView1.Columns[i].HeaderText;
                    }
                }

                DataTable fullDataTable = GetFullDataForExport();

                int rowIndex = headerRow + 1;
                int dataGridRowIndex = 0;

                foreach (DataGridViewRow dataGridRow in dataGridView1.Rows)
                {
                    if (!dataGridRow.IsNewRow)
                    {
                        int colIndex = 1;
                        foreach (DataGridViewColumn column in dataGridView1.Columns)
                        {
                            if (column.Visible)
                            {
                                object cellValue = dataGridRow.Cells[column.Name].Value?.ToString() ?? "";

                                if (column.Name == "NumberPhoneClient")
                                {
                                    if (fullDataTable != null && dataGridRowIndex < fullDataTable.Rows.Count)
                                    {
                                        cellValue = fullDataTable.Rows[dataGridRowIndex]["NumberPhoneClient"].ToString();
                                    }
                                }

                                dataWorksheet.Cells[rowIndex, colIndex] = cellValue;
                                colIndex++;
                            }
                        }
                        rowIndex++;
                        dataGridRowIndex++;
                    }
                }

                int statusColumnIndex = -1;
                for (int i = 0; i < dataGridView1.Columns.Count; i++)
                {
                    if (dataGridView1.Columns[i].HeaderText == "Статус" && dataGridView1.Columns[i].Visible)
                    {
                        statusColumnIndex = i + 1;
                        break;
                    }
                }

                if (statusColumnIndex != -1)
                {
                    for (int row = headerRow + 1; row <= rowIndex - 1; row++)
                    {
                        Microsoft.Office.Interop.Excel.Range statusCell = dataWorksheet.Cells[row, statusColumnIndex];
                        string statusValue = statusCell.Value?.ToString();

                        if (!string.IsNullOrEmpty(statusValue))
                        {
                            switch (statusValue)
                            {
                                case "Принят":
                                    statusCell.Interior.Color = System.Drawing.ColorTranslator.ToOle(Color.FromArgb(255, 253, 213));
                                    break;
                                case "Оплачен":
                                    statusCell.Interior.Color = System.Drawing.ColorTranslator.ToOle(Color.FromArgb(220, 255, 220));
                                    break;
                                case "Отменен":
                                    statusCell.Interior.Color = System.Drawing.ColorTranslator.ToOle(Color.FromArgb(255, 220, 220));
                                    break;
                            }

                            statusCell.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                            statusCell.Borders.Weight = Microsoft.Office.Interop.Excel.XlBorderWeight.xlThin;
                            statusCell.HorizontalAlignment = Microsoft.Office.Interop.Excel.XlHAlign.xlHAlignCenter;
                        }
                    }
                }

                Microsoft.Office.Interop.Excel.Range tableHeaders = dataWorksheet.Range[
                    dataWorksheet.Cells[headerRow, 1],
                    dataWorksheet.Cells[headerRow, dataGridView1.Columns.Count]];
                tableHeaders.Font.Bold = true;
                tableHeaders.Interior.Color = System.Drawing.ColorTranslator.ToOle(Color.FromArgb(240, 240, 240));
                tableHeaders.HorizontalAlignment = Microsoft.Office.Interop.Excel.XlHAlign.xlHAlignCenter;

                Microsoft.Office.Interop.Excel.Range dataRange = dataWorksheet.Range[
                    dataWorksheet.Cells[headerRow, 1],
                    dataWorksheet.Cells[rowIndex - 1, dataGridView1.Columns.Count]];
                dataRange.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                dataRange.Borders.Weight = Microsoft.Office.Interop.Excel.XlBorderWeight.xlThin;

                Microsoft.Office.Interop.Excel.Range allDataRange = dataWorksheet.UsedRange;
                allDataRange.Columns.AutoFit();

                foreach (Microsoft.Office.Interop.Excel.Range column in allDataRange.Columns)
                {
                    if (column.ColumnWidth > 30)
                        column.ColumnWidth = 30;
                }

                statsWorksheet.Cells[1, 1] = "СТАТИСТИКА ПО ЗАКАЗАМ";
                Microsoft.Office.Interop.Excel.Range statsTitleRange = statsWorksheet.Range[statsWorksheet.Cells[1, 1], statsWorksheet.Cells[1, 2]];
                statsTitleRange.Merge();
                statsTitleRange.Font.Bold = true;
                statsTitleRange.Font.Size = 14;
                statsTitleRange.HorizontalAlignment = Microsoft.Office.Interop.Excel.XlHAlign.xlHAlignCenter;

                statsWorksheet.Cells[2, 1] = periodInfo;
                Microsoft.Office.Interop.Excel.Range statsPeriodRange = statsWorksheet.Range[statsWorksheet.Cells[2, 1], statsWorksheet.Cells[2, 2]];
                statsPeriodRange.Merge();
                statsPeriodRange.Font.Size = 11;
                statsPeriodRange.Font.Italic = true;

                int statsRow = 4;

                int totalOrders = 0;
                int acceptedOrders = 0;
                int paidOrders = 0;
                int cancelledOrders = 0;
                decimal totalRevenue = 0;
                decimal totalPrepayment = 0;

                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (!row.IsNewRow)
                    {
                        totalOrders++;

                        string status = row.Cells["IdStatus"]?.Value?.ToString() ?? "";
                        decimal prepayment = 0;
                        decimal priceAll = 0;

                        if (row.Cells["Prepayment"].Value != null && decimal.TryParse(row.Cells["Prepayment"].Value.ToString(), out prepayment))
                        {
                            totalPrepayment += prepayment;
                        }

                        if (row.Cells["PriceAll"].Value != null && decimal.TryParse(row.Cells["PriceAll"].Value.ToString(), out priceAll))
                        {
                            // Не добавляем сразу
                        }

                        switch (status)
                        {
                            case "Принят":
                                acceptedOrders++;
                                totalRevenue += prepayment;
                                break;
                            case "Оплачен":
                                paidOrders++;
                                totalRevenue += priceAll;
                                break;
                            case "Отменен":
                                cancelledOrders++;
                                totalRevenue += prepayment;
                                break;
                        }
                    }
                }

                statsWorksheet.Cells[statsRow, 1] = "ОБЩАЯ СТАТИСТИКА:";
                statsWorksheet.Cells[statsRow, 1].Font.Bold = true;
                statsWorksheet.Cells[statsRow, 1].Font.Size = 12;
                statsRow += 2;

                statsWorksheet.Cells[statsRow, 1] = "Количество заказов:";
                statsWorksheet.Cells[statsRow, 1].Font.Bold = true;
                statsWorksheet.Cells[statsRow, 2] = totalOrders;
                statsRow++;

                statsWorksheet.Cells[statsRow, 1] = "Принято заказов:";
                statsWorksheet.Cells[statsRow, 2] = acceptedOrders;
                statsRow++;

                statsWorksheet.Cells[statsRow, 1] = "Оплачено заказов:";
                statsWorksheet.Cells[statsRow, 2] = paidOrders;
                statsRow++;

                statsWorksheet.Cells[statsRow, 1] = "Отменено заказов:";
                statsWorksheet.Cells[statsRow, 2] = cancelledOrders;
                statsRow += 2;

                statsWorksheet.Cells[statsRow, 1] = "ФИНАНСОВАЯ СТАТИСТИКА:";
                statsWorksheet.Cells[statsRow, 1].Font.Bold = true;
                statsWorksheet.Cells[statsRow, 1].Font.Size = 12;
                statsRow += 2;

                statsWorksheet.Cells[statsRow, 1] = "Общая выручка:";
                statsWorksheet.Cells[statsRow, 1].Font.Bold = true;
                statsWorksheet.Cells[statsRow, 2] = totalRevenue.ToString("N2") + " ₽";
                statsRow++;

                statsWorksheet.Cells[statsRow, 1] = "Сумма предоплат:";
                statsWorksheet.Cells[statsRow, 2] = totalPrepayment.ToString("N2") + " ₽";
                statsRow += 2;

                statsWorksheet.Cells[statsRow, 1] = "ИНФОРМАЦИЯ ОБ ОТЧЕТЕ:";
                statsWorksheet.Cells[statsRow, 1].Font.Bold = true;
                statsWorksheet.Cells[statsRow, 1].Font.Size = 12;
                statsRow += 2;

                statsWorksheet.Cells[statsRow, 1] = "Автор отчета:";
                statsWorksheet.Cells[statsRow, 1].Font.Bold = true;
                statsWorksheet.Cells[statsRow, 2] = Properties.Settings.Default.userName;
                statsRow++;

                statsWorksheet.Cells[statsRow, 1] = "Дата создания:";
                statsWorksheet.Cells[statsRow, 1].Font.Bold = true;
                statsWorksheet.Cells[statsRow, 2] = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                statsRow += 2;

                statsWorksheet.Columns.AutoFit();

                dataWorksheet.Activate();

                MessageBox.Show("Отчет в Excel составлен.",
                               "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте в Excel: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (workbook != null)
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(workbook);
                    workbook = null;
                }
                if (excelApp != null)
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);
                    excelApp = null;
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        //Получение полных данных для экспорта
        private DataTable GetFullDataForExport()
        {
            try
            {
                string query = BuildQuery();

                using (MySqlConnection con = new MySqlConnection(conString))
                {
                    con.Open();

                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                    {
                        DataTable table = new DataTable();
                        adapter.Fill(table);
                        return table;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении данных для экспорта: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
        }
    }
}