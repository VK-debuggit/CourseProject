using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace Kursovaya
{
    public partial class ViewingAnOrderForMeneger : Form
    {
        string conString = $"host={Properties.Settings.Default.host};uid={Properties.Settings.Default.uid};pwd={Properties.Settings.Default.pwd};database={Properties.Settings.Default.database};";
        private DataTable cartItems;
        private OrderData orderData;
        private decimal discountAmountValue;
        private int selectedProductRowIndex = -1;
        private bool isWordGenerated = false;
        private bool isOrderSaved = false;

        public ViewingAnOrderForMeneger(DataTable cartItems, OrderData orderData)
        {
            InitializeComponent();
            this.cartItems = cartItems;
            this.orderData = orderData;
            InitializeViewOrderForm();

            isWordGenerated = false;
            isOrderSaved = false;

            button1.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button2.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button3.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
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

            CountOrderItems();
        }

        //Инициализация формы просмотра заказа
        private void InitializeViewOrderForm()
        {
            DisplayOrderInfo();
            SetupCartDataGridView();
        }

        //Подсчет количества позиций в заказе
        private void CountOrderItems()
        {
            if (cartItems != null && cartItems.Rows.Count > 0)
            {
                int totalItems = cartItems.Rows.Count;
                label25.Text = totalItems.ToString();
            }
            else
            {
                label25.Text = "0";
            }
        }

        //Отображение информации о заказе
        private void DisplayOrderInfo()
        {
            label4.Text = orderData.NumberOrder;
            label6.Text = orderData.DateOrder;
            label12.Text = orderData.Event;
            label8.Text = orderData.Date;
            label14.Text = orderData.NameClient;
            label10.Text = orderData.Time;
            label16.Text = orderData.NumberPhone;
            label21.Text = $"{orderData.Prepayment} ₽";

            CalculateDiscountAndPrepayment();
        }

        //Расчет скидки и предоплаты
        private void CalculateDiscountAndPrepayment()
        {
            decimal totalAmount = orderData.TotalAmount;
            decimal discountAmount = 0;
            discountAmountValue = 0;
            decimal discountPercent = 0;
            decimal amountAfterDiscount = totalAmount;
            decimal prepayment = 0;
            string discountDescription = "";

            if (totalAmount >= 40000)
            {
                discountPercent = 15;
                discountDescription = " (15% при сумме заказа от 40 000 ₽ и выше)";
            }
            else if (totalAmount >= 30000)
            {
                discountPercent = 10;
                discountDescription = " (10% при сумме заказа от 30 000 ₽ и выше)";
            }

            discountAmountValue = totalAmount * discountPercent / 100;
            amountAfterDiscount = totalAmount - discountAmount;
            prepayment = amountAfterDiscount / 2;

            label19.Text = $"{Math.Round(totalAmount)} ₽";

            if (discountPercent > 0)
            {
                label23.Text = $"{Math.Round(discountAmountValue)} ₽{discountDescription}";
            }
            else
            {
                label23.Text = "0 ₽";
            }
        }

        //Настройка таблицы товаров
        private void SetupCartDataGridView()
        {
            dataGridView1.DataSource = cartItems;

            dataGridView1.Columns["Article"].HeaderText = "Артикул";
            dataGridView1.Columns["Name"].HeaderText = "Наименование";
            dataGridView1.Columns["Price"].HeaderText = "Цена";
            dataGridView1.Columns["Quantity"].HeaderText = "Количество";
            dataGridView1.Columns["Total"].HeaderText = "Сумма";

            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private bool allowClose = false;

        //Обработчик кнопки возврата
        private void button3_Click(object sender, EventArgs e)
        {
            if (!isWordGenerated)
            {
                DialogResult result = MessageBox.Show(
                    "Если вы вернетесь к оформлению, изменения не сохранятся. Продолжить?",
                    "Подтверждение",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                {
                    return;
                }
            }

            allowClose = true;
            this.Visible = false;
            MakingAnOrder makingAnOrder = new MakingAnOrder();
            makingAnOrder.ShowDialog();
            this.Close();
        }

        //Обработчик закрытия формы
        private void ViewingAnOrderForMeneger_FormClosing(object sender, FormClosingEventArgs e)
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

        //Обработчик кнопки печати
        private void button2_Click(object sender, EventArgs e)
        {
            this.Visible = false;
            SelectFormPrint selectFormPrint = new SelectFormPrint(cartItems, orderData, discountAmountValue, DocumentType.Preliminary, this);
            selectFormPrint.ShowDialog();
            this.Close();
        }

        //Обработчик кнопки сохранения заказа
        private void button1_Click(object sender, EventArgs e)
        {
            if (cartItems.Rows.Count == 0)
            {
                MessageBox.Show("Добавьте товары в заказ", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (SaveOrderToDatabase())
                {
                    MessageBox.Show("Заказ успешно оформлен!", "Успех",
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);

                    isOrderSaved = true;

                    button1.Enabled = false;
                    button2.Enabled = true;
                    button3.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка оформления заказа: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //Сохранение заказа в базу данных
        private bool SaveOrderToDatabase()
        {
            using (MySqlConnection con = new MySqlConnection(conString))
            {
                con.Open();
                MySqlTransaction transaction = con.BeginTransaction();

                try
                {
                    int nextOrderNumber = Convert.ToInt32(label4.Text);
                    int clientId = GetClient(con, transaction);
                    int userId = GetCurrentUserId(con, transaction);
                    int eventId = GetEventIdByName(con, transaction, orderData.Event);
                    int scheduleId = GetScheduleId(con, transaction, orderData.Time);
                    DateTime DateOfConclusion = Convert.ToDateTime(label6.Text).Date;
                    DateTime DateEvent = Convert.ToDateTime(label8.Text).Date;
                    int statusId = GetStatusId(con, transaction);

                    decimal totalAmount = Math.Round(Convert.ToDecimal(CleanDecimalString(label19.Text)), 0);
                    decimal discountAmount = Math.Round(discountAmountValue, 0);
                    decimal finalAmount = Math.Round(totalAmount - discountAmount, 0);
                    decimal prepayment = Math.Round(Convert.ToDecimal(CleanDecimalString(label21.Text)), 0);

                    InsertMainOrder(con, transaction, nextOrderNumber, clientId, userId, eventId, scheduleId, statusId,
                                  totalAmount, discountAmount, finalAmount, prepayment, DateOfConclusion, DateEvent);

                    InsertOrderComposition(con, transaction, nextOrderNumber);

                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show($"Ошибка сохранения заказа: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
        }

        //Очистка строки с денежным значением
        private string CleanDecimalString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "0";

            string cleaned = value.Replace("₽", "").Replace("руб", "").Replace(" ", "").Trim();

            if (string.IsNullOrEmpty(cleaned) || !decimal.TryParse(cleaned, out _))
                return "0";

            return cleaned;
        }

        //Получение клиента
        private int GetClient(MySqlConnection con, MySqlTransaction transaction)
        {
            string checkQuery = "SELECT IDclient FROM Clients WHERE Name = @name";
            using (MySqlCommand cmd = new MySqlCommand(checkQuery, con, transaction))
            {
                cmd.Parameters.AddWithValue("@name", orderData.NameClient);
                object result = cmd.ExecuteScalar();

                if (result != null)
                {
                    return Convert.ToInt32(result);
                }
                else
                {
                    throw new Exception($"Клиент '{orderData.NameClient}' не найден в базе данных");
                }
            }
        }

        //Получение текущего пользователя
        private int GetCurrentUserId(MySqlConnection con, MySqlTransaction transaction)
        {
            string checkQuery = "SELECT IDuser FROM Users WHERE FullName = @name";
            using (MySqlCommand cmd = new MySqlCommand(checkQuery, con, transaction))
            {
                cmd.Parameters.AddWithValue("@name", Properties.Settings.Default.userName);
                object result = cmd.ExecuteScalar();

                if (result != null)
                {
                    return Convert.ToInt32(result);
                }
                else
                {
                    throw new Exception("Пользователь не найден в базе данных");
                }
            }
        }

        //Получение ID статуса
        private int GetStatusId(MySqlConnection con, MySqlTransaction transaction)
        {
            string query = "SELECT IDstatus FROM Status WHERE Status = 'Принят';";
            using (MySqlCommand cmd = new MySqlCommand(query, con, transaction))
            {
                object result = cmd.ExecuteScalar();

                if (result != null)
                {
                    return Convert.ToInt32(result);
                }
                else
                {
                    throw new Exception("Статус не найдет.");
                }
            }
        }

        //Получение ID мероприятия по названию
        private int GetEventIdByName(MySqlConnection con, MySqlTransaction transaction, string eventName)
        {
            string query = "SELECT IDevent FROM Events WHERE Event = @eventName";
            using (MySqlCommand cmd = new MySqlCommand(query, con, transaction))
            {
                cmd.Parameters.AddWithValue("@eventName", eventName);
                object result = cmd.ExecuteScalar();

                if (result != null)
                {
                    return Convert.ToInt32(result);
                }
                else
                {
                    throw new Exception($"Мероприятие '{eventName}' не найдено в базе данных");
                }
            }
        }

        //Получение ID расписания по временному слоту
        private int GetScheduleId(MySqlConnection con, MySqlTransaction transaction, string timeRange)
        {
            string[] timeParts = timeRange.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);

            if (timeParts.Length != 2)
            {
                throw new Exception($"Неверный формат времени: {timeRange}");
            }

            string startTime = timeParts[0].Trim();
            string endTime = timeParts[1].Trim();

            string query = @"SELECT IDschedule FROM Schedule 
                 WHERE StartTime = @startTime AND EndTime = @endTime";

            using (MySqlCommand cmd = new MySqlCommand(query, con, transaction))
            {
                cmd.Parameters.AddWithValue("@startTime", TimeSpan.Parse(startTime));
                cmd.Parameters.AddWithValue("@endTime", TimeSpan.Parse(endTime));

                object result = cmd.ExecuteScalar();

                if (result != null)
                {
                    return Convert.ToInt32(result);
                }
                else
                {
                    throw new Exception("Время не найдено в базе данных");
                }
            }
        }

        //Вставка основного заказа
        private void InsertMainOrder(MySqlConnection con, MySqlTransaction transaction, int orderNumber,
       int clientId, int userId, int eventId, int scheduleId, int statusId,
       decimal price, decimal discountAmount, decimal priceAll, decimal prepayment,
       DateTime dateConclusion, DateTime dateEvent)
        {
            string query = @"INSERT INTO Orders (
                    NumberOrder, IdClient, NumberPhoneClient, DateOfConclusion, 
                    DateEvent, IdSchedule, IdStatus, IdEvent, IdUser, 
                    Price, DiscountAmount, PriceAll, Prepayment
                ) VALUES (
                    @numberOrder, @idClient, @phone, @dateConclusion, 
                    @dateEvent, @idSchedule, @idStatus, @idEvent, @idUser, 
                    @price, @discountAmount, @priceAll, @prepayment
                )";

            using (MySqlCommand cmd = new MySqlCommand(query, con, transaction))
            {
                cmd.Parameters.AddWithValue("@numberOrder", orderNumber);
                cmd.Parameters.AddWithValue("@idClient", clientId);
                cmd.Parameters.AddWithValue("@dateConclusion", dateConclusion.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@dateEvent", dateEvent.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@idSchedule", scheduleId);
                cmd.Parameters.AddWithValue("@idStatus", statusId);
                cmd.Parameters.AddWithValue("@idEvent", eventId);
                cmd.Parameters.AddWithValue("@idUser", userId);
                cmd.Parameters.AddWithValue("@price", price);
                cmd.Parameters.AddWithValue("@discountAmount", discountAmount);
                cmd.Parameters.AddWithValue("@priceAll", priceAll);
                cmd.Parameters.AddWithValue("@prepayment", prepayment);

                string cleanPhone = CleanPhoneNumber(orderData.NumberPhone);
                cmd.Parameters.AddWithValue("@phone", cleanPhone);

                cmd.ExecuteNonQuery();
            }
        }

        //Очистка номера телефона
        private string CleanPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return "";

            string cleanNumber = new string(phoneNumber.Where(char.IsDigit).ToArray());

            return cleanNumber;
        }

        //Вставка состава заказа
        private void InsertOrderComposition(MySqlConnection con, MySqlTransaction transaction, int orderNumber)
        {
            string query = @"INSERT INTO OrderComposition (IdOrder, IdDish, Count) 
                 VALUES (@idOrder, @idDish, @count)";

            foreach (DataRow row in cartItems.Rows)
            {
                using (MySqlCommand cmd = new MySqlCommand(query, con, transaction))
                {
                    string article = row["Article"].ToString();
                    int quantity = Convert.ToInt32(row["Quantity"]);

                    cmd.Parameters.AddWithValue("@idOrder", orderNumber);
                    cmd.Parameters.AddWithValue("@idDish", article.PadLeft(6, '0'));
                    cmd.Parameters.AddWithValue("@count", quantity);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}