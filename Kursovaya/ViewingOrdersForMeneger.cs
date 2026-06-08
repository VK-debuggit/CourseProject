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
    public partial class ViewingOrdersForMeneger : Form
    {
        string conString = $"host={Properties.Settings.Default.host};uid={Properties.Settings.Default.uid};pwd={Properties.Settings.Default.pwd};database={Properties.Settings.Default.database};";
        private DateTime defaultStartDate;
        private DateTime defaultEndDate;
        private Timer searchTimer;
        private DataTable dataTable;
        private Timer inactivityTimer;
        private int inactivityTimeout;

        // переменные для сохранения данных формы
        private int savedPage = 1;
        private string savedSearchText = "";
        private DateTime savedStartDate;
        private DateTime savedEndDate;
        private int savedSelectedUserIndex = 0;
        private bool savedStatus1 = false;
        private bool savedStatus2 = false;
        private bool savedStatus3 = false;

        // Переменные для пагинации
        private int currentPage = 1;
        private int totalPages = 1;

        public ViewingOrdersForMeneger()
        {
            InitializeComponent();

            this.WindowState = FormWindowState.Maximized;

            searchTimer = new Timer();
            searchTimer.Interval = 500;
            searchTimer.Tick += SearchTimer_Tick;

            button1.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button2.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button3.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            dataGridView1.BackgroundColor = System.Drawing.Color.FromArgb(255, 221, 153);
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dataGridView1.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            textBox1.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            comboBox1.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);

            // Кнопка "Просмотр" изначально НЕДОСТУПНА
            button2.Enabled = false;

            SetupDateControls();
            SetupUserInfo();
            FillFilterUsers();

            LoadData();
        }

        // ========== МЕТОДЫ ДЛЯ СОХРАНЕНИЯ/ВОССТАНОВЛЕНИЯ СОСТОЯНИЯ ==========

        private void SaveFormState()
        {
            savedPage = this.currentPage;
            savedSearchText = this.textBox1.Text;
            savedStartDate = this.dateTimePicker1.Value;
            savedEndDate = this.dateTimePicker2.Value;
            savedSelectedUserIndex = this.comboBox1.SelectedIndex;
            savedStatus1 = this.checkBox1.Checked;
            savedStatus2 = this.checkBox2.Checked;
            savedStatus3 = this.checkBox3.Checked;
        }

        private void RestoreFormState()
        {
            this.currentPage = savedPage;
            this.textBox1.Text = savedSearchText;
            this.dateTimePicker1.Value = savedStartDate;
            this.dateTimePicker2.Value = savedEndDate;

            if (savedSelectedUserIndex >= 0 && savedSelectedUserIndex < comboBox1.Items.Count)
                this.comboBox1.SelectedIndex = savedSelectedUserIndex;

            this.checkBox1.Checked = savedStatus1;
            this.checkBox2.Checked = savedStatus2;
            this.checkBox3.Checked = savedStatus3;

            if (!string.IsNullOrEmpty(textBox1.Text))
            {
                LoadData();
            }

            Pagination();
            ShowPage(currentPage);
        }

        private void UpdateCurrentUserInfo()
        {
            string fullname = Properties.Settings.Default.userName;
            string formattedname = FormatFullName(fullname);
            label1.Text = formattedname;
            label2.Text = Properties.Settings.Default.userRole;
        }

        private void RefreshFormData()
        {
            LoadData();
        }

        // ========== МЕТОДЫ ФОРМАТИРОВАНИЯ (из AccountingForOrdersForAdmin) ==========

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

        private string FormatPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 4)
                return phoneNumber;

            string firstDigit = phoneNumber.Substring(0, 1);
            string lastFourDigits = phoneNumber.Substring(phoneNumber.Length - 4);
            string stars = new string('*', phoneNumber.Length - 4);

            return $"{firstDigit}{stars}{lastFourDigits}";
        }

        // ========== ПАГИНАЦИЯ ==========

        void Pagination()
        {
            // Удаляем старые элементы пагинации
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

            // Вычисляем количество страниц
            totalPages = dataGridView1.Rows.Count / 20;
            if (Convert.ToBoolean(dataGridView1.Rows.Count % 20)) totalPages += 1;
            if (totalPages == 0) totalPages = 1;

            // Позиционируем пагинацию под DataGridView
            int yPosition = dataGridView1.Bottom + 10;
            int leftMargin = 13;

            // Кнопка "Назад"
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

            // Ссылки на страницы
            int x = leftMargin + 35;
            int step = 20;

            for (int i = 0; i < totalPages; i++)
            {
                int pageNumber = i + 1;
                LinkLabel link = new LinkLabel();
                link.Text = Convert.ToString(pageNumber);
                link.Font = new Font("Microsoft Sans Serif", 14, FontStyle.Regular);
                link.Name = "page" + pageNumber;
                link.AutoSize = true;
                link.Location = new Point(x, yPosition);
                link.Click += new EventHandler(LinkLabel_Click);
                link.BackColor = Color.Transparent;

                if (pageNumber == currentPage)
                {
                    link.LinkBehavior = LinkBehavior.NeverUnderline;
                    link.ForeColor = Color.DarkRed;
                    link.Font = new Font(link.Font, FontStyle.Bold);
                }
                else
                {
                    link.LinkBehavior = LinkBehavior.AlwaysUnderline;
                    link.ForeColor = Color.Blue;
                }

                this.Controls.Add(link);
                x += step;
            }

            // Кнопка "Вперед"
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
            UpdateRowCount();
        }

        private void ShowPage(int pageNumber)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageNumber > totalPages) pageNumber = totalPages;

            currentPage = pageNumber;

            int sizePage = 20;
            int start = (pageNumber - 1) * sizePage;
            int stop = Math.Min(start + sizePage - 1, dataGridView1.Rows.Count - 1);

            for (int j = 0; j < dataGridView1.Rows.Count; ++j)
            {
                dataGridView1.Rows[j].Visible = (j >= start && j <= stop);
            }

            if (dataGridView1.Rows.Count > start)
            {
                dataGridView1.FirstDisplayedScrollingRowIndex = start;
            }

            UpdateRowCount();

            // После смены страницы проверяем, есть ли выделенные строки
            UpdateButtonState();
        }

        private void UpdateButtonState()
        {
            // Кнопка доступна только если есть выделенные строки
            button2.Enabled = dataGridView1.SelectedRows.Count > 0;
        }

        private void BtnPrev_Click(object sender, EventArgs e)
        {
            if (currentPage > 1)
            {
                ShowPage(currentPage - 1);
                Pagination();
            }
        }

        private void BtnNext_Click(object sender, EventArgs e)
        {
            if (currentPage < totalPages)
            {
                ShowPage(currentPage + 1);
                Pagination();
            }
        }

        private void LinkLabel_Click(object sender, EventArgs e)
        {
            LinkLabel l = sender as LinkLabel;
            if (l != null && int.TryParse(l.Text, out int pageNumber))
            {
                ShowPage(pageNumber);
                Pagination();
            }
        }

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

        private void ViewingOrdersForMeneger_Resize(object sender, EventArgs e)
        {
            int savedPage = currentPage;
            Pagination();
            currentPage = savedPage;
            ShowPage(currentPage);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Left || keyData == Keys.PageUp)
            {
                if (currentPage > 1)
                {
                    BtnPrev_Click(null, null);
                    return true;
                }
            }
            else if (keyData == Keys.Right || keyData == Keys.PageDown)
            {
                if (currentPage < totalPages)
                {
                    BtnNext_Click(null, null);
                    return true;
                }
            }
            else if (keyData == Keys.Home)
            {
                if (currentPage != 1)
                {
                    ShowPage(1);
                    Pagination();
                }
                return true;
            }
            else if (keyData == Keys.End)
            {
                if (currentPage != totalPages)
                {
                    ShowPage(totalPages);
                    Pagination();
                }
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ========== ТАЙМЕРЫ ==========

        private void SearchTimer_Tick(object sender, EventArgs e)
        {
            searchTimer.Stop();
            LoadData();
        }

        // ========== НАСТРОЙКА ФОРМЫ ==========

        private void SetupDateControls()
        {
            // Получаем границы из базы данных
            DateTime minOrderDate = GetMinOrderDate();  // Самая ранняя дата оформления
            DateTime maxEventDate = GetMaxEventDate();   // Самая поздняя дата проведения

            // Устанавливаем ОДИНАКОВЫЙ диапазон для обоих календарей
            dateTimePicker1.MinDate = minOrderDate;
            dateTimePicker1.MaxDate = maxEventDate;

            dateTimePicker2.MinDate = minOrderDate;
            dateTimePicker2.MaxDate = maxEventDate;

            // Значения по умолчанию - весь доступный диапазон
            defaultStartDate = minOrderDate;
            defaultEndDate = maxEventDate;

            dateTimePicker1.Value = defaultStartDate;
            dateTimePicker2.Value = defaultEndDate;
        }

        // Самая ранняя дата оформления заказа
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

        // Самая поздняя дата проведения мероприятия
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

        private void SetupUserInfo()
        {
            string fullname = Properties.Settings.Default.userName;
            string formattedname = FormatFullName(fullname);
            label1.Text = formattedname;
            label2.Text = Properties.Settings.Default.userRole;
        }

        void FillFilterUsers()
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(conString))
                {
                    con.Open();

                    using (MySqlCommand cmd = new MySqlCommand(@"SELECT FullName FROM CafeActivities.Users WHERE IdRole = 2 ORDER BY FullName;", con))
                    using (MySqlDataReader rdr = cmd.ExecuteReader())
                    {
                        comboBox1.Items.Clear();
                        comboBox1.Items.Add("Все сотрудники");

                        while (rdr.Read())
                        {
                            string fullName = rdr["FullName"].ToString();
                            string formattedName = FormatFullName(fullName);
                            comboBox1.Items.Add(formattedName);
                        }

                        comboBox1.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке списка сотрудников: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ========== РАБОТА С ДАННЫМИ ==========

        private void LoadData()
        {
            try
            {
                Cursor = Cursors.WaitCursor;

                string query = BuildQuery();

                using (MySqlConnection con = new MySqlConnection(conString))
                {
                    con.Open();

                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                    {
                        dataTable = new DataTable();
                        adapter.Fill(dataTable);
                    }
                }

                DisplayDataInDataGridView(dataTable);
                currentPage = 1;
                Pagination();

                // После загрузки новых данных кнопка недоступна
                button2.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

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

            // Фильтр по датам
            bool dateFilterApplied = (dateTimePicker1.Value != defaultStartDate) ||
                                   (dateTimePicker2.Value != defaultEndDate);

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
                    MessageBox.Show("Дата 'С' не может быть больше даты 'До'", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    dateTimePicker2.Value = dateTimePicker1.Value;
                    return BuildQuery();
                }
            }

            // Фильтр по сотруднику
            if (comboBox1.SelectedIndex != 0 && comboBox1.SelectedItem != null)
            {
                string selectedUser = comboBox1.SelectedItem.ToString();

                // Если выбран "Все сотрудники" (индекс 0) - пропускаем
                if (selectedUser != "Все сотрудники")
                {
                    // Нужно найти полное имя сотрудника в базе по отформатированному имени
                    string fullUserName = GetFullUserNameFromFormatted(selectedUser);
                    conditions.Add($"w.FullName = '{MySqlHelper.EscapeString(fullUserName)}'");
                }
            }

            // Фильтр по статусам
            List<string> statusConditions = new List<string>();
            if (checkBox1.Checked)
                statusConditions.Add($"s.Status = '{MySqlHelper.EscapeString(checkBox1.Text)}'");
            if (checkBox2.Checked)
                statusConditions.Add($"s.Status = '{MySqlHelper.EscapeString(checkBox2.Text)}'");
            if (checkBox3.Checked)
                statusConditions.Add($"s.Status = '{MySqlHelper.EscapeString(checkBox3.Text)}'");

            if (statusConditions.Count > 0)
                conditions.Add("(" + string.Join(" OR ", statusConditions) + ")");

            // Фильтр по номеру заказа
            string searchText = textBox1.Text.Trim();
            if (!string.IsNullOrEmpty(searchText))
            {
                conditions.Add($"p.NumberOrder LIKE '{MySqlHelper.EscapeString(searchText)}%'");
            }

            if (conditions.Count > 0)
            {
                query.Append(" WHERE ");
                query.Append(string.Join(" AND ", conditions));
            }

            query.Append(" ORDER BY p.NumberOrder ASC");

            return query.ToString();
        }

        // Вспомогательный метод для получения полного имени сотрудника из отформатированного
        private string GetFullUserNameFromFormatted(string formattedName)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(conString))
                {
                    con.Open();

                    // Ищем сотрудника, у которого FullName начинается с фамилии из formattedName
                    // formattedName имеет формат "Иванов И.И."
                    string[] parts = formattedName.Split(' ');
                    if (parts.Length > 0)
                    {
                        string lastName = parts[0];
                        string query = "SELECT FullName FROM CafeActivities.Users WHERE FullName LIKE @lastNamePattern AND IdRole = 2";

                        using (MySqlCommand cmd = new MySqlCommand(query, con))
                        {
                            cmd.Parameters.AddWithValue("@lastNamePattern", lastName + "%");

                            object result = cmd.ExecuteScalar();
                            if (result != null)
                            {
                                return result.ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения полного имени: {ex.Message}");
            }

            return formattedName; // Если не нашли, возвращаем как есть
        }

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
                dataGridView1.Columns["IdEvent"].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                dataGridView1.Columns.Add("IdUser", "ФИО сотрудника");
                dataGridView1.Columns.Add("Price", "Цена");
                dataGridView1.Columns["Price"].Visible = false;
                dataGridView1.Columns.Add("DiscountAmount", "Сумма скидки");
                dataGridView1.Columns["DiscountAmount"].Visible = false;
                dataGridView1.Columns.Add("PriceAll", "Полная стоимость");
                dataGridView1.Columns.Add("Prepayment", "Предоплата");
            }

            foreach (DataRow row in tableToDisplay.Rows)
            {
                int rowIndex = dataGridView1.Rows.Add(
                    row["NumberOrder"],
                    FormatFullName(row["IdClient"].ToString()),
                    FormatPhoneNumber(row["NumberPhoneClient"].ToString()),
                    Convert.ToDateTime(row["DateOfConclusion"]).ToString("dd.MM.yyyy"),
                    Convert.ToDateTime(row["DateEvent"]).ToString("dd.MM.yyyy"),
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
                DataGridViewRow dataGridRow = dataGridView1.Rows[rowIndex];

                switch (status)
                {
                    case "Принят":
                        foreach (DataGridViewCell cell in dataGridRow.Cells)
                            cell.Style.BackColor = Color.FromArgb(255, 255, 102);
                        break;
                    case "Оплачен":
                        foreach (DataGridViewCell cell in dataGridRow.Cells)
                            cell.Style.BackColor = Color.FromArgb(170, 255, 170);
                        break;
                    case "Отменен":
                        foreach (DataGridViewCell cell in dataGridRow.Cells)
                            cell.Style.BackColor = Color.FromArgb(255, 182, 182);
                        break;
                }
            }

            // Сбрасываем выделение после загрузки данных
            dataGridView1.ClearSelection();
        }

        private void UpdateRowCount()
        {
            // Получаем общее количество строк после всех фильтров
            int totalFilteredCount = dataGridView1.Rows.Count;

            // Подсчитываем видимые строки на текущей странице
            int visibleCount = 0;
            int startIndex = (currentPage - 1) * 20;
            int endIndex = Math.Min(startIndex + 20, totalFilteredCount);

            for (int i = startIndex; i < endIndex; i++)
            {
                visibleCount++;
            }

            // Обновляем label
            if (totalFilteredCount == 0)
            {
                label4.Text = "0 из 0";
            }
            else
            {
                label4.Text = $"{visibleCount} из {totalFilteredCount}";
            }
        }

        // ========== СОБЫТИЯ КНОПОК ==========

        private bool allowClose = false;

        private void button1_Click(object sender, EventArgs e)
        {
            allowClose = true;
            this.Visible = false;
            MainFormMeneger mainFormMeneger = new MainFormMeneger();
            mainFormMeneger.ShowDialog();
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите заказ для просмотра", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataGridViewRow selectedRow = dataGridView1.SelectedRows[0];
            string orderId = selectedRow.Cells["NumberOrder"].Value?.ToString();

            if (string.IsNullOrEmpty(orderId))
            {
                MessageBox.Show("Не удалось получить номер заказа", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            allowClose = true;
            this.Visible = false;
            ViewingAnOrder viewingAnOrder = new ViewingAnOrder(orderId);
            viewingAnOrder.ShowDialog();
            this.Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                textBox1.Text = "";
                dateTimePicker1.Value = defaultStartDate;
                dateTimePicker2.Value = defaultEndDate;
                comboBox1.SelectedIndex = 0;
                checkBox1.Checked = false;
                checkBox2.Checked = false;
                checkBox3.Checked = false;

                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сбросе фильтров: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ========== СОБЫТИЯ ПОИСКА И ФИЛЬТРАЦИИ ==========

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

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadData();
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            // Если дата "С" стала больше даты "До" - корректируем дату "До"
            if (dateTimePicker1.Value > dateTimePicker2.Value)
            {
                dateTimePicker2.Value = dateTimePicker1.Value;
            }
            LoadData();
        }

        private void dateTimePicker2_ValueChanged(object sender, EventArgs e)
        {
            // Если дата "До" стала меньше даты "С" - корректируем дату "С"
            if (dateTimePicker2.Value < dateTimePicker1.Value)
            {
                dateTimePicker1.Value = dateTimePicker2.Value;
            }
            LoadData();
        }

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

        // ========== СОБЫТИЯ DataGridView ==========

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // При клике на ячейку проверяем, есть ли выделенные строки
            UpdateButtonState();
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            // При изменении выделения обновляем состояние кнопки
            UpdateButtonState();
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
                button2.PerformClick();
        }

        // ========== СОБЫТИЯ ФОРМЫ ==========

        private void ViewingOrdersForMeneger_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.ApplicationExitCall)
                return;

            if (!allowClose)
                e.Cancel = true;
        }
    }
}