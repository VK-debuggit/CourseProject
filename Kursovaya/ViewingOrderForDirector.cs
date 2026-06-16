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

        public ViewingOrderForDirector(string orderId)
        {
            InitializeComponent();
            this.orderId = orderId;

            LoadOrderData();

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

        //Класс данных заказа
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
            public string NameUser { get; set; }
        }

        //Загрузка данных заказа
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
                                string startTime = reader["StartTime"] != DBNull.Value ?
                                    ((TimeSpan)reader["StartTime"]).ToString(@"hh\:mm") : "";
                                string endTime = reader["EndTime"] != DBNull.Value ?
                                    ((TimeSpan)reader["EndTime"]).ToString(@"hh\:mm") : "";
                                string timeRange = $"{startTime} - {endTime}";

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

                    LoadOrderComposition(con);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных заказа: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //Загрузка состава заказа
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

            SetupOrderItemsDataGridView();
        }

        //Отображение информации о заказе
        private void DisplayOrderInfo()
        {
            if (orderData == null) return;

            label4.Text = orderData.NumberOrder;
            label12.Text = orderData.DateOrder;
            label14.Text = orderData.Date;
            label16.Text = orderData.Time;
            label6.Text = orderData.Event;
            label8.Text = orderData.NameClient;
            label10.Text = orderData.NumberPhone;

            CalculateTotalWithAdditionalExpenses();
        }

        //Расчет суммы с учетом дополнительных расходов
        private void CalculateTotalWithAdditionalExpenses()
        {
            if (orderData == null) return;

            label20.Text = ((int)orderData.Prepayment).ToString("C0");
            label22.Text = ((int)orderData.DiscountAmount).ToString("C0");

            decimal finalAmount = orderData.FinalAmount > 0 ? orderData.FinalAmount : orderData.TotalAmount - orderData.DiscountAmount;
            label24.Text = ((int)finalAmount).ToString("C0");
            return;

            decimal baseTotalAmount = orderData.TotalAmount;
            decimal baseDiscountAmount = orderData.DiscountAmount;
            decimal baseFinalAmount = orderData.FinalAmount > 0 ? orderData.FinalAmount : baseTotalAmount - baseDiscountAmount;
            decimal basePrepayment = orderData.Prepayment;

            decimal newPrepayment = basePrepayment;

            label20.Text = newPrepayment.ToString("C");
            label22.Text = baseDiscountAmount.ToString("C");
        }

        //Настройка таблицы состава заказа
        private void SetupOrderItemsDataGridView()
        {
            dataGridView1.DataSource = orderItems;

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

            UpdateRowCount();
        }

        //Обновление количества записей
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

        //Обработчик кнопки возврата в учет заказов
        private void button1_Click(object sender, EventArgs e)
        {
            allowClose = true;
            this.Visible = false;
            ViewingOrderAccounting viewingOrderAccounting = new ViewingOrderAccounting();
            viewingOrderAccounting.ShowDialog();
            this.Close();
        }

        private void ViewingOrderForDirector_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.ApplicationExitCall)
                return;

            if (!allowClose)
                e.Cancel = true;
        }
    }
}