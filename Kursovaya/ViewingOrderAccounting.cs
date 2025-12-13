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

        public ViewingOrderAccounting()
        {
            InitializeComponent();

            searchTimer = new Timer();
            searchTimer.Interval = 500;
            searchTimer.Tick += SearchTimer_Tick;

            inactivityTimeout = Properties.Settings.Default.InactivityTimeout * 1000;
            inactivityTimer = new Timer();
            inactivityTimer.Interval = inactivityTimeout;
            inactivityTimer.Tick += InactivityTimer_Tick;
            inactivityTimer.Start();

            button1.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button2.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button3.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button4.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            dataGridView1.BackgroundColor = System.Drawing.Color.FromArgb(255, 221, 153);
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dataGridView1.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            textBox1.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            comboBox1.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);

            SetupDateControls();
            SetupUserInfo();
            FillFilterUsers();
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
            DateTime minDate = new DateTime(2025, 01, 01);
            DateTime maxDate = DateTime.Today.AddMonths(6);

            dateTimePicker1.MinDate = minDate;
            dateTimePicker1.MaxDate = DateTime.Today;
            dateTimePicker2.MinDate = DateTime.Today;
            dateTimePicker2.MaxDate = maxDate;

            defaultStartDate = DateTime.Now.AddMonths(-6);
            defaultEndDate = DateTime.Now.AddMonths(6);

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
                        DisplayDataInDataGridView(dataTable);
                    }
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

            query.Append(" ORDER BY p.DateEvent ASC, p.NumberOrder DESC");
            return query.ToString();
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
                dataGridView1.Columns.Add("IdUser", "ФИО сотрудника");
                dataGridView1.Columns.Add("Price", "Цена");
                dataGridView1.Columns.Add("DiscountAmount", "Сумма скидки");
                dataGridView1.Columns.Add("PriceAll", "Полная стоимость");
                dataGridView1.Columns.Add("Prepayment", "Предоплата");
            }

            string searchText = textBox1.Text.Trim();
            DataView dv = new DataView(tableToDisplay);

            if (!string.IsNullOrEmpty(searchText))
                dv.RowFilter = $"CONVERT(NumberOrder, 'System.String') LIKE '%{searchText}%'";

            decimal totalSum = 0;
            foreach (DataRowView rowView in dv)
            {
                DataRow row = rowView.Row;
                dataGridView1.Rows.Add(
                    row["NumberOrder"],
                    row["IdClient"],
                    FormatPhoneNumber(row["NumberPhoneClient"].ToString()),
                    FormatDate(row["DateOfConclusion"]),
                    FormatDate(row["DateEvent"]),
                    row["IdSchedule"],
                    row["IdStatus"],
                    row["IdEvent"],
                    row["IdUser"],
                    row["Price"],
                    row["DiscountAmount"],
                    row["PriceAll"],
                    row["Prepayment"]
                );

                string status = row["IdStatus"].ToString();
                decimal priceAll = 0;
                decimal prepayment = 0;

                if (row["PriceAll"] != null && row["PriceAll"] != DBNull.Value)
                    decimal.TryParse(row["PriceAll"].ToString(), out priceAll);

                if (row["Prepayment"] != null && row["Prepayment"] != DBNull.Value)
                    decimal.TryParse(row["Prepayment"].ToString(), out prepayment);

                if (status == "Отменен")
                    continue;
                else if (status == "Принят")
                    totalSum += prepayment;
                else
                    totalSum += priceAll;
            }

            label4.Text = dataGridView1.Rows.Count.ToString();
            label12.Text = totalSum.ToString("C2");
        }

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

        private void SearchTimer_Tick(object sender, EventArgs e)
        {
            searchTimer.Stop();
            if (dataTable != null)
                DisplayDataInDataGridView(dataTable);
        }

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

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                textBox1.Text = "";
                FillFilterUsers();

                DateTime safeStartDate = defaultStartDate;
                DateTime safeEndDate = defaultEndDate;

                if (safeStartDate < dateTimePicker1.MinDate)
                    safeStartDate = dateTimePicker1.MinDate;
                if (safeStartDate > dateTimePicker1.MaxDate)
                    safeStartDate = dateTimePicker1.MaxDate;

                if (safeEndDate < dateTimePicker2.MinDate)
                    safeEndDate = dateTimePicker2.MinDate;
                if (safeEndDate > dateTimePicker2.MaxDate)
                    safeEndDate = dateTimePicker2.MaxDate;

                dateTimePicker1.Value = safeStartDate;
                dateTimePicker2.Value = safeEndDate;

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

        private void button3_Click(object sender, EventArgs e)
        {
            allowClose = true;
            this.Visible = false;
            MainFormDirector mainFormDirector = new MainFormDirector();
            mainFormDirector.ShowDialog();
            this.Close();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            ExportToExcel();
        }

        private void buttonEnable()
        {
            button2.Enabled = true;
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            buttonEnable();
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            button2.Enabled = dataGridView1.SelectedRows.Count > 0;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadData();
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            LoadData();
        }

        private void dateTimePicker2_ValueChanged(object sender, EventArgs e)
        {
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

        private bool allowClose = false;

        private void ViewingOrderAccounting_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.ApplicationExitCall)
                return;

            if (!allowClose)
                e.Cancel = true;
        }

        private void ViewingOrderAccounting_Load(object sender, EventArgs e)
        {
            LoadData();
        }

        private void ExportToExcel()
        {
            Microsoft.Office.Interop.Excel.Application excelApp = null;
            Microsoft.Office.Interop.Excel.Workbook workbook = null;

            try
            {
                excelApp = new Microsoft.Office.Interop.Excel.Application();
                excelApp.Visible = true;

                workbook = excelApp.Workbooks.Add();

                // Создаем первый лист для данных
                Microsoft.Office.Interop.Excel.Worksheet dataWorksheet = workbook.Worksheets[1];
                dataWorksheet.Name = "Данные по заказам";

                // Создаем второй лист для статистики
                Microsoft.Office.Interop.Excel.Worksheet statsWorksheet = workbook.Worksheets.Add();
                statsWorksheet.Name = "Статистика";

                // ========== ЛИСТ С ДАННЫМИ ==========

                // Заголовок отчета
                dataWorksheet.Cells[1, 1] = "ОТЧЕТ ПО ЗАКАЗАМ";
                Microsoft.Office.Interop.Excel.Range titleRange = dataWorksheet.Range[dataWorksheet.Cells[1, 1], dataWorksheet.Cells[1, 3]];
                titleRange.Merge();
                titleRange.Font.Bold = true;
                titleRange.Font.Size = 14;
                titleRange.HorizontalAlignment = Microsoft.Office.Interop.Excel.XlHAlign.xlHAlignCenter;

                // Информация о периоде отчета
                string periodInfo = $"Период отчета: с {dateTimePicker1.Value:dd.MM.yyyy} по {dateTimePicker2.Value:dd.MM.yyyy}";
                dataWorksheet.Cells[2, 1] = periodInfo;
                Microsoft.Office.Interop.Excel.Range periodRange = dataWorksheet.Range[dataWorksheet.Cells[2, 1], dataWorksheet.Cells[2, 3]];
                periodRange.Merge();
                periodRange.Font.Size = 11;
                periodRange.Font.Italic = true;

                int headerRow = 5; // Строка с заголовками столбцов

                // Заполняем заголовки столбцов
                for (int i = 0; i < dataGridView1.Columns.Count; i++)
                {
                    if (dataGridView1.Columns[i].Visible)
                    {
                        dataWorksheet.Cells[headerRow, i + 1] = dataGridView1.Columns[i].HeaderText;
                    }
                }

                // Получаем полные данные из базы для телефонов
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

                                // Для номера телефона получаем полный номер из базы
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

                // Определяем индекс столбца со статусом для форматирования
                int statusColumnIndex = -1;
                for (int i = 0; i < dataGridView1.Columns.Count; i++)
                {
                    if (dataGridView1.Columns[i].HeaderText == "Статус" && dataGridView1.Columns[i].Visible)
                    {
                        statusColumnIndex = i + 1; // +1 потому что Excel индексируется с 1
                        break;
                    }
                }

                // Применяем форматирование к ячейкам со статусом
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

                // Форматирование заголовков таблицы
                Microsoft.Office.Interop.Excel.Range tableHeaders = dataWorksheet.Range[
                    dataWorksheet.Cells[headerRow, 1],
                    dataWorksheet.Cells[headerRow, dataGridView1.Columns.Count]];
                tableHeaders.Font.Bold = true;
                tableHeaders.Interior.Color = System.Drawing.ColorTranslator.ToOle(Color.FromArgb(240, 240, 240));
                tableHeaders.HorizontalAlignment = Microsoft.Office.Interop.Excel.XlHAlign.xlHAlignCenter;

                // Добавляем рамки к таблице
                Microsoft.Office.Interop.Excel.Range dataRange = dataWorksheet.Range[
                    dataWorksheet.Cells[headerRow, 1],
                    dataWorksheet.Cells[rowIndex - 1, dataGridView1.Columns.Count]];
                dataRange.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                dataRange.Borders.Weight = Microsoft.Office.Interop.Excel.XlBorderWeight.xlThin;

                // Автоподбор ширины столбцов
                Microsoft.Office.Interop.Excel.Range allDataRange = dataWorksheet.UsedRange;
                allDataRange.Columns.AutoFit();

                // Ограничиваем максимальную ширину столбцов
                foreach (Microsoft.Office.Interop.Excel.Range column in allDataRange.Columns)
                {
                    if (column.ColumnWidth > 30)
                        column.ColumnWidth = 30;
                }

                // ========== ЛИСТ СО СТАТИСТИКОЙ ==========

                // Заголовок листа статистики
                statsWorksheet.Cells[1, 1] = "СТАТИСТИКА ПО ЗАКАЗАМ";
                Microsoft.Office.Interop.Excel.Range statsTitleRange = statsWorksheet.Range[statsWorksheet.Cells[1, 1], statsWorksheet.Cells[1, 2]];
                statsTitleRange.Merge();
                statsTitleRange.Font.Bold = true;
                statsTitleRange.Font.Size = 14;
                statsTitleRange.HorizontalAlignment = Microsoft.Office.Interop.Excel.XlHAlign.xlHAlignCenter;

                // Информация о периоде на листе статистики
                statsWorksheet.Cells[2, 1] = periodInfo;
                Microsoft.Office.Interop.Excel.Range statsPeriodRange = statsWorksheet.Range[statsWorksheet.Cells[2, 1], statsWorksheet.Cells[2, 2]];
                statsPeriodRange.Merge();
                statsPeriodRange.Font.Size = 11;
                statsPeriodRange.Font.Italic = true;

                int statsRow = 4; // Начальная строка для статистики

                // Рассчитываем статистику
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
                        switch (status)
                        {
                            case "Принят":
                                acceptedOrders++;
                                if (row.Cells["Prepayment"].Value != null && decimal.TryParse(row.Cells["Prepayment"].Value.ToString(), out decimal prepayment))
                                    totalPrepayment += prepayment;
                                break;
                            case "Оплачен":
                                paidOrders++;
                                if (row.Cells["PriceAll"].Value != null && decimal.TryParse(row.Cells["PriceAll"].Value.ToString(), out decimal priceAll))
                                    totalRevenue += priceAll;
                                break;
                            case "Отменен":
                                cancelledOrders++;
                                break;
                        }

                        // Для принятых заказов также считаем предоплату в общую выручку
                        if (status == "Принят" && row.Cells["Prepayment"].Value != null && decimal.TryParse(row.Cells["Prepayment"].Value.ToString(), out decimal prepayment2))
                            totalRevenue += prepayment2;
                    }
                }

                // Общая статистика
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

                // Финансовая статистика
                statsWorksheet.Cells[statsRow, 1] = "ФИНАНСОВАЯ СТАТИСТИКА:";
                statsWorksheet.Cells[statsRow, 1].Font.Bold = true;
                statsWorksheet.Cells[statsRow, 1].Font.Size = 12;
                statsRow += 2;

                statsWorksheet.Cells[statsRow, 1] = "Общая выручка:";
                statsWorksheet.Cells[statsRow, 1].Font.Bold = true;
                statsWorksheet.Cells[statsRow, 2] = totalRevenue.ToString("C2");
                statsWorksheet.Cells[statsRow, 2].NumberFormat = "#,##0.00 ₽";
                statsRow++;

                statsWorksheet.Cells[statsRow, 1] = "Сумма предоплат:";
                statsWorksheet.Cells[statsRow, 2] = totalPrepayment.ToString("C2");
                statsWorksheet.Cells[statsRow, 2].NumberFormat = "#,##0.00 ₽";
                statsRow += 2;

                // Информация об отчете
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

                // Автоподбор ширины столбцов на листе статистики
                statsWorksheet.Columns.AutoFit();

                // Переходим на первый лист
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
                // Освобождаем COM-объекты
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

        // Новая функция для получения полных данных (с телефоном без маскировки)
        private DataTable GetFullDataForExport()
        {
            try
            {
                string query = BuildQuery(); // Используем существующий метод BuildQuery()

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