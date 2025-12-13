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
        private Timer searchTimer; // Таймер для задержки поиска
        private DataTable dataTable; // Храним данные в DataTable для фильтрации
        private DataTable allDataTable;
        private Timer inactivityTimer;
        private int inactivityTimeout;

        public ViewingOrdersForMeneger()
        {
            InitializeComponent();

            // Инициализация таймера для поиска с задержкой
            searchTimer = new Timer();
            searchTimer.Interval = 500; // 500 мс задержка
            searchTimer.Tick += SearchTimer_Tick;

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

            // Настройка цветов
            button1.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button2.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button3.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            dataGridView1.BackgroundColor = System.Drawing.Color.FromArgb(255, 221, 153);
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dataGridView1.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            textBox1.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            comboBox1.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);

            button2.Enabled = false; // Кнопка просмотра заказа изначально неактивна

            // Настройка дат
            SetupDateControls();

            // Настройка пользователя
            SetupUserInfo();

            // Заполнение фильтров
            FillFilterUsers();
        }

        // создание пагинации        
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
            btnPrev.Location = new Point(13, 435);
            btnPrev.Click += new EventHandler(BtnPrev_Click);
            btnPrev.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            btnPrev.FlatStyle = FlatStyle.Flat;
            btnPrev.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnPrev);

            // Создаем ссылки на страницы
            int x = 48; // Начинаем после кнопки "Назад"
            int y = 435;
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
            btnNext.Location = new Point(x, 435);
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

            if (dataTable != null)
            {
                UpdateRowCount(0, dataTable.Rows.Count);
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

        private void SetupDateControls()
        {
            // Устанавливаем ограничения для DateTimePicker
            DateTime minDate = DateTime.Today.AddMonths(-6);
            DateTime maxDate = DateTime.Today.AddMonths(6);

            dateTimePicker1.MinDate = minDate;
            dateTimePicker1.MaxDate = DateTime.Today;
            dateTimePicker2.MinDate = DateTime.Today;
            dateTimePicker2.MaxDate = maxDate;

            // Устанавливаем значения по умолчанию
            defaultStartDate = DateTime.Now.AddMonths(-6);
            defaultEndDate = DateTime.Now.AddMonths(6);

            // Корректируем значения по умолчанию, если они выходят за границы
            if (defaultStartDate < dateTimePicker1.MinDate)
                defaultStartDate = dateTimePicker1.MinDate;
            if (defaultStartDate > dateTimePicker1.MaxDate)
                defaultStartDate = dateTimePicker1.MaxDate;

            if (defaultEndDate < dateTimePicker2.MinDate)
                defaultEndDate = dateTimePicker2.MinDate;
            if (defaultEndDate > dateTimePicker2.MaxDate)
                defaultEndDate = dateTimePicker2.MaxDate;

            dateTimePicker1.Value = defaultStartDate;
            dateTimePicker2.Value = defaultEndDate;
        }

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
            }

            if (comboBox1.SelectedIndex != 0 && comboBox1.SelectedItem != null)
            {
                conditions.Add($"w.FullName = '{MySqlHelper.EscapeString(comboBox1.SelectedItem.ToString())}'");
            }

            List<string> statusConditions = new List<string>();

            if (checkBox1.Checked)
            {
                statusConditions.Add($"s.Status = '{MySqlHelper.EscapeString(checkBox1.Text)}'");
            }

            if (checkBox2.Checked)
            {
                statusConditions.Add($"s.Status = '{MySqlHelper.EscapeString(checkBox2.Text)}'");
            }

            if (checkBox3.Checked)
            {
                statusConditions.Add($"s.Status = '{MySqlHelper.EscapeString(checkBox3.Text)}'");
            }

            if (statusConditions.Count > 0)
            {
                conditions.Add("(" + string.Join(" OR ", statusConditions) + ")");
            }

            if (conditions.Count > 0)
            {
                query.Append(" WHERE ");
                query.Append(string.Join(" AND ", conditions));
            }

            return query.ToString();
        }

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
                        UpdateRowCount(dataTable.Rows.Count, totalCount);
                    }

                    DisplayDataInDataGridView(dataTable);
                }
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

            // Фильтр по датам (если изменены относительно значений по умолчанию)
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
                    return BuildQuery(); // Перестраиваем запрос с корректными датами
                }
            }

            // Фильтр по сотруднику
            if (comboBox1.SelectedIndex != 0 && comboBox1.SelectedItem != null)
            {
                conditions.Add($"w.FullName = '{MySqlHelper.EscapeString(comboBox1.SelectedItem.ToString())}'");
            }

            // Фильтр по статусам
            List<string> statusConditions = new List<string>();

            if (checkBox1.Checked)
            {
                statusConditions.Add($"s.Status = '{MySqlHelper.EscapeString(checkBox1.Text)}'");
            }

            if (checkBox2.Checked)
            {
                statusConditions.Add($"s.Status = '{MySqlHelper.EscapeString(checkBox2.Text)}'");
            }

            if (checkBox3.Checked)
            {
                statusConditions.Add($"s.Status = '{MySqlHelper.EscapeString(checkBox3.Text)}'");
            }

            if (statusConditions.Count > 0)
            {
                conditions.Add("(" + string.Join(" OR ", statusConditions) + ")");
            }

            // Добавляем условия WHERE
            if (conditions.Count > 0)
            {
                query.Append(" WHERE ");
                query.Append(string.Join(" AND ", conditions));
            }

            // Сортировка
            query.Append(" ORDER BY p.DateEvent ASC, p.NumberOrder DESC");

            return query.ToString();
        }

        private void DisplayDataInDataGridView(DataTable tableToDisplay = null)
        {
            if (tableToDisplay == null)
            {
                if (dataTable == null) return;
                tableToDisplay = dataTable;
            }

            dataGridView1.Rows.Clear();

            // Если колонки еще не созданы - создаем их
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
            {
                dv.RowFilter = $"CONVERT(NumberOrder, 'System.String') LIKE '%{searchText}%'";
            }

            // Заполняем DataGridView отфильтрованными данными
            foreach (DataRowView rowView in dv)
            {
                DataRow row = rowView.Row;
                int rowIndex = dataGridView1.Rows.Add(
                    row["NumberOrder"],
                    row["IdClient"],
                    FormatPhoneNumber(row["NumberPhoneClient"].ToString()),
                    row["DateOfConclusion"],
                    row["DateEvent"],
                    row["IdSchedule"],
                    row["IdStatus"],
                    row["IdEvent"],
                    row["IdUser"],
                    row["Price"],
                    row["DiscountAmount"],
                    row["PriceAll"],
                    row["Prepayment"]
                );

                // Форматирование по статусу
                string status = row["IdStatus"].ToString();

                DataGridViewRow dataGridRow = dataGridView1.Rows[rowIndex];

                switch (status)
                {
                    case "Принят":
                        // Желтый фон для всей строки
                        foreach (DataGridViewCell cell in dataGridRow.Cells)
                        {
                            cell.Style.BackColor = Color.FromArgb(255, 254, 230);
                        }
                        break;
                    case "Оплачен":
                        // Зеленый фон для всей строки
                        foreach (DataGridViewCell cell in dataGridRow.Cells)
                        {
                            cell.Style.BackColor = Color.FromArgb(240, 255, 230);
                        }
                        break;
                    case "Отменен":
                        // Красный фон для всей строки
                        foreach (DataGridViewCell cell in dataGridRow.Cells)
                        {
                            cell.Style.BackColor = Color.FromArgb(255, 230, 230);
                        }
                        break;
                }
            }

            int displayedCount = dataGridView1.Rows.Count;
            if (dataGridView1.AllowUserToAddRows && displayedCount > 0)
            {
                displayedCount--;
            }
            UpdateRowCount(displayedCount, tableToDisplay.Rows.Count);
        }

        private string FormatPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 4)
                return phoneNumber;

            // Оставляем первую цифру и последние две цифры, остальное заменяем *
            char firstDigit = phoneNumber[0];
            string lastTwoDigits = phoneNumber.Substring(phoneNumber.Length - 2);

            // Создаем строку со звездочками
            int starsCount = phoneNumber.Length - 3; // минус первая цифра и две последние
            string stars = new string('*', starsCount);

            return $"{firstDigit}{stars}{lastTwoDigits}";
        }

        private void UpdateRowCount(int displayedCount, int totalCount)
        {
            // Считаем только видимые строки
            int visibleCount = 0;
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                if (dataGridView1.Rows[i].Visible)
                {
                    visibleCount++;
                }
            }

            label4.Text = $"{visibleCount} из {totalCount}";
        }

        // ========== СОБЫТИЯ ТАЙМЕРА ПОИСКА ==========

        private void SearchTimer_Tick(object sender, EventArgs e)
        {
            searchTimer.Stop();
            // Применяем фильтр поиска локально
            ApplySearchFilter();
        }

        private void ApplySearchFilter()
        {
            if (dataTable == null) return;

            // Просто перерисовываем DataGridView с текущим фильтром
            DisplayDataInDataGridView(dataTable);
        }

        // ========== СОБЫТИЯ КНОПОК ==========

        private void button1_Click(object sender, EventArgs e)
        {
            inactivityTimer.Stop();
            allowClose = true;
            this.Visible = false;
            MainFormMeneger mainFormMeneger = new MainFormMeneger();
            mainFormMeneger.ShowDialog();
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Получаем выбранную строку
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите заказ для просмотра", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataGridViewRow selectedRow = dataGridView1.SelectedRows[0];

            // Получаем ID заказа из выбранной строки
            string orderId = selectedRow.Cells["NumberOrder"].Value?.ToString();

            if (string.IsNullOrEmpty(orderId))
            {
                MessageBox.Show("Не удалось получить номер заказа", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            inactivityTimer.Stop();
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
                // Сбрасываем все фильтры
                textBox1.Text = "";

                // Восстанавливаем даты по умолчанию
                dateTimePicker1.Value = defaultStartDate;
                dateTimePicker2.Value = defaultEndDate;

                // Сбрасываем комбобокс
                comboBox1.SelectedIndex = 0;

                // Сбрасываем чекбоксы статусов
                checkBox1.Checked = false;
                checkBox2.Checked = false;
                checkBox3.Checked = false;

                // Перезагружаем данные
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
            // Разрешаем ввод только цифр
            if (!string.IsNullOrEmpty(textBox1.Text))
            {
                // Удаляем все нецифровые символы
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

            Pagination();
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Разрешаем: цифры, Backspace, Delete
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true; // Блокируем ввод
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadData(); // Перезагружаем данные с новым фильтром
            Pagination();
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            LoadData(); // Перезагружаем данные с новым диапазоном дат
            Pagination();
        }

        private void dateTimePicker2_ValueChanged(object sender, EventArgs e)
        {
            LoadData(); // Перезагружаем данные с новым диапазоном дат
            Pagination();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            LoadData(); // Перезагружаем данные с новым фильтром статусов
            Pagination();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            LoadData(); // Перезагружаем данные с новым фильтром статусов
            Pagination();
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            LoadData(); // Перезагружаем данные с новым фильтром статусов
            Pagination();
        }

        // ========== СОБЫТИЯ DataGridView ==========

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            button2.Enabled = (e.RowIndex >= 0);
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            button2.Enabled = dataGridView1.SelectedRows.Count > 0;
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                button2.PerformClick();
            }
        }

        // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========

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
                        {
                            comboBox1.Items.Add(rdr[1].ToString());
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

        // ========== СОБЫТИЯ ФОРМЫ ==========

        private bool allowClose = false;

        private void ViewingOrdersForMeneger_FormClosing(object sender, FormClosingEventArgs e)
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

        private void ViewingOrdersForMeneger_Load(object sender, EventArgs e)
        {
            // Загрузка данных при загрузке формы
            LoadData();
            Pagination();
        }
    }
}