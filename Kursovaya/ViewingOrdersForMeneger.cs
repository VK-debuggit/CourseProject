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

        public ViewingOrdersForMeneger()
        {
            InitializeComponent();

            // Инициализация таймера для поиска с задержкой
            searchTimer = new Timer();
            searchTimer.Interval = 500; // 500 мс задержка
            searchTimer.Tick += SearchTimer_Tick;

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

        private void LoadData()
        {
            try
            {
                // Отображаем индикатор загрузки
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

                        // ВАЖНО! Отображаем данные после загрузки
                        DisplayDataInDataGridView(dataTable);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine("Error details: " + ex.ToString());
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

            // Очищаем только строки
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

            // Фильтруем по номеру заказа локально (посимвольный поиск)
            string searchText = textBox1.Text.Trim();
            DataView dv = new DataView(tableToDisplay);

            if (!string.IsNullOrEmpty(searchText))
            {
                // Используем DataView.RowFilter для поиска по части номера
                // CONVERT преобразует NumberOrder в строку для поиска
                dv.RowFilter = $"CONVERT(NumberOrder, 'System.String') LIKE '%{searchText}%'";
            }

            // Заполняем DataGridView отфильтрованными данными
            foreach (DataRowView rowView in dv)
            {
                DataRow row = rowView.Row;
                dataGridView1.Rows.Add(
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
            }

            // Обновляем счетчик строк
            UpdateRowCount();
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

        private void UpdateRowCount()
        {
            int rowCount = dataGridView1.Rows.Count;
            if (dataGridView1.AllowUserToAddRows)
                rowCount--; // Не учитываем пустую строку для добавления

            label4.Text = rowCount.ToString();
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
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            LoadData(); // Перезагружаем данные с новым диапазоном дат
        }

        private void dateTimePicker2_ValueChanged(object sender, EventArgs e)
        {
            LoadData(); // Перезагружаем данные с новым диапазоном дат
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            LoadData(); // Перезагружаем данные с новым фильтром статусов
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            LoadData(); // Перезагружаем данные с новым фильтром статусов
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            LoadData(); // Перезагружаем данные с новым фильтром статусов
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
        }
    }
}