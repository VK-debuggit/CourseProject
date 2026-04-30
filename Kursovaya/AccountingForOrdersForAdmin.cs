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
    public partial class AccountingForOrdersForAdmin : Form
    {
        string conString = $"host={Properties.Settings.Default.host};uid={Properties.Settings.Default.uid};pwd={Properties.Settings.Default.pwd};database={Properties.Settings.Default.database};";
        private Dictionary<string, string> statusMap = new Dictionary<string, string>();
        private int rowCount = 0;
        private int selectedOrderId = -1;
        private string initialStatus = "";
        private bool isStatusChanged = false;
        private Timer inactivityTimer;
        private int inactivityTimeout;
        private Timer searchTimer;
        private DataTable dataTable; // Хранилище всех данных для поиска

        // Переменные для пагинации
        private int currentPage = 1;
        private int totalPages = 1;

        public AccountingForOrdersForAdmin()
        {
            InitializeComponent();

            // Развернуть форму на весь экран
            this.WindowState = FormWindowState.Maximized;

            FillDataGridView();
            FillStatusComboBox();

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
            this.Resize += AccountingForOrdersForAdmin_Resize;

            searchTimer = new Timer();
            searchTimer.Interval = 500;
            searchTimer.Tick += SearchTimer_Tick;

            button1.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button2.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            comboBox1.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            textBox1.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            dataGridView1.BackgroundColor = System.Drawing.Color.FromArgb(255, 221, 153);
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dataGridView1.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            comboBox1.Enabled = false;
            button2.Enabled = false;
            label5.Text = "Выберите заказ для изменения статуса";

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
            dataGridView1.BackgroundColor = System.Drawing.Color.FromArgb(255, 221, 153);
        }

        // Метод для форматирования ФИО в формат "Фамилия И.О."
        private string FormatFullName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                return "";

            string[] parts = fullName.Trim().Split(' ');

            if (parts.Length >= 3)
            {
                string lastName = parts[0];
                string firstName = parts[1].Substring(0, 1);
                string middleName = parts[2].Substring(0, 1);
                return $"{lastName} {firstName}.{middleName}.";
            }
            else if (parts.Length == 2)
            {
                string lastName = parts[0];
                string firstName = parts[1].Substring(0, 1);
                return $"{lastName} {firstName}.";
            }

            return fullName;
        }

        // Метод для форматирования номера телефона (оставляем последние 4 цифры)
        private string FormatPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 4)
                return phoneNumber;

            // Оставляем первую и последние 4 цифры
            string firstDigit = phoneNumber.Substring(0, 1);
            string lastFourDigits = phoneNumber.Substring(phoneNumber.Length - 4);
            string stars = new string('*', phoneNumber.Length - 4);

            return $"{firstDigit}{stars}{lastFourDigits}";
        }

        private void SearchTimer_Tick(object sender, EventArgs e)
        {
            searchTimer.Stop();
            FilterDataGridView();
        }

        private void FilterDataGridView()
        {
            if (dataTable == null) return;

            string searchText = textBox1.Text.Trim();
            DataView dv = new DataView(dataTable);

            if (!string.IsNullOrEmpty(searchText))
            {
                dv.RowFilter = $"CONVERT(NumberOrder, 'System.String') LIKE '{searchText}%'";
            }

            // Отображаем отфильтрованные данные в DataGridView
            dataGridView1.Rows.Clear();

            foreach (DataRowView rowView in dv)
            {
                DataRow row = rowView.Row;
                string statusId = row["IDstatus"].ToString();
                string statusName = GetStatusNameById(statusId);

                dataGridView1.Rows.Add(
                    row["NumberOrder"].ToString(),
                    FormatFullName(row["IdClient"].ToString()), // Форматируем ФИО клиента
                    FormatPhoneNumber(row["NumberPhoneClient"].ToString()), // Форматируем телефон
                    Convert.ToDateTime(row["DateOfConclusion"]).ToString("dd.MM.yyyy"),
                    Convert.ToDateTime(row["DateEvent"]).ToString("dd.MM.yyyy"),
                    row["IdSchedule"].ToString(),
                    statusName,
                    statusId,
                    row["IdEvent"].ToString(),
                    FormatFullName(row["IdUser"].ToString()), // Форматируем ФИО сотрудника
                    row["Price"].ToString(),
                    row["DiscountAmount"].ToString(),
                    row["PriceAll"].ToString(),
                    row["Prepayment"].ToString()
                );
            }

            rowCount = dataGridView1.Rows.Count;
            label4.Text = rowCount.ToString();

            // Обновляем пагинацию
            currentPage = 1;
            Pagination();
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

        private void button1_Click(object sender, EventArgs e)
        {
            inactivityTimer.Stop();
            allowClose = true;
            this.Visible = false;
            MainFormAdmin mainFormAdmin = new MainFormAdmin();
            mainFormAdmin.ShowDialog();
            this.Close();
        }

        private void AccountingForOrdersForAdmin_FormClosing(object sender, FormClosingEventArgs e)
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
            FillStatusMap();

            string conStr = @"SELECT  
                    p.NumberOrder,
                    c.Name as IdClient,
                    p.NumberPhoneClient,
                    p.DateOfConclusion,
                    p.DateEvent,
                    CONCAT(DATE_FORMAT(r.StartTime, '%H:%i'), ' - ', DATE_FORMAT(r.EndTime, '%H:%i')) as IdSchedule,
                    s.IDstatus as IDstatus,
                    s.Status as StatusName,
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
            LEFT JOIN CafeActivities.Users w ON p.IdUser = w.IDuser
            ORDER BY p.NumberOrder ASC;";

            try
            {
                using (MySqlConnection con = new MySqlConnection(conString))
                {
                    con.Open();

                    // Заполняем DataTable для поиска
                    using (MySqlCommand cmd = new MySqlCommand(conStr, con))
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                    {
                        dataTable = new DataTable();
                        adapter.Fill(dataTable);
                    }

                    using (MySqlCommand cmd = new MySqlCommand(conStr, con))
                    using (MySqlDataReader rdr = cmd.ExecuteReader())
                    {
                        dataGridView1.Rows.Clear();
                        dataGridView1.Columns.Clear();

                        dataGridView1.Columns.Add("NumberOrder", "№ заказа");
                        dataGridView1.Columns.Add("IdClient", "ФИО клиента");
                        dataGridView1.Columns.Add("NumberPhoneClient", "Телефон");
                        dataGridView1.Columns.Add("DateOfConclusion", "Дата оформления");
                        dataGridView1.Columns.Add("DateEvent", "Дата проведения");
                        dataGridView1.Columns.Add("IdSchedule", "Время проведения");
                        dataGridView1.Columns.Add("Status", "Статус");
                        dataGridView1.Columns.Add("IDstatus", "ID статуса");
                        dataGridView1.Columns["IDstatus"].Visible = false;
                        dataGridView1.Columns.Add("IdEvent", "Мероприятие");
                        dataGridView1.Columns["IdEvent"].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                        dataGridView1.Columns.Add("IdUser", "Сотрудник");
                        dataGridView1.Columns.Add("Price", "Цена");
                        dataGridView1.Columns["Price"].Visible = false;
                        dataGridView1.Columns.Add("DiscountAmount", "Сумма скидки");
                        dataGridView1.Columns["DiscountAmount"].Visible = false;
                        dataGridView1.Columns.Add("PriceAll", "Полная стоимость");
                        dataGridView1.Columns.Add("Prepayment", "Предоплата");

                        int rowCount = 0;
                        while (rdr.Read())
                        {
                            string statusId = rdr["IDstatus"].ToString();
                            string statusName = GetStatusNameById(statusId);

                            dataGridView1.Rows.Add(
                                rdr["NumberOrder"].ToString(),
                                FormatFullName(rdr["IdClient"].ToString()), // Форматируем ФИО клиента
                                FormatPhoneNumber(rdr["NumberPhoneClient"].ToString()), // Форматируем телефон
                                Convert.ToDateTime(rdr["DateOfConclusion"]).ToString("dd.MM.yyyy"),
                                Convert.ToDateTime(rdr["DateEvent"]).ToString("dd.MM.yyyy"),
                                rdr["IdSchedule"].ToString(),
                                statusName,
                                statusId,
                                rdr["IdEvent"].ToString(),
                                FormatFullName(rdr["IdUser"].ToString()), // Форматируем ФИО сотрудника
                                rdr["Price"].ToString(),
                                rdr["DiscountAmount"].ToString(),
                                rdr["PriceAll"].ToString(),
                                rdr["Prepayment"].ToString()
                            );

                            rowCount++;
                        }

                        label4.Text = rowCount.ToString();

                        if (rowCount == 0)
                        {
                            MessageBox.Show("Данные не найдены", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine("Error details: " + ex.ToString());
            }

            currentPage = 1;
            Pagination();
        }

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
        }

        private void BtnPrev_Click(object sender, EventArgs e)
        {
            if (currentPage > 1)
            {
                ShowPage(currentPage - 1);
                Pagination();
                ResetInactivityTimer(sender, e);
            }
        }

        private void BtnNext_Click(object sender, EventArgs e)
        {
            if (currentPage < totalPages)
            {
                ShowPage(currentPage + 1);
                Pagination();
                ResetInactivityTimer(sender, e);
            }
        }

        private void LinkLabel_Click(object sender, EventArgs e)
        {
            LinkLabel l = sender as LinkLabel;
            if (l != null && int.TryParse(l.Text, out int pageNumber))
            {
                ShowPage(pageNumber);
                Pagination();
                ResetInactivityTimer(sender, e);
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

        private void AccountingForOrdersForAdmin_Resize(object sender, EventArgs e)
        {
            // Обновляем позицию пагинации при изменении размера формы
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
                    ResetInactivityTimer(null, null);
                }
                return true;
            }
            else if (keyData == Keys.End)
            {
                if (currentPage != totalPages)
                {
                    ShowPage(totalPages);
                    Pagination();
                    ResetInactivityTimer(null, null);
                }
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void FillStatusComboBox()
        {
            comboBox1.Items.Clear();
            foreach (var status in statusMap.Values)
            {
                comboBox1.Items.Add(status);
            }
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var selectedRow = dataGridView1.Rows[e.RowIndex];
                string orderNumber = selectedRow.Cells["NumberOrder"].Value.ToString();
                string currentStatus = selectedRow.Cells["Status"].Value.ToString();
                string statusId = selectedRow.Cells["IDstatus"].Value.ToString();

                initialStatus = currentStatus;
                selectedOrderId = int.Parse(orderNumber);
                isStatusChanged = false;

                comboBox1.SelectedItem = currentStatus;
                comboBox1.Enabled = true;
                button2.Enabled = false;

                label5.Text = $"Выбран заказ: №{orderNumber}. Текущий статус: '{currentStatus}'";
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem != null)
            {
                string selectedStatus = comboBox1.SelectedItem.ToString();
                isStatusChanged = (selectedStatus != initialStatus);
                button2.Enabled = isStatusChanged;

                if (isStatusChanged)
                {
                    label5.Text = $"Выбран заказ: №{selectedOrderId}. Статус изменен с '{initialStatus}' на '{selectedStatus}'";
                }
                else
                {
                    label5.Text = $"Выбран заказ: №{selectedOrderId}. Статус не изменен";
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem == null || !isStatusChanged)
            {
                MessageBox.Show("Выберите новый статус для заказа", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string newStatusName = comboBox1.SelectedItem.ToString();

            var result = MessageBox.Show(
                $"Подтвердите смену статуса заказа №{selectedOrderId}\n" +
                $"С '{initialStatus}' на '{newStatusName}'",
                "Подтверждение изменения статуса",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                UpdateStatusInDatabase(selectedOrderId, newStatusName);
            }
        }

        private void UpdateStatusInDatabase(int orderNumber, string newStatusName)
        {
            try
            {
                string newStatusId = GetStatusIdByName(newStatusName);

                using (MySqlConnection con = new MySqlConnection(conString))
                {
                    con.Open();

                    string updateQuery = "UPDATE CafeActivities.Orders SET IdStatus = @StatusId WHERE NumberOrder = @OrderNumber";

                    using (MySqlCommand cmd = new MySqlCommand(updateQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@StatusId", newStatusId);
                        cmd.Parameters.AddWithValue("@OrderNumber", orderNumber);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            UpdateDataGridViewStatus(orderNumber, newStatusName, newStatusId);

                            // Обновляем также DataTable для поиска
                            UpdateDataTableStatus(orderNumber, newStatusName, newStatusId);

                            MessageBox.Show(
                                $"Статус заказа №{orderNumber} успешно изменен на '{newStatusName}'",
                                "Статус обновлен",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information
                            );

                            ResetStatusChangeControls();
                            initialStatus = newStatusName;
                        }
                        else
                        {
                            MessageBox.Show("Не удалось обновить статус", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении статуса: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateDataGridViewStatus(int orderNumber, string newStatusName, string newStatusId)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells["NumberOrder"].Value?.ToString() == orderNumber.ToString())
                {
                    row.Cells["Status"].Value = newStatusName;
                    row.Cells["IDstatus"].Value = newStatusId;
                    break;
                }
            }
        }

        private void UpdateDataTableStatus(int orderNumber, string newStatusName, string newStatusId)
        {
            if (dataTable != null)
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    if (row["NumberOrder"].ToString() == orderNumber.ToString())
                    {
                        row["IDstatus"] = newStatusId;
                        break;
                    }
                }
            }
        }

        private void FillStatusMap()
        {
            try
            {
                string query = "SELECT IDstatus, Status FROM CafeActivities.Status";
                using (MySqlConnection con = new MySqlConnection(conString))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    using (MySqlDataReader rdr = cmd.ExecuteReader())
                    {
                        statusMap.Clear();
                        while (rdr.Read())
                        {
                            string statusId = rdr["IDstatus"].ToString();
                            string statusName = rdr["Status"].ToString();
                            statusMap[statusId] = statusName;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке статусов: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetStatusNameById(string statusId)
        {
            return statusMap.ContainsKey(statusId) ? statusMap[statusId] : "Неизвестно";
        }

        private string GetStatusIdByName(string statusName)
        {
            return statusMap.FirstOrDefault(x => x.Value == statusName).Key;
        }

        private void ResetStatusChangeControls()
        {
            button2.Enabled = false;
            comboBox1.SelectedIndex = -1;
            comboBox1.Enabled = false;
            label5.Text = "Выберите заказ для изменения статуса";
            selectedOrderId = -1;
            initialStatus = "";
            isStatusChanged = false;
            dataGridView1.ClearSelection();
        }

        private void ClearAllFields()
        {
            button2.Enabled = false;
            comboBox1.SelectedIndex = -1;
            comboBox1.Enabled = false;
            label5.Text = "Выберите заказ для изменения статуса";
            selectedOrderId = -1;
            initialStatus = "";
            isStatusChanged = false;
            dataGridView1.ClearSelection();
        }

        private void AccountingForOrdersForAdmin_Load(object sender, EventArgs e)
        {
            ClearAllFields();
            Pagination();
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                ResetStatusChangeControls();
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBox1.Text))
            {
                // Разрешаем ввод только цифр
                string digitsOnly = new string(textBox1.Text.Where(char.IsDigit).ToArray());
                if (textBox1.Text != digitsOnly)
                {
                    textBox1.Text = digitsOnly;
                    textBox1.SelectionStart = textBox1.Text.Length;
                }
            }

            // Перезапускаем таймер при каждом изменении текста
            searchTimer.Stop();
            searchTimer.Start();
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }
    }
}