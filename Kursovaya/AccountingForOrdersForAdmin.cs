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

        public AccountingForOrdersForAdmin()
        {
            InitializeComponent();
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

            button1.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button2.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
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

        // Создаем переменные для хранения текущей страницы и общего количества страниц
        private int currentPage = 1;
        private int totalPages = 1;

        // создание пагинации        
        void Pagination()
        {
            // удаляем LinkLabel служащий для пагинации
            // каждый раз будем создавать новую пагинацию
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

            // узнаём сколько страниц будет
            totalPages = dataGridView1.Rows.Count / 20; // на каждой странице по 20 записей
            if (Convert.ToBoolean(dataGridView1.Rows.Count % 20)) totalPages += 1; // ситуация когда при делении получаем не целое число

            // Если нет данных, устанавливаем 1 страницу
            if (totalPages == 0) totalPages = 1;

            // Создаем кнопку "Назад"
            Button btnPrev = new Button();
            btnPrev.Name = "btnPrev";
            btnPrev.Text = "◀";
            btnPrev.Font = new Font("Microsoft Sans Serif", 8, FontStyle.Bold);
            btnPrev.Size = new Size(30, 25);
            btnPrev.Location = new Point(13, 460);
            btnPrev.Click += new EventHandler(BtnPrev_Click);
            btnPrev.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            btnPrev.FlatStyle = FlatStyle.Flat;
            btnPrev.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnPrev);

            // Создаем ссылки на страницы
            int x = 48; // Начинаем после кнопки "Назад"
            int y = 460;
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
                ll[i].Location = new Point(x, y);
                ll[i].Click += new EventHandler(LinkLabel_Click);
                ll[i].BackColor = Color.Transparent;

                // Выделяем текущую страницу - убираем подчеркивание и меняем цвет
                if (pageNumber == currentPage)
                {
                    ll[i].LinkBehavior = LinkBehavior.NeverUnderline;
                    ll[i].ForeColor = Color.DarkRed; // Меняем цвет текущей страницы
                    ll[i].Font = new Font(ll[i].Font, FontStyle.Bold);
                }
                else
                {
                    ll[i].LinkBehavior = LinkBehavior.AlwaysUnderline;
                    ll[i].ForeColor = Color.Blue; // Цвет для остальных страниц
                    ll[i].Font = new Font(ll[i].Font, FontStyle.Regular);
                }

                this.Controls.Add(ll[i]);
                x += step;
            }

            // Создаем кнопку "Вперед"
            Button btnNext = new Button();
            btnNext.Name = "btnNext";
            btnNext.Text = "▶";
            btnNext.Font = new Font("Microsoft Sans Serif", 8, FontStyle.Bold);
            btnNext.Size = new Size(30, 25);
            btnNext.Location = new Point(x, 457);
            btnNext.Click += new EventHandler(BtnNext_Click);
            btnNext.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            btnNext.FlatStyle = FlatStyle.Flat;
            btnNext.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnNext);

            // Обновляем отображение данных
            ShowPage(currentPage);

            // Обновляем состояние кнопок
            UpdateNavigationButtons();
        }

        // Метод для отображения конкретной страницы
        private void ShowPage(int pageNumber)
        {
            // Проверяем корректность номера страницы
            if (pageNumber < 1) pageNumber = 1;
            if (pageNumber > totalPages) pageNumber = totalPages;

            currentPage = pageNumber;

            // Скрываем/показываем строки в зависимости от страницы
            int countRows = dataGridView1.Rows.Count;
            int sizePage = 20;
            int start = (pageNumber - 1) * sizePage;
            int stop = Math.Min(start + sizePage - 1, countRows - 1);

            for (int j = 0; j < countRows; ++j)
            {
                dataGridView1.Rows[j].Visible = (j >= start && j <= stop);
            }

            // Прокручиваем таблицу к началу страницы
            if (dataGridView1.Rows.Count > start)
            {
                dataGridView1.FirstDisplayedScrollingRowIndex = start;
            }
        }

        // Обработчик для кнопки "Назад"
        private void BtnPrev_Click(object sender, EventArgs e)
        {
            if (currentPage > 1)
            {
                ShowPage(currentPage - 1);
                Pagination(); // Пересоздаем пагинацию с обновленным выделением
                ResetInactivityTimer(sender, e);
            }
        }

        // Обработчик для кнопки "Вперед"
        private void BtnNext_Click(object sender, EventArgs e)
        {
            if (currentPage < totalPages)
            {
                ShowPage(currentPage + 1);
                Pagination(); // Пересоздаем пагинацию с обновленным выделением
                ResetInactivityTimer(sender, e);
            }
        }

        // Выбор страницы пагинации по клику на номер
        private void LinkLabel_Click(object sender, EventArgs e)
        {
            LinkLabel l = sender as LinkLabel;
            if (l != null && int.TryParse(l.Text, out int pageNumber))
            {
                ShowPage(pageNumber);
                Pagination(); // Пересоздаем пагинацию с обновленным выделением
                ResetInactivityTimer(sender, e);
            }
        }

        // Метод для обновления состояния кнопок навигации
        private void UpdateNavigationButtons()
        {
            // Находим кнопки на форме
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

        // Также можно добавить обработку клавиатуры для навигации
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Обработка стрелок для пагинации
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
                // Переход на первую страницу
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
                // Переход на последнюю страницу
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

        private string FormatPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 4)
                return phoneNumber;

            char firstDigit = phoneNumber[0];
            string lastTwoDigits = phoneNumber.Substring(phoneNumber.Length - 2);

            int starsCount = phoneNumber.Length - 3;
            string stars = new string('*', starsCount);

            return $"{firstDigit}{stars}{lastTwoDigits}";
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
            LEFT JOIN CafeActivities.Users w ON p.IdUser = w.IDuser;";

            try
            {
                using (MySqlConnection con = new MySqlConnection(conString))
                {
                    con.Open();

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

                            int rowIndex = dataGridView1.Rows.Add(
                                rdr["NumberOrder"].ToString(),
                                rdr["IdClient"].ToString(),
                                FormatPhoneNumber(rdr["NumberPhoneClient"].ToString()),
                                Convert.ToDateTime(rdr["DateOfConclusion"]).ToString("dd.MM.yyyy"),
                                Convert.ToDateTime(rdr["DateEvent"]).ToString("dd.MM.yyyy"),
                                rdr["IdSchedule"].ToString(),
                                statusName,
                                statusId,
                                rdr["IdEvent"].ToString(),
                                rdr["IdUser"].ToString(),
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

            // После загрузки данных обновляем пагинацию
            currentPage = 1; // Сбрасываем на первую страницу
            Pagination();
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
                // Получаем данные выбранной строки
                var selectedRow = dataGridView1.Rows[e.RowIndex];
                string orderNumber = selectedRow.Cells["NumberOrder"].Value.ToString();
                string currentStatus = selectedRow.Cells["Status"].Value.ToString();
                string statusId = selectedRow.Cells["IDstatus"].Value.ToString();

                // Сохраняем начальный статус
                initialStatus = currentStatus;
                selectedOrderId = int.Parse(orderNumber);
                isStatusChanged = false;

                // Устанавливаем текущий статус в ComboBox
                comboBox1.SelectedItem = currentStatus;
                comboBox1.Enabled = true;

                // Деактивируем кнопку обновления (статус еще не изменен)
                button2.Enabled = false;

                // Показываем информацию о выбранном заказе
                label5.Text = $"Выбран заказ: №{orderNumber}. Текущий статус: '{currentStatus}'";
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem != null)
            {
                string selectedStatus = comboBox1.SelectedItem.ToString();

                // Проверяем, изменился ли статус по сравнению с начальным
                isStatusChanged = (selectedStatus != initialStatus);

                // Активируем кнопку только если статус изменился
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

            // Запрашиваем подтверждение выбора смены статуса
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
                            // Обновляем DataGridView
                            UpdateDataGridViewStatus(orderNumber, newStatusName, newStatusId);

                            // Выводим сообщение об успешной смене статуса
                            MessageBox.Show(
                                $"Статус заказа №{orderNumber} успешно изменен на '{newStatusName}'",
                                "Статус обновлен",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information
                            );

                            // Сбрасываем состояние
                            ResetStatusChangeControls();

                            // Обновляем начальный статус
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
            // Если выбор снят со строки, сбрасываем элементы управления
            if (dataGridView1.SelectedRows.Count == 0)
            {
                ResetStatusChangeControls();
            }
        }
    }
}