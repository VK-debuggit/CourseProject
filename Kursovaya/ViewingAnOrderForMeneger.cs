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
    public partial class ViewingAnOrderForMeneger : Form
    {
        string conString = $"host={Properties.Settings.Default.host};uid={Properties.Settings.Default.uid};pwd={Properties.Settings.Default.pwd};database={Properties.Settings.Default.database};";
        private DataTable cartItems;
        private OrderData orderData;
        private int selectedProductRowIndex = -1;
        private bool isWordGenerated = false; // Флаг, что Word документ был создан

        public ViewingAnOrderForMeneger(DataTable cartItems, OrderData orderData)
        {
            InitializeComponent();
            this.cartItems = cartItems;
            this.orderData = orderData;
            InitializeViewOrderForm();
            button1.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button2.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button3.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            dataGridView1.BackgroundColor = System.Drawing.Color.FromArgb(255, 221, 153);
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dataGridView1.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            
            button1.Enabled = false;

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

            // Считаем количество записей в заказе
            CountOrderItems();
        }

        private void InitializeViewOrderForm()
        {
            // Заполняем информацию о заказе
            DisplayOrderInfo();

            // Настраиваем DataGridView с товарами
            SetupCartDataGridView();
        }

        private void CountOrderItems()
        {
            if (cartItems != null && cartItems.Rows.Count > 0)
            {
                // Подсчитываем общее количество позиций в заказе
                int totalItems = cartItems.Rows.Count;
                label25.Text = totalItems.ToString();
            }
            else
            {
                label25.Text = "0";
            }
        }

        private void DisplayOrderInfo()
        {
            // Выводим информацию из orderData
            label4.Text = orderData.NumberOrder;
            label6.Text = orderData.DateOrder;
            label12.Text = orderData.Event;
            label8.Text = orderData.Date;
            label14.Text = orderData.NameClient;
            label10.Text = orderData.Time;
            label16.Text = orderData.NumberPhone;
            label21.Text = orderData.Prepayment.ToString("C");

            // Рассчитываем скидку и предоплату
            CalculateDiscountAndPrepayment();
        }

        private void CalculateDiscountAndPrepayment()
        {
            decimal totalAmount = orderData.TotalAmount;
            decimal discountAmount = 0;
            decimal discountPercent = 0;
            decimal amountAfterDiscount = totalAmount;
            decimal prepayment = 0;

            // Определяем размер скидки
            if (totalAmount >= 40000)
            {
                discountPercent = 15;
            }
            else if (totalAmount >= 30000)
            {
                discountPercent = 10;
            }

            // Рассчитываем суммы
            discountAmount = totalAmount * discountPercent / 100;
            amountAfterDiscount = totalAmount - discountAmount;
            prepayment = amountAfterDiscount / 2;

            // Обновляем интерфейс
            label19.Text = totalAmount.ToString("C"); 
            label23.Text = discountAmount.ToString("C"); 
            //label21.Text = prepayment.ToString("C"); 
        }

        private void SetupCartDataGridView()
        {
            dataGridView1.DataSource = cartItems;

            // Настройка колонок
            dataGridView1.Columns["Article"].HeaderText = "Артикул";
            dataGridView1.Columns["Name"].HeaderText = "Наименование";
            dataGridView1.Columns["Price"].HeaderText = "Цена";
            dataGridView1.Columns["Quantity"].HeaderText = "Количество";
            dataGridView1.Columns["Total"].HeaderText = "Сумма";

            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private string GetTemplatePath()
        {
            // Пробуем разные возможные пути
            string[] possiblePaths = {
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "firstblank.docx"),
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "firstblank.docx"),
        Path.Combine(Directory.GetCurrentDirectory(), "Resources", "firstblank.docx"),
        @"Resources\firstblank.docx",
        @"..\Resources\firstblank.docx"
    };

            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return Path.GetFullPath(path); // Возвращаем полный путь
                }
            }

            throw new FileNotFoundException("Шаблон firstblank.docx не найден. Проверьте наличие файла в папке Resources");
        }

        private void GenerateWordTicket()
        {
            try
            {
                // Создаем Word Application
                Microsoft.Office.Interop.Word.Application wordApp = new Microsoft.Office.Interop.Word.Application();
                wordApp.Visible = true;

                // Путь к шаблону - правильные варианты:
                string templatePath1 = @"Resources\firstblank.docx"; // если файл в папке Resources рядом с exe
                string templatePath2 = Path.Combine("Resources", "firstblank.docx"); // кроссплатформенный вариант
                string templatePath3 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "firstblank.docx"); // полный путь

                // Проверяем какой путь работает
                string templatePath = GetTemplatePath();

                // Открываем шаблон
                Microsoft.Office.Interop.Word.Document doc = wordApp.Documents.Open(templatePath, ReadOnly: false);
                doc.Activate();

                // Рассчитываем суммы для шаблона
                decimal totalAmount = CalculateTotalAmount();
                (decimal discountAmount, decimal discountPercent, decimal prepayment) = CalculateDiscountValues();
                decimal finalAmount = totalAmount - discountAmount;

                // Заполняем закладки данными
                FillBookmark(doc, "NumberOrder", orderData.NumberOrder);
                FillBookmark(doc, "DateOrder", orderData.DateOrder);
                FillBookmark(doc, "NameClient", orderData.NameClient);
                FillBookmark(doc, "NumberPhone", orderData.NumberPhone);
                FillBookmark(doc, "Event", orderData.Event);
                FillBookmark(doc, "DateCreate", orderData.Date);
                FillBookmark(doc, "Time", orderData.Time);

                FillBookmark(doc, "CountOrder", totalAmount.ToString("C"));
                FillBookmark(doc, "DiscountAmoust", discountAmount.ToString("C"));
                FillBookmark(doc, "CountOrderAmoust", finalAmount.ToString("C"));
                FillBookmark(doc, "Prepaymant", prepayment.ToString("C"));
                FillBookmark(doc, "Discount", discountPercent.ToString());

                // Удаляем пример товара из шаблона и вставляем актуальную таблицу
                ReplaceExampleTableWithActualData(doc, wordApp);

                // Добавляем служебную информацию в конец документа
                AddServiceInfoToWord(doc);

                MessageBox.Show("Предварительный документ заказа создан.", "Успех",
                              MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании Word-документа: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

                    // Удаляем закладку после заполнения, чтобы не мешала
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

        private void ReplaceExampleTableWithActualData(Microsoft.Office.Interop.Word.Document doc, Microsoft.Office.Interop.Word.Application wordApp)
        {
            try
            {
                // Находим таблицу с примером товара (первую таблицу в документе)
                if (doc.Tables.Count > 0)
                {
                    Microsoft.Office.Interop.Word.Table exampleTable = doc.Tables[1];

                    // Определяем диапазон таблицы
                    Microsoft.Office.Interop.Word.Range tableRange = exampleTable.Range;

                    // Удаляем старую таблицу
                    exampleTable.Delete();

                    // Вставляем новую таблицу с актуальными данными на том же месте
                    InsertActualOrderTable(doc, wordApp, tableRange);
                }
                else
                {
                    // Если таблицы нет, вставляем в конец раздела "СОСТАВ ЗАКАЗА"
                    InsertActualOrderTable(doc, wordApp, null);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при замене таблицы: {ex.Message}");
                // Резервный вариант - вставляем таблицу обычным способом
                InsertActualOrderTable(doc, wordApp, null);
            }
        }

        private void InsertActualOrderTable(Microsoft.Office.Interop.Word.Document doc, Microsoft.Office.Interop.Word.Application wordApp, Microsoft.Office.Interop.Word.Range targetRange)
        {
            if (cartItems.Rows.Count == 0)
            {
                // Если нет товаров, вставляем сообщение
                Microsoft.Office.Interop.Word.Paragraph paragraph;
                if (targetRange != null)
                {
                    paragraph = doc.Paragraphs.Add(targetRange);
                }
                else
                {
                    paragraph = doc.Paragraphs.Add();
                }
                paragraph.Range.Text = "Заказ не содержит товаров";
                paragraph.Range.Font.Size = 12;
                paragraph.Range.InsertParagraphAfter();
                return;
            }

            Microsoft.Office.Interop.Word.Table table;

            // Создаем таблицу в указанном диапазоне или в конце документа
            if (targetRange != null)
            {
                table = doc.Tables.Add(targetRange, cartItems.Rows.Count + 1, 5);
            }
            else
            {
                table = doc.Tables.Add(doc.Range(doc.Content.End - 1), cartItems.Rows.Count + 1, 5);
            }

            // Настройка таблицы
            table.PreferredWidth = wordApp.CentimetersToPoints(16);
            table.AllowAutoFit = true;

            // Настройка ширины колонок
            table.Columns[1].PreferredWidth = wordApp.CentimetersToPoints(1);
            table.Columns[2].PreferredWidth = wordApp.CentimetersToPoints(8);
            table.Columns[3].PreferredWidth = wordApp.CentimetersToPoints(2);
            table.Columns[4].PreferredWidth = wordApp.CentimetersToPoints(2);
            table.Columns[5].PreferredWidth = wordApp.CentimetersToPoints(2);

            // Заголовки таблицы
            table.Cell(1, 1).Range.Text = "№";
            table.Cell(1, 2).Range.Text = "Наименование";
            table.Cell(1, 3).Range.Text = "Цена";
            table.Cell(1, 4).Range.Text = "Кол-во";
            table.Cell(1, 5).Range.Text = "Сумма";

            // Заполняем таблицу данными из cartItems
            for (int i = 0; i < cartItems.Rows.Count; i++)
            {
                DataRow row = cartItems.Rows[i];
                decimal price = Convert.ToDecimal(row["Price"]);
                int quantity = Convert.ToInt32(row["Quantity"]);
                decimal total = price * quantity;

                table.Cell(i + 2, 1).Range.Text = (i + 1).ToString();
                table.Cell(i + 2, 2).Range.Text = row["Name"].ToString();
                table.Cell(i + 2, 3).Range.Text = price.ToString("C");
                table.Cell(i + 2, 4).Range.Text = quantity.ToString();
                table.Cell(i + 2, 5).Range.Text = total.ToString("C");
            }

            // Форматирование таблицы
            table.Borders.Enable = 1;
            table.Rows[1].Range.Font.Bold = 1;

            // Выравнивание
            table.Columns[1].Cells.VerticalAlignment = Microsoft.Office.Interop.Word.WdCellVerticalAlignment.wdCellAlignVerticalCenter;
            table.Columns[3].Cells.VerticalAlignment = Microsoft.Office.Interop.Word.WdCellVerticalAlignment.wdCellAlignVerticalCenter;
            table.Columns[4].Cells.VerticalAlignment = Microsoft.Office.Interop.Word.WdCellVerticalAlignment.wdCellAlignVerticalCenter;
            table.Columns[5].Cells.VerticalAlignment = Microsoft.Office.Interop.Word.WdCellVerticalAlignment.wdCellAlignVerticalCenter;

            foreach (Microsoft.Office.Interop.Word.Cell cell in table.Columns[3].Cells)
            {
                cell.Range.ParagraphFormat.Alignment = Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphCenter;
            }

            foreach (Microsoft.Office.Interop.Word.Cell cell in table.Columns[4].Cells)
            {
                cell.Range.ParagraphFormat.Alignment = Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphCenter;
            }

            foreach (Microsoft.Office.Interop.Word.Cell cell in table.Columns[5].Cells)
            {
                cell.Range.ParagraphFormat.Alignment = Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphCenter;
            }
        }

        private void AddServiceInfoToWord(Microsoft.Office.Interop.Word.Document doc)
        {
            // Перемещаемся в конец документа
            Microsoft.Office.Interop.Word.Range range = doc.Range(doc.Content.End - 1, doc.Content.End - 1);

            // Добавляем отступ
            range.InsertParagraphAfter();
            range.InsertParagraphAfter();

            // Добавляем служебную информацию
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

            range.Text = $"Документ сгенерирован: {DateTime.Now:dd.MM.yyyy HH:mm:ss}\rСотрудник: {formattedname}";
            range.Font.Size = 10;
            range.Font.Italic = 1;
        }

        // Метод для расчета скидки (убедитесь, что он есть в вашем коде)
        private (decimal discountAmount, decimal discountPercent, decimal prepayment) CalculateDiscountValues()
        {
            decimal totalAmount = CalculateTotalAmount();
            decimal discountAmount = 0;
            decimal discountPercent = 0;
            decimal prepayment = 0;

            // Определяем размер скидки
            if (totalAmount >= 40000)
            {
                discountPercent = 15;
            }
            else if (totalAmount >= 30000)
            {
                discountPercent = 10;
            }

            // Рассчитываем суммы
            discountAmount = totalAmount * ((decimal)discountPercent / 100m);
            decimal amountAfterDiscount = totalAmount - discountAmount;
            prepayment = amountAfterDiscount * 0.1m;

            return (discountAmount, discountPercent, prepayment);
        }

        private decimal CalculateTotalAmount()
        {
            if (cartItems == null || cartItems.Rows.Count == 0)
                return 0;

            decimal total = 0;
            foreach (DataRow row in cartItems.Rows)
            {
                decimal price = Convert.ToDecimal(row["Price"]);
                int quantity = Convert.ToInt32(row["Quantity"]);
                total += price * quantity;
            }
            return total;
        }

        private bool allowClose = false;

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

        private void button2_Click(object sender, EventArgs e)
        {
            GenerateWordTicket();

            isWordGenerated = true;
            button1.Enabled = true;
            button2.Enabled = false;
            button3.Enabled = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (cartItems.Rows.Count == 0)
            {
                MessageBox.Show("Добавьте товары в заказ", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Проверяем, был ли создан Word документ
            if (!isWordGenerated)
            {
                MessageBox.Show("Сначала создайте Word документ для заказа", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (SaveOrderToDatabase())
                {
                    MessageBox.Show("Заказ успешно оформлен!", "Успех",
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // После сохранения заказа все кнопки становятся неактивными
                    button1.Enabled = false;
                    button2.Enabled = false;
                    button3.Enabled = false;

                    this.DialogResult = DialogResult.Yes;
                    allowClose = true;
                    this.Visible = false;
                    MainFormMeneger mainFormMeneger = new MainFormMeneger();
                    mainFormMeneger.ShowDialog();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка оформления заказа: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool SaveOrderToDatabase()
        {
            using (MySqlConnection con = new MySqlConnection(conString))
            {
                con.Open();
                MySqlTransaction transaction = con.BeginTransaction();

                try
                {
                    // 1. Получаем следующий номер заказа
                    int nextOrderNumber = Convert.ToInt32(label4.Text);

                    // 2. Получаем IdClient (если клиент существует) или создаем нового
                    int clientId = GetClient(con, transaction);

                    // 3. Получаем IdUser (текущий пользователь)
                    int userId = GetCurrentUserId(con, transaction);

                    // 4. Получаем IdEvent по названию мероприятия
                    int eventId = GetEventIdByName(con, transaction, orderData.Event);

                    // 5. Получаем IdSchedule по дате и времени
                    int scheduleId = GetScheduleId(con, transaction, orderData.Time);
                    // Преобразуем в date (без времени)
                    DateTime DateOfConclusion = Convert.ToDateTime(label6.Text).Date;
                    DateTime DateEvent = Convert.ToDateTime(label8.Text).Date;

                    int statusId = GetStatusId(con, transaction);

                    // 6. Рассчитываем суммы с округлением до 2 знаков после запятой
                    decimal totalAmount = Math.Round(Convert.ToDecimal(CleanDecimalString(label19.Text)), 2);
                    decimal discountAmount = Math.Round(Convert.ToDecimal(CleanDecimalString(label23.Text)), 2);
                    decimal finalAmount = Math.Round(totalAmount - discountAmount, 2);
                    decimal prepayment = Math.Round(Convert.ToDecimal(CleanDecimalString(label21.Text)), 2);

                    // 9. Создаем заказ в таблице NumberOrder
                    InsertMainOrder(con, transaction, nextOrderNumber, clientId, userId, eventId, scheduleId, statusId,
                                  totalAmount, discountAmount, finalAmount, prepayment, DateOfConclusion, DateEvent);

                    // 10. Сохраняем состав заказа в таблицу OrderComposition
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

        // Добавьте этот метод для очистки денежных строк
        private string CleanDecimalString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "0";

            // Убираем символы валюты, пробелы и другие нечисловые символы
            string cleaned = value.Replace("₽", "").Replace(" ", "").Replace(" ", "").Trim();

            return cleaned;
        }

        private int GetClient(MySqlConnection con, MySqlTransaction transaction)
        {
            // Проверяем, существует ли клиент с таким ФИО
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

        private int GetScheduleId(MySqlConnection con, MySqlTransaction transaction, string timeRange)
        {
            // Разбираем строку времени на начало и конец
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
                    // Создаем новое расписание
                    throw new Exception("Время не найдено в базе данных");
                }
            }
        }

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

                // Очищаем номер телефона от всех нецифровых символов
                string cleanPhone = CleanPhoneNumber(orderData.NumberPhone);
                cmd.Parameters.AddWithValue("@phone", cleanPhone);

                cmd.ExecuteNonQuery();
            }
        }

        // Метод для очистки номера телефона
        private string CleanPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return "";

            // Удаляем все символы, кроме цифр
            string cleanNumber = new string(phoneNumber.Where(char.IsDigit).ToArray());

            return cleanNumber;
        }

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
                    cmd.Parameters.AddWithValue("@idDish", article.PadLeft(6, '0')); // Форматируем как в примере
                    cmd.Parameters.AddWithValue("@count", quantity);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
