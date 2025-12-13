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
    public partial class ViewingOrderForDirector : Form
    {
        string conString = $"host={Properties.Settings.Default.host};uid={Properties.Settings.Default.uid};pwd={Properties.Settings.Default.pwd};database={Properties.Settings.Default.database};";
        private string orderId;
        private int rowCount = 0;
        private DataTable orderItems;
        private OrderData orderData;
        private Timer inactivityTimer;
        private int inactivityTimeout;

        public ViewingOrderForDirector(string orderId)
        {
            InitializeComponent();
            this.orderId = orderId;

            // Загружаем данные заказа
            LoadOrderData();

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
            dataGridView1.BackgroundColor = System.Drawing.Color.FromArgb(255, 221, 153);
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dataGridView1.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(217, 152, 22);
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

            label18.Text = rowCount.ToString();
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

        public class OrderData
        {
            public string NumberOrder { get; set; }
            public string DateOrder { get; set; }
            public string NameClient { get; set; }
            public string NumberPhone { get; set; }
            public string Event { get; set; }
            public string Date { get; set; }
            public string Time { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal DiscountAmount { get; set; }
            public decimal FinalAmount { get; set; }
            public decimal Prepayment { get; set; }
            public string Status { get; set; }
            public string NameUser { get; set; } // Добавляем это свойство
        }

        private void LoadOrderData()
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(conString))
                {
                    con.Open();

                    string orderQuery = @"
                                            SELECT 
                                                o.NumberOrder,
                                                o.DateOfConclusion,
                                                o.DateEvent,
                                                s.StartTime,
                                                s.EndTime,
                                                c.Name as ClientName,
                                                o.NumberPhoneClient,
                                                e.Event as EventName,
                                                o.Price as TotalAmount,
                                                o.DiscountAmount,
                                                w.FullName as NameUser,
                                                o.PriceAll as FinalAmount,
                                                o.Prepayment,
                                                st.Status as OrderStatus
                                            FROM Orders o
                                            LEFT JOIN Clients c ON o.IdClient = c.IDclient
                                            LEFT JOIN Events e ON o.IdEvent = e.IDevent
                                            LEFT JOIN Schedule s ON o.IdSchedule = s.IDschedule
                                            LEFT JOIN Status st ON o.IdStatus = st.IDstatus
                                            LEFT JOIN Users w ON o.IdUser = w.IDuser
                                            WHERE o.NumberOrder = @orderId";

                    using (MySqlCommand cmd = new MySqlCommand(orderQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@orderId", orderId);

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Правильное чтение TimeSpan
                                string startTime = reader["StartTime"] != DBNull.Value ?
                                    ((TimeSpan)reader["StartTime"]).ToString(@"hh\:mm") : "";
                                string endTime = reader["EndTime"] != DBNull.Value ?
                                    ((TimeSpan)reader["EndTime"]).ToString(@"hh\:mm") : "";
                                string timeRange = $"{startTime} - {endTime}";

                                // Создаем объект OrderData
                                orderData = new OrderData
                                {
                                    NumberOrder = reader["NumberOrder"].ToString(),
                                    DateOrder = Convert.ToDateTime(reader["DateOfConclusion"]).ToString("dd.MM.yyyy"),
                                    Date = Convert.ToDateTime(reader["DateEvent"]).ToString("dd.MM.yyyy"),
                                    Time = timeRange,
                                    NameClient = reader["ClientName"].ToString(),
                                    NumberPhone = reader["NumberPhoneClient"].ToString(),
                                    Event = reader["EventName"].ToString(),
                                    TotalAmount = reader["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(reader["TotalAmount"]) : 0,
                                    DiscountAmount = reader["DiscountAmount"] != DBNull.Value ? Convert.ToDecimal(reader["DiscountAmount"]) : 0,
                                    NameUser = reader["NameUser"] != DBNull.Value ? reader["NameUser"].ToString() : "Не указан",
                                    FinalAmount = reader["FinalAmount"] != DBNull.Value ? Convert.ToDecimal(reader["FinalAmount"]) : 0,
                                    Prepayment = reader["Prepayment"] != DBNull.Value ? Convert.ToDecimal(reader["Prepayment"]) : 0,
                                    Status = reader["OrderStatus"].ToString()
                                };

                                // Заполняем Label на форме
                                DisplayOrderInfo();
                            }
                            else
                            {
                                MessageBox.Show("Заказ не найден", "Ошибка",
                                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                                this.Close();
                                return;
                            }
                        }
                    }

                    // Загружаем состав заказа
                    LoadOrderComposition(con);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных заказа: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadOrderComposition(MySqlConnection con)
        {
            string compositionQuery = @"
                                        SELECT 
                                            d.Article,
                                            d.Name,
                                            d.Price,
                                            oc.Count,
                                            (d.Price * oc.Count) as Total
                                        FROM OrderComposition oc
                                        LEFT JOIN Dishes d ON oc.IdDish = d.Article
                                        WHERE oc.IdOrder = @orderId";

            orderItems = new DataTable();
            using (MySqlDataAdapter adapter = new MySqlDataAdapter(compositionQuery, con))
            {
                adapter.SelectCommand.Parameters.AddWithValue("@orderId", orderId);
                adapter.Fill(orderItems);
            }

            // Настраиваем DataGridView
            SetupOrderItemsDataGridView();
        }

        private void DisplayOrderInfo()
        {
            if (orderData == null) return;

            // Заполняем Label на форме
            label4.Text = orderData.NumberOrder;
            label12.Text = orderData.DateOrder;
            label14.Text = orderData.Date;
            label16.Text = orderData.Time;
            label6.Text = orderData.Event;
            label8.Text = orderData.NameClient;
            label10.Text = orderData.NumberPhone;

            // Рассчитываем суммы с учетом дополнительных расходов
            CalculateTotalWithAdditionalExpenses();
        }

        private void CalculateTotalWithAdditionalExpenses()
        {
            if (orderData == null) return;

            // Показываем исходные суммы без учета дополнительных расходов
            label20.Text = orderData.Prepayment.ToString("C");
            label22.Text = orderData.DiscountAmount.ToString("C");
            label24.Text = (orderData.FinalAmount > 0 ? orderData.FinalAmount : orderData.TotalAmount - orderData.DiscountAmount).ToString("C");
            return;

            // Получаем базовую сумму заказа (без дополнительных расходов)
            decimal baseTotalAmount = orderData.TotalAmount;
            decimal baseDiscountAmount = orderData.DiscountAmount;
            decimal baseFinalAmount = orderData.FinalAmount > 0 ? orderData.FinalAmount : baseTotalAmount - baseDiscountAmount;
            decimal basePrepayment = orderData.Prepayment;

            // Предоплата может быть пересчитана пропорционально или оставлена как есть
            decimal newPrepayment = basePrepayment;

            // Отображаем суммы
            label20.Text = newPrepayment.ToString("C");
            label22.Text = baseDiscountAmount.ToString("C");
        }

        private void SetupOrderItemsDataGridView()
        {
            dataGridView1.DataSource = orderItems;

            // Настройка колонок
            if (orderItems.Columns.Contains("Article"))
                dataGridView1.Columns["Article"].HeaderText = "Артикул";
            if (orderItems.Columns.Contains("Name"))
                dataGridView1.Columns["Name"].HeaderText = "Наименование";
            if (orderItems.Columns.Contains("Price"))
                dataGridView1.Columns["Price"].HeaderText = "Цена";
            if (orderItems.Columns.Contains("Count"))
                dataGridView1.Columns["Count"].HeaderText = "Количество";
            if (orderItems.Columns.Contains("Total"))
                dataGridView1.Columns["Total"].HeaderText = "Сумма";

            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Обновляем количество записей
            UpdateRowCount();
        }

        private void UpdateRowCount()
        {
            if (orderItems != null && orderItems.Rows.Count > 0)
            {
                rowCount = orderItems.Rows.Count;
                label18.Text = rowCount.ToString();
            }
            else
            {
                label18.Text = "0";
            }
        }

        private bool allowClose = false;

        private void button3_Click(object sender, EventArgs e)
        {
            allowClose = true;
            this.Visible = false;
            ViewingOrdersForMeneger viewingOrdersForMeneger = new ViewingOrdersForMeneger();
            viewingOrdersForMeneger.ShowDialog();
            this.Close();
        }

        private void ViewingAnOrder_FormClosing(object sender, FormClosingEventArgs e)
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

        private string GetTemplatePath()
        {
            string[] possiblePaths = {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "secondblank.docx"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "secondblank.docx"),
                Path.Combine(Directory.GetCurrentDirectory(), "Resources", "secondblank.docx"),
                @"Resources\secondblank.docx",
                @"..\Resources\secondblank.docx"
            };

            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return Path.GetFullPath(path);
                }
            }

            throw new FileNotFoundException("Шаблон secondblank.docx не найден. Проверьте наличие файла в папке Resources");
        }

        private void FillBookmark(Microsoft.Office.Interop.Word.Document doc, string bookmarkName, string value)
        {
            try
            {
                if (doc.Bookmarks.Exists(bookmarkName))
                {
                    Microsoft.Office.Interop.Word.Bookmark bookmark = doc.Bookmarks[bookmarkName];
                    Microsoft.Office.Interop.Word.Range range = bookmark.Range;
                    range.Text = value;
                    doc.Bookmarks[bookmarkName].Delete();
                }
                else
                {
                    Console.WriteLine($"Закладка '{bookmarkName}' не найдена");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при заполнении закладки '{bookmarkName}': {ex.Message}");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            inactivityTimer.Stop();
            allowClose = true;
            this.Visible = false;
            ViewingOrderAccounting viewingOrderAccounting = new ViewingOrderAccounting();
            viewingOrderAccounting.ShowDialog();
            this.Close();
        }
    }
}
