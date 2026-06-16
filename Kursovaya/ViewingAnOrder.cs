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
    public partial class ViewingAnOrder : Form
    {
        string conString = $"host={Properties.Settings.Default.host};uid={Properties.Settings.Default.uid};pwd={Properties.Settings.Default.pwd};database={Properties.Settings.Default.database};";
        private string orderId;
        private int rowCount = 0;
        private DataTable orderItems;
        private OrderData orderData;
        private decimal additionalExpenses = 0;
        private bool isDataUpdated = false;

        public ViewingAnOrder(string orderId)
        {
            InitializeComponent();
            this.orderId = orderId;

            LoadOrderData();

            button1.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button2.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button3.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            textBox1.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            comboBox1.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
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

                                int finalAmount = reader["FinalAmount"] != DBNull.Value ?
                                    Convert.ToInt32(reader["FinalAmount"]) : 0;
                                int baseTotalAmount = reader["TotalAmount"] != DBNull.Value ?
                                    Convert.ToInt32(reader["TotalAmount"]) : 0;
                                int discountAmount = reader["DiscountAmount"] != DBNull.Value ?
                                    Convert.ToInt32(reader["DiscountAmount"]) : 0;

                                decimal baseFinalAmount = finalAmount > 0 ? finalAmount : baseTotalAmount - discountAmount;
                                additionalExpenses = finalAmount - (baseTotalAmount - discountAmount);
                                if (additionalExpenses < 0) additionalExpenses = 0;

                                orderData = new OrderData
                                {
                                    NumberOrder = reader["NumberOrder"].ToString(),
                                    DateOrder = Convert.ToDateTime(reader["DateOfConclusion"]).ToString("dd.MM.yyyy"),
                                    Date = Convert.ToDateTime(reader["DateEvent"]).ToString("dd.MM.yyyy"),
                                    Time = timeRange,
                                    NameClient = reader["ClientName"].ToString(),
                                    NumberPhone = reader["NumberPhoneClient"].ToString(),
                                    Event = reader["EventName"].ToString(),
                                    TotalAmount = Convert.ToInt32(reader["TotalAmount"]),
                                    DiscountAmount = Convert.ToInt32(reader["DiscountAmount"]),
                                    NameUser = reader["NameUser"] != DBNull.Value ? reader["NameUser"].ToString() : "Не указан",
                                    FinalAmount = Convert.ToInt32(reader["FinalAmount"]),
                                    Prepayment = Convert.ToInt32(reader["Prepayment"]),
                                    Status = reader["OrderStatus"].ToString()
                                };

                                isDataUpdated = (orderData.Status == "Оплачен" || orderData.Status == "Отменен");
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

                FillFilter();
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

            textBox1.Text = additionalExpenses > 0 ? additionalExpenses.ToString() : "";

            CalculateTotalWithAdditionalExpenses();

            if (comboBox1.Items.Contains(orderData.Status))
            {
                comboBox1.SelectedItem = orderData.Status;
            }

            UpdateButtonsBasedOnStatus();
        }

        //Управление кнопками в зависимости от статуса
        private void UpdateButtonsBasedOnStatus()
        {
            if (orderData == null) return;

            bool isEditable = orderData.Status == "Принят";
            comboBox1.Enabled = isEditable;
            textBox1.Enabled = isEditable;

            if (orderData.Status == "Принят")
            {
                string currentStatus = comboBox1.SelectedItem?.ToString();
                bool isStatusChanged = currentStatus != orderData.Status;
                bool isValidNewStatus = currentStatus == "Оплачен" || currentStatus == "Отменен";

                button1.Enabled = isStatusChanged && isValidNewStatus;
                button1.Text = "Обновить данные";

                button2.Enabled = isDataUpdated;
            }
            else
            {
                button1.Enabled = false;
                button1.Text = "Обновить данные";

                button2.Enabled = true;
            }

            button2.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button1.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
        }

        //Расчет суммы с учетом дополнительных расходов
        private void CalculateTotalWithAdditionalExpenses()
        {
            if (orderData == null) return;

            if (orderData.Status != "Принят")
            {
                label20.Text = $"{orderData.Prepayment:N0} ₽";
                label22.Text = $"{orderData.DiscountAmount:N0} ₽";
                label27.Text = $"{(orderData.FinalAmount > 0 ? orderData.FinalAmount : orderData.TotalAmount - orderData.DiscountAmount):N0} ₽";
                return;
            }

            decimal baseTotalAmount = orderData.TotalAmount;
            decimal baseDiscountAmount = orderData.DiscountAmount;
            decimal baseFinalAmount = orderData.FinalAmount > 0 ? orderData.FinalAmount : baseTotalAmount - baseDiscountAmount;
            decimal basePrepayment = orderData.Prepayment;

            int currentAdditionalExpenses = GetAdditionalExpenses();

            decimal maxTotalAllowed = 9999999;
            decimal newFinalAmount = baseFinalAmount + currentAdditionalExpenses;

            if (newFinalAmount > maxTotalAllowed)
            {
                int maxAdditionalExpenses = (int)(maxTotalAllowed - baseFinalAmount);

                if (maxAdditionalExpenses < 0)
                {
                    maxAdditionalExpenses = 0;
                }

                currentAdditionalExpenses = maxAdditionalExpenses;

                textBox1.Text = currentAdditionalExpenses.ToString();
                textBox1.SelectionStart = textBox1.Text.Length;

                MessageBox.Show($"Дополнительные расходы ограничены до {maxAdditionalExpenses:N0} ₽, чтобы общая сумма не превышала {maxTotalAllowed:N0} ₽",
                               "Ограничение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            additionalExpenses = currentAdditionalExpenses;

            decimal newTotalAmount = baseTotalAmount + currentAdditionalExpenses;
            newFinalAmount = baseFinalAmount + currentAdditionalExpenses;
            decimal newPrepayment = basePrepayment;

            label20.Text = $"{newPrepayment:N0} ₽";
            label22.Text = $"{baseDiscountAmount:N0} ₽";
            label27.Text = $"{newFinalAmount:N0} ₽";
        }

        //Обновление статуса заказа с расходами
        private void UpdateOrderStatusWithExpenses(string newStatus)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(conString))
                {
                    con.Open();

                    int additionalExpenses = GetAdditionalExpenses();

                    int baseFinalAmount = orderData.FinalAmount > 0 ? orderData.FinalAmount :
                                             orderData.TotalAmount - orderData.DiscountAmount;
                    int newFinalAmount = baseFinalAmount + additionalExpenses;

                    string updateQuery = @"
                UPDATE Orders 
                SET 
                    IdStatus = (SELECT IDstatus FROM Status WHERE Status = @status),
                    PriceAll = @finalAmount
                WHERE NumberOrder = @orderId";

                    using (MySqlCommand cmd = new MySqlCommand(updateQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@status", newStatus);
                        cmd.Parameters.AddWithValue("@finalAmount", newFinalAmount);
                        cmd.Parameters.AddWithValue("@orderId", orderId);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            orderData.Status = newStatus;
                            orderData.FinalAmount = newFinalAmount;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении статуса: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //Получение дополнительных расходов
        private int GetAdditionalExpenses()
        {
            if (string.IsNullOrWhiteSpace(textBox1.Text))
                return 0;

            if (int.TryParse(textBox1.Text, out int expenses))
            {
                return expenses >= 0 ? expenses : 0;
            }

            return 0;
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
            {
                dataGridView1.Columns["Price"].HeaderText = "Цена";
                dataGridView1.Columns["Price"].DefaultCellStyle.Format = "N0";
                dataGridView1.Columns["Price"].DefaultCellStyle.FormatProvider = System.Globalization.CultureInfo.GetCultureInfo("ru-RU");
            }
            if (orderItems.Columns.Contains("Count"))
                dataGridView1.Columns["Count"].HeaderText = "Количество";
            if (orderItems.Columns.Contains("Total"))
            {
                dataGridView1.Columns["Total"].HeaderText = "Сумма";
                dataGridView1.Columns["Total"].DefaultCellStyle.Format = "N0";
                dataGridView1.Columns["Total"].DefaultCellStyle.FormatProvider = System.Globalization.CultureInfo.GetCultureInfo("ru-RU");
            }

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

        //Обработчик кнопки обновления данных
        private void button1_Click(object sender, EventArgs e)
        {
            if (orderData == null) return;

            string newStatus = comboBox1.SelectedItem?.ToString();

            if (newStatus == orderData.Status)
            {
                MessageBox.Show("Нет изменений для сохранения", "Информация",
                              MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (newStatus == "Принят")
            {
                MessageBox.Show("Для завершения обработки заказа выберите статус 'Оплачен' или 'Отменен'", "Информация",
                              MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            UpdateOrderStatusWithExpenses(newStatus);

            isDataUpdated = true;

            UpdateButtonsBasedOnStatus();

            MessageBox.Show("Данные успешно обновлены.", "Успех",
                          MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        //Обработчик кнопки возврата
        private void button3_Click(object sender, EventArgs e)
        {
            allowClose = true;
            this.Visible = false;
            ViewingOrdersForMeneger viewingOrdersForMeneger = new ViewingOrdersForMeneger();
            viewingOrdersForMeneger.ShowDialog();
            this.Close();
        }

        //Обработчик закрытия формы
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

        //Заполнение выпадающего списка статусов
        void FillFilter()
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(conString))
                {
                    con.Open();

                    string query = @"SELECT Status FROM CafeActivities.Status ORDER BY Status";

                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    using (MySqlDataReader rdr = cmd.ExecuteReader())
                    {
                        comboBox1.Items.Clear();

                        List<string> allStatuses = new List<string>();

                        while (rdr.Read())
                        {
                            allStatuses.Add(rdr["Status"].ToString());
                        }

                        if (orderData != null && !string.IsNullOrEmpty(orderData.Status))
                        {
                            foreach (string status in allStatuses)
                            {
                                if (status != orderData.Status)
                                {
                                    comboBox1.Items.Add(status);
                                }
                            }
                        }
                        else
                        {
                            foreach (string status in allStatuses)
                            {
                                comboBox1.Items.Add(status);
                            }
                        }
                    }

                    if (comboBox1.Items.Count > 0)
                    {
                        comboBox1.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке статусов: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);

                comboBox1.Items.Clear();

                string[] fallbackStatuses = { "Оплачен", "Отменен" };

                if (orderData != null && !string.IsNullOrEmpty(orderData.Status))
                {
                    foreach (string status in fallbackStatuses)
                    {
                        if (status != orderData.Status)
                        {
                            comboBox1.Items.Add(status);
                        }
                    }
                }
                else
                {
                    comboBox1.Items.AddRange(fallbackStatuses);
                }

                if (comboBox1.Items.Count > 0)
                    comboBox1.SelectedIndex = 0;
            }
        }

        //Ограничение ввода в поле дополнительных расходов
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox tb = (TextBox)sender;

            if (char.IsControl(e.KeyChar))
                return;

            if (e.KeyChar == '.' || e.KeyChar == ',')
            {
                e.Handled = true;
                return;
            }

            if (char.IsDigit(e.KeyChar))
            {
                string currentText = tb.Text.Substring(0, tb.SelectionStart) +
                                     tb.Text.Substring(tb.SelectionStart + tb.SelectionLength);

                if (currentText.Length >= 7)
                {
                    e.Handled = true;
                    return;
                }

                e.Handled = false;
                return;
            }

            e.Handled = true;
        }

        //Валидация и форматирование дополнительных расходов
        private void ValidateAndFormatAdditionalExpenses()
        {
            if (string.IsNullOrWhiteSpace(textBox1.Text))
            {
                return;
            }

            string cleanText = new string(textBox1.Text.Where(char.IsDigit).ToArray());

            if (cleanText.Length > 7)
            {
                cleanText = cleanText.Substring(0, 7);
            }

            if (!string.IsNullOrEmpty(cleanText))
            {
                if (textBox1.Text != cleanText)
                {
                    textBox1.Text = cleanText;
                    textBox1.SelectionStart = textBox1.Text.Length;
                }
            }
            else
            {
                textBox1.Text = "";
            }
        }

        //Обработчик изменения текста в поле дополнительных расходов
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            ValidateAndFormatAdditionalExpenses();

            CalculateTotalWithAdditionalExpenses();
        }

        //Обработчик изменения выбранного статуса
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateButtonsBasedOnStatus();
        }

        //Обработчик кнопки печати
        private void button2_Click(object sender, EventArgs e)
        {
            if (orderData == null || orderItems == null)
            {
                MessageBox.Show("Данные заказа не загружены", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            decimal additionalExpenses = GetAdditionalExpenses();
            SelectFormPrint selectFormPrint = new SelectFormPrint(orderData, orderItems, DocumentType.Final, this, additionalExpenses, this);
            selectFormPrint.ShowDialog();
        }

        //Получение пути к шаблону
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

        //Заполнение закладки в документе Word
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

        //Генерация Word документа
        private void GenerateWordTicket()
        {
            Microsoft.Office.Interop.Word.Application wordApp = null;
            Microsoft.Office.Interop.Word.Document doc = null;

            try
            {
                if (orderData == null || orderItems == null)
                {
                    MessageBox.Show("Данные заказа не загружены", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                wordApp = new Microsoft.Office.Interop.Word.Application();
                wordApp.Visible = true;

                string templatePath = GetTemplatePath();
                doc = wordApp.Documents.Open(templatePath, ReadOnly: false);
                doc.Activate();

                decimal additionalExpenses = GetAdditionalExpenses();
                decimal totalAmount = orderData.TotalAmount + additionalExpenses;
                decimal discountAmount = orderData.DiscountAmount;
                decimal finalAmount = (orderData.FinalAmount > 0 ? orderData.FinalAmount : orderData.TotalAmount - discountAmount) + additionalExpenses;
                decimal prepayment = orderData.Prepayment;

                decimal discountPercent = totalAmount > 0 ? (discountAmount / totalAmount) * 100 : 0;

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
                FillBookmark(doc, "Discount", Math.Round(discountPercent).ToString());
                FillBookmark(doc, "AddExpenses", additionalExpenses.ToString("C"));

                ReplaceExampleTableWithActualData(doc, wordApp);
                AddServiceInfoToWord(doc);

                doc.Save();

                MessageBox.Show("Документ заказа создан.", "Успех",
                              MessageBoxButtons.OK, MessageBoxIcon.Information);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании Word-документа: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                try
                {
                    if (doc != null)
                    {
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(doc);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при освобождении ресурсов: {ex.Message}");
                }
            }
        }

        //Добавление служебной информации в Word документ
        private void AddServiceInfoToWord(Microsoft.Office.Interop.Word.Document doc)
        {
            Microsoft.Office.Interop.Word.Range range = doc.Range(doc.Content.End - 1, doc.Content.End - 1);
            range.InsertParagraphAfter();
            range.InsertParagraphAfter();

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

            string orderCreatorName = orderData.NameUser;
            string formattedOrderCreator = orderCreatorName;

            string[] creatorParts = orderCreatorName.Split(' ');
            if (creatorParts.Length == 3)
            {
                string lastnameCreator = creatorParts[0];
                string firstnameCreator = creatorParts[1].Substring(0, 1);
                string middleCreator = creatorParts[2].Substring(0, 1);
                formattedOrderCreator = $"{lastnameCreator} {firstnameCreator}.{middleCreator}.";
            }

            range.Text = $"Документ сгенерирован: {DateTime.Now:dd.MM.yyyy HH:mm:ss}\rСотрудник: {formattedname}\rЗаказ был оформлен: {formattedOrderCreator}";
            range.Font.Size = 10;
            range.Font.Italic = 1;
        }

        //Замена примера таблицы на актуальные данные
        private void ReplaceExampleTableWithActualData(Microsoft.Office.Interop.Word.Document doc, Microsoft.Office.Interop.Word.Application wordApp)
        {
            try
            {
                if (doc.Tables.Count > 0)
                {
                    Microsoft.Office.Interop.Word.Table exampleTable = doc.Tables[1];
                    Microsoft.Office.Interop.Word.Range tableRange = exampleTable.Range;
                    exampleTable.Delete();
                    InsertActualOrderTable(doc, wordApp, tableRange);
                }
                else
                {
                    InsertActualOrderTable(doc, wordApp, null);
                }
            }
            catch (Exception ex)
            {
                InsertActualOrderTable(doc, wordApp, null);
            }
        }

        //Вставка таблицы с актуальными данными заказа
        private void InsertActualOrderTable(Microsoft.Office.Interop.Word.Document doc, Microsoft.Office.Interop.Word.Application wordApp, Microsoft.Office.Interop.Word.Range targetRange)
        {
            if (orderItems.Rows.Count == 0)
            {
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

            if (targetRange != null)
            {
                table = doc.Tables.Add(targetRange, orderItems.Rows.Count + 1, 5);
            }
            else
            {
                table = doc.Tables.Add(doc.Range(doc.Content.End - 1), orderItems.Rows.Count + 1, 5);
            }

            table.PreferredWidth = wordApp.CentimetersToPoints(16);
            table.AllowAutoFit = true;

            table.Columns[1].PreferredWidth = wordApp.CentimetersToPoints(1);
            table.Columns[2].PreferredWidth = wordApp.CentimetersToPoints(8);
            table.Columns[3].PreferredWidth = wordApp.CentimetersToPoints(2);
            table.Columns[4].PreferredWidth = wordApp.CentimetersToPoints(2);
            table.Columns[5].PreferredWidth = wordApp.CentimetersToPoints(2);

            table.Cell(1, 1).Range.Text = "№";
            table.Cell(1, 2).Range.Text = "Наименование";
            table.Cell(1, 3).Range.Text = "Цена";
            table.Cell(1, 4).Range.Text = "Кол-во";
            table.Cell(1, 5).Range.Text = "Сумма";

            for (int i = 0; i < orderItems.Rows.Count; i++)
            {
                DataRow row = orderItems.Rows[i];
                decimal price = Convert.ToDecimal(row["Price"]);
                int quantity = Convert.ToInt32(row["Count"]);
                decimal total = price * quantity;

                table.Cell(i + 2, 1).Range.Text = (i + 1).ToString();
                table.Cell(i + 2, 2).Range.Text = row["Name"].ToString();
                table.Cell(i + 2, 3).Range.Text = price.ToString("C");
                table.Cell(i + 2, 4).Range.Text = quantity.ToString();
                table.Cell(i + 2, 5).Range.Text = total.ToString("C");
            }

            table.Borders.Enable = 1;
            table.Rows[1].Range.Font.Bold = 1;

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

        private void label3_Click(object sender, EventArgs e) { }
        private void label4_Click(object sender, EventArgs e) { }
        private void label6_Click(object sender, EventArgs e) { }

        private void ViewingAnOrder_Load(object sender, EventArgs e) { }

        //Обработчик потери фокуса полем дополнительных расходов
        private void textBox1_Leave(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(textBox1.Text))
            {
                string cleanText = new string(textBox1.Text.Where(char.IsDigit).ToArray());

                if (!string.IsNullOrEmpty(cleanText))
                {
                    if (cleanText.Length > 7)
                    {
                        cleanText = cleanText.Substring(0, 7);
                    }

                    textBox1.Text = cleanText;
                }
                else
                {
                    textBox1.Text = "";
                }
            }

            CalculateTotalWithAdditionalExpenses();
        }

        //Валидация и вставка дополнительных расходов
        private void ValidateAndPasteAdditionalExpenses()
        {
            if (Clipboard.ContainsText())
            {
                string clipboardText = Clipboard.GetText();

                string numericText = new string(clipboardText.Where(char.IsDigit).ToArray());

                if (numericText.Length > 7)
                {
                    numericText = numericText.Substring(0, 7);
                }

                if (!string.IsNullOrEmpty(numericText))
                {
                    textBox1.Text = numericText;
                    textBox1.SelectionStart = textBox1.Text.Length;

                    CalculateTotalWithAdditionalExpenses();
                }
            }
        }

        //Обработчик нажатия клавиш в поле дополнительных расходов
        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.V)
            {
                e.Handled = true;
                ValidateAndPasteAdditionalExpenses();
            }
        }
    }
}