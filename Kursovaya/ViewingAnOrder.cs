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
        // Добавлено поле для отслеживания состояния обновления
        private bool isDataUpdated = false;

        public ViewingAnOrder(string orderId)
        {
            InitializeComponent();
            this.orderId = orderId;

            // Загружаем данные заказа
            LoadOrderData();
            FillFilter();

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

                                // Используем общий класс OrderData
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

            // Устанавливаем дополнительные расходы в TextBox (целое число)
            textBox1.Text = additionalExpenses > 0 ? additionalExpenses.ToString() : "";

            // Рассчитываем суммы с учетом дополнительных расходов
            CalculateTotalWithAdditionalExpenses();

            // Устанавливаем статус в комбобокс
            if (comboBox1.Items.Contains(orderData.Status))
            {
                comboBox1.SelectedItem = orderData.Status;
            }

            // Управляем кнопками в зависимости от статуса
            UpdateButtonsBasedOnStatus();
        }

        // Новый метод для управления кнопками
        private void UpdateButtonsBasedOnStatus()
        {
            if (orderData == null) return;

            // Определяем, можно ли редактировать статус
            bool isEditable = orderData.Status == "Принят";
            comboBox1.Enabled = isEditable;
            textBox1.Enabled = isEditable;

            // Управление кнопкой "Обновить данные" (button1)
            if (orderData.Status == "Принят")
            {
                // Для статуса "Принят" проверяем, изменился ли статус
                string currentStatus = comboBox1.SelectedItem?.ToString();
                bool isStatusChanged = currentStatus != orderData.Status;
                bool isValidNewStatus = currentStatus == "Оплачен" || currentStatus == "Отменен";

                button1.Enabled = isStatusChanged && isValidNewStatus;
                button1.Text = "Обновить данные";

                // Кнопка печати доступна только после обновления данных
                button2.Enabled = isDataUpdated;
            }
            else
            {
                // Для статусов "Оплачен" или "Отменен" кнопка обновления недоступна
                button1.Enabled = false;
                button1.Text = "Обновить данные";

                // Кнопка печати доступна сразу
                button2.Enabled = true;
            }

            // Визуальная индикация доступности кнопки печати
            if (button2.Enabled)
            {
                button2.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            }
            else
            {
                button2.BackColor = System.Drawing.Color.FromArgb(217, 152, 22); // Оригинальный
            }

            // Визуальная индикация для кнопки обновления
            if (button1.Enabled)
            {
                button2.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            }
            else
            {
                button1.BackColor = System.Drawing.Color.FromArgb(217, 152, 22); // Оригинальный
            }
        }

        private void CalculateTotalWithAdditionalExpenses()
        {
            if (orderData == null) return;

            // Если статус не "Принят", не пересчитываем
            if (orderData.Status != "Принят")
            {
                // Показываем исходные суммы с целыми числами
                label20.Text = $"{orderData.Prepayment:N0} ₽";
                label22.Text = $"{orderData.DiscountAmount:N0} ₽";
                label27.Text = $"{(orderData.FinalAmount > 0 ? orderData.FinalAmount : orderData.TotalAmount - orderData.DiscountAmount):N0} ₽";
                return;
            }

            // Получаем базовую сумму заказа (без дополнительных расходов)
            decimal baseTotalAmount = orderData.TotalAmount;
            decimal baseDiscountAmount = orderData.DiscountAmount;
            decimal baseFinalAmount = orderData.FinalAmount > 0 ? orderData.FinalAmount : baseTotalAmount - baseDiscountAmount;
            decimal basePrepayment = orderData.Prepayment;

            // Получаем дополнительные расходы из TextBox1
            int currentAdditionalExpenses = GetAdditionalExpenses();

            // Проверяем, не превышает ли общая сумма максимальное значение
            decimal maxTotalAllowed = 9999999; // 7 цифр
            decimal newFinalAmount = baseFinalAmount + currentAdditionalExpenses;

            if (newFinalAmount > maxTotalAllowed)
            {
                // Вычисляем максимально допустимые дополнительные расходы
                int maxAdditionalExpenses = (int)(maxTotalAllowed - baseFinalAmount);

                if (maxAdditionalExpenses < 0)
                {
                    maxAdditionalExpenses = 0;
                }

                // Обрезаем введенное значение
                currentAdditionalExpenses = maxAdditionalExpenses;

                // Обновляем текст в TextBox
                textBox1.Text = currentAdditionalExpenses.ToString();
                textBox1.SelectionStart = textBox1.Text.Length;

                // Показываем предупреждение
                MessageBox.Show($"Дополнительные расходы ограничены до {maxAdditionalExpenses:N0} ₽, чтобы общая сумма не превышала {maxTotalAllowed:N0} ₽",
                               "Ограничение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            // Сохраняем текущие дополнительные расходы
            additionalExpenses = currentAdditionalExpenses;

            // Рассчитываем новые суммы с учетом дополнительных расходов
            decimal newTotalAmount = baseTotalAmount + currentAdditionalExpenses;
            newFinalAmount = baseFinalAmount + currentAdditionalExpenses;

            // Предоплата может быть пересчитана пропорционально или оставлена как есть
            decimal newPrepayment = basePrepayment;

            // Отображаем суммы (целые числа с валютой)
            label20.Text = $"{newPrepayment:N0} ₽";
            label22.Text = $"{baseDiscountAmount:N0} ₽";
            label27.Text = $"{newFinalAmount:N0} ₽";
        }

        private void UpdateOrderStatusWithExpenses(string newStatus)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(conString))
                {
                    con.Open();

                    // Получаем дополнительные расходы (целое число)
                    int additionalExpenses = GetAdditionalExpenses();

                    // Рассчитываем новую общую сумму с учетом дополнительных расходов
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
                            // Обновляем данные в объекте
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

        private void SetupOrderItemsDataGridView()
        {
            dataGridView1.DataSource = orderItems;

            // Настройка колонок
            if (orderItems.Columns.Contains("Article"))
                dataGridView1.Columns["Article"].HeaderText = "Артикул";
            if (orderItems.Columns.Contains("Name"))
                dataGridView1.Columns["Name"].HeaderText = "Наименование";
            if (orderItems.Columns.Contains("Price"))
            {
                dataGridView1.Columns["Price"].HeaderText = "Цена";
                dataGridView1.Columns["Price"].DefaultCellStyle.Format = "N0"; // Целые числа
                dataGridView1.Columns["Price"].DefaultCellStyle.FormatProvider = System.Globalization.CultureInfo.GetCultureInfo("ru-RU");
            }
            if (orderItems.Columns.Contains("Count"))
                dataGridView1.Columns["Count"].HeaderText = "Количество";
            if (orderItems.Columns.Contains("Total"))
            {
                dataGridView1.Columns["Total"].HeaderText = "Сумма";
                dataGridView1.Columns["Total"].DefaultCellStyle.Format = "N0"; // Целые числа
                dataGridView1.Columns["Total"].DefaultCellStyle.FormatProvider = System.Globalization.CultureInfo.GetCultureInfo("ru-RU");
            }

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

        private void button1_Click(object sender, EventArgs e)
        {
            if (orderData == null) return;

            // Проверяем, что статус изменился и не остался "Принят"
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

            // Сохраняем изменения статуса в базу данных
            UpdateOrderStatusWithExpenses(newStatus);

            // Обновляем состояние - данные обновлены
            isDataUpdated = true;

            // Обновляем управление кнопками
            UpdateButtonsBasedOnStatus();

            MessageBox.Show("Данные успешно обновлены.", "Успех",
                          MessageBoxButtons.OK, MessageBoxIcon.Information);

            // После сохранения НЕ возвращаемся к предыдущей форме
            // Оставляем пользователя на текущей форме для возможной печати
        }

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

        void FillFilter()
        {
            MySqlConnection con = new MySqlConnection(conString);
            con.Open();

            MySqlCommand cmd = new MySqlCommand(@"SELECT * FROM CafeActivities.Status;", con);
            MySqlDataReader rdr = cmd.ExecuteReader();

            comboBox1.Items.Clear();

            while (rdr.Read())
            {
                comboBox1.Items.Add(rdr[1].ToString());
            }

            comboBox1.SelectedIndex = 2;

            con.Close();
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox tb = (TextBox)sender;

            // Разрешаем управляющие символы (Backspace, Delete, Enter и т.д.)
            if (char.IsControl(e.KeyChar))
                return;

            // Запрещаем точку и запятую
            if (e.KeyChar == '.' || e.KeyChar == ',')
            {
                e.Handled = true;
                return;
            }

            // Разрешаем только цифры
            if (char.IsDigit(e.KeyChar))
            {
                // Получаем текущий текст без выделенной части
                string currentText = tb.Text.Substring(0, tb.SelectionStart) +
                                     tb.Text.Substring(tb.SelectionStart + tb.SelectionLength);

                // Проверяем, не превысит ли длина 7 символов после вставки
                if (currentText.Length >= 7)
                {
                    e.Handled = true;
                    return;
                }

                e.Handled = false;
                return;
            }

            // Запрещаем все остальные символы
            e.Handled = true;
        }

        private void ValidateAndFormatAdditionalExpenses()
        {
            if (string.IsNullOrWhiteSpace(textBox1.Text))
            {
                return;
            }

            // Оставляем только цифры
            string cleanText = new string(textBox1.Text.Where(char.IsDigit).ToArray());

            // Ограничиваем длину до 7 цифр
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

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            // Проверяем и форматируем введенное значение
            ValidateAndFormatAdditionalExpenses();

            // Пересчитываем суммы при изменении дополнительных расходов
            CalculateTotalWithAdditionalExpenses();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Обновляем состояние кнопок при изменении статуса
            UpdateButtonsBasedOnStatus();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (orderData == null || orderItems == null)
            {
                MessageBox.Show("Данные заказа не загружены", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            decimal additionalExpenses = GetAdditionalExpenses();
            // Передаем this как previousForm, но НЕ закрываем форму здесь
            SelectFormPrint selectFormPrint = new SelectFormPrint(orderData, orderItems, DocumentType.Final, this, additionalExpenses, this);
            selectFormPrint.ShowDialog();
            // НЕ вызываем this.Close() - форма закроется сама при необходимости
            // this.Close(); // УБРАТЬ ИЛИ ЗАКОММЕНТИРОВАТЬ
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

                // Создаем Word Application
                wordApp = new Microsoft.Office.Interop.Word.Application();
                wordApp.Visible = true;

                string templatePath = GetTemplatePath();
                doc = wordApp.Documents.Open(templatePath, ReadOnly: false);
                doc.Activate();

                // Используем данные из заказа с учетом дополнительных расходов
                decimal additionalExpenses = GetAdditionalExpenses();
                decimal totalAmount = orderData.TotalAmount + additionalExpenses;
                decimal discountAmount = orderData.DiscountAmount;
                decimal finalAmount = (orderData.FinalAmount > 0 ? orderData.FinalAmount : orderData.TotalAmount - discountAmount) + additionalExpenses;
                decimal prepayment = orderData.Prepayment;

                // Рассчитываем процент скидки от новой общей суммы
                decimal discountPercent = totalAmount > 0 ? (discountAmount / totalAmount) * 100 : 0;

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
                FillBookmark(doc, "Discount", Math.Round(discountPercent).ToString());
                FillBookmark(doc, "AddExpenses", additionalExpenses.ToString("C"));

                ReplaceExampleTableWithActualData(doc, wordApp);
                AddServiceInfoToWord(doc);

                // Сохраняем изменения
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
                        // Не закрываем документ, чтобы пользователь мог его просмотреть
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(doc);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при освобождении ресурсов: {ex.Message}");
                }
            }
        }

        private void AddServiceInfoToWord(Microsoft.Office.Interop.Word.Document doc)
        {
            Microsoft.Office.Interop.Word.Range range = doc.Range(doc.Content.End - 1, doc.Content.End - 1);
            range.InsertParagraphAfter();
            range.InsertParagraphAfter();

            // Получаем информацию о текущем пользователе (директоре)
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

            // Форматируем имя сотрудника, который оформил заказ
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

            // Добавляем служебную информацию с текущим временем и обоими сотрудниками
            range.Text = $"Документ сгенерирован: {DateTime.Now:dd.MM.yyyy HH:mm:ss}\rСотрудник: {formattedname}\rЗаказ был оформлен: {formattedOrderCreator}";
            range.Font.Size = 10;
            range.Font.Italic = 1;
        }

        // Метод для замены примера таблицы на актуальные данные
        private void ReplaceExampleTableWithActualData(Microsoft.Office.Interop.Word.Document doc, Microsoft.Office.Interop.Word.Application wordApp)
        {
            try
            {
                // Находим таблицу с примером товара (первую таблицу в документе)
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

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void ViewingAnOrder_Load(object sender, EventArgs e)
        {

        }

        private void textBox1_Leave(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(textBox1.Text))
            {
                // Убираем все нецифровые символы
                string cleanText = new string(textBox1.Text.Where(char.IsDigit).ToArray());

                if (!string.IsNullOrEmpty(cleanText))
                {
                    // Ограничиваем до 7 цифр
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

            // Пересчитываем с проверкой лимита
            CalculateTotalWithAdditionalExpenses();
        }

        private void ValidateAndPasteAdditionalExpenses()
        {
            if (Clipboard.ContainsText())
            {
                string clipboardText = Clipboard.GetText();

                // Оставляем только цифры
                string numericText = new string(clipboardText.Where(char.IsDigit).ToArray());

                // Ограничиваем длину до 7 цифр
                if (numericText.Length > 7)
                {
                    numericText = numericText.Substring(0, 7);
                }

                if (!string.IsNullOrEmpty(numericText))
                {
                    textBox1.Text = numericText;
                    textBox1.SelectionStart = textBox1.Text.Length;

                    // Пересчитываем суммы
                    CalculateTotalWithAdditionalExpenses();
                }
            }
        }

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