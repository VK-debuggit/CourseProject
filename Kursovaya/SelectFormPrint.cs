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
using Microsoft.Office.Interop.Word;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using Microsoft.Win32;

namespace Kursovaya
{
    // Перечисление типов документов
    public enum DocumentType
    {
        Preliminary,  // Предварительный документ (firstblank.docx)
        Final        // Окончательный документ (secondblank.docx)
    }

    public partial class SelectFormPrint : Form
    {
        private System.Data.DataTable _cartItems;
        private OrderData _orderData;
        private decimal _discountAmountValue;
        private DocumentType _documentType;
        private ViewingAnOrder _viewingAnOrderForm;
        private decimal _additionalExpenses;
        private Form _previousForm;

        private bool allowClose = false;

        // Конструктор для предварительного документа (из ViewingAnOrderForMeneger)
        public SelectFormPrint(System.Data.DataTable cartItems, OrderData orderData, decimal discountAmountValue, DocumentType type, Form previousForm = null)
        {
            InitializeComponent();
            this._cartItems = cartItems;
            this._orderData = orderData;
            this._discountAmountValue = discountAmountValue;
            this._documentType = type;
            this._viewingAnOrderForm = null;
            this._previousForm = previousForm;
            button1.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button2.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button3.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
        }

        // Конструктор для окончательного документа (из ViewingAnOrder)
        public SelectFormPrint(OrderData orderData, System.Data.DataTable orderItems, DocumentType type, ViewingAnOrder parentForm, decimal additionalExpenses, Form previousForm = null)
        {
            InitializeComponent();
            this._orderData = orderData;
            this._cartItems = orderItems;
            this._documentType = type;
            this._viewingAnOrderForm = parentForm;
            this._additionalExpenses = additionalExpenses;
            this._previousForm = previousForm;
            button1.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button2.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button3.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
        }

        // ========== КНОПКИ НАВИГАЦИИ ==========

        private void button3_Click(object sender, EventArgs e)
        {
            allowClose = true;
            this.Close();
        }

        private void SelectFormPrint_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.ApplicationExitCall)
                return;

            if (!allowClose)
            {
                e.Cancel = true;
                this.Hide();

                if (_previousForm != null && !_previousForm.IsDisposed)
                {
                    _previousForm.Show();
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (_documentType == DocumentType.Preliminary)
            {
                GeneratePreliminaryWordTicket();
                this.DialogResult = DialogResult.OK;
                allowClose = true;
                this.Visible = false;
                MakingAnOrder makingAnOrder1 = new MakingAnOrder();
                makingAnOrder1.ShowDialog();
                this.Close();
            }
            else
            {
                GenerateFinalWordTicket();
                this.DialogResult = DialogResult.OK;
                allowClose = true;
                this.Close();
            }

            this.DialogResult = DialogResult.OK;
            allowClose = true;
            this.Visible = false;
            MakingAnOrder makingAnOrder = new MakingAnOrder();
            makingAnOrder.ShowDialog();
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (_documentType == DocumentType.Preliminary)
            {
                GeneratePreliminaryPDFTicket();
                this.DialogResult = DialogResult.OK;
                allowClose = true;
                this.Visible = false;
                MakingAnOrder makingAnOrder = new MakingAnOrder();
                makingAnOrder.ShowDialog();
                this.Close();
            }
            else
            {
                GenerateFinalPDFTicket();
                this.DialogResult = DialogResult.OK;
                allowClose = true;
                this.Close();
            }
        }

        // ========== ГЕНЕРАЦИЯ PDF С АВТОСОХРАНЕНИЕМ ==========

        // Генерация PDF предварительного документа с автосохранением
        private void GeneratePreliminaryPDFTicket()
        {
            try
            {
                // Создаем стандартную папку для отчетов
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string reportsFolder = Path.Combine(documentsPath, "CafeOrderReports");

                if (!Directory.Exists(reportsFolder))
                {
                    Directory.CreateDirectory(reportsFolder);
                }

                string fileName = $"Предварительный_документ_заказ_{_orderData.NumberOrder}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                string filePath = Path.Combine(reportsFolder, fileName);

                decimal totalAmount = CalculateTotalAmount(_cartItems);
                (decimal discountAmount, decimal discountPercent, decimal prepayment) = CalculateDiscountValues(totalAmount);
                decimal finalAmount = totalAmount - discountAmount;

                CreatePDFDocument(filePath, totalAmount, discountAmount, discountPercent, prepayment, finalAmount, isPreliminary: true);

                MessageBox.Show(
                    $"PDF-документ предварительного заказа успешно создан!\n\n" +
                    $"Файл сохранен:\n{filePath}",
                    "Успех",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                OpenPDFFile(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании PDF-документа: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Генерация PDF итогового документа с автосохранением
        private void GenerateFinalPDFTicket()
        {
            try
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string reportsFolder = Path.Combine(documentsPath, "CafeOrderReports");

                if (!Directory.Exists(reportsFolder))
                {
                    Directory.CreateDirectory(reportsFolder);
                }

                string fileName = $"Итоговый_документ_заказ_{_orderData.NumberOrder}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                string filePath = Path.Combine(reportsFolder, fileName);

                decimal totalAmount = _orderData.TotalAmount + _additionalExpenses;
                decimal discountAmount = _orderData.DiscountAmount;
                decimal discountPercent = totalAmount > 0 ? (discountAmount / totalAmount) * 100 : 0;
                decimal finalAmount = (_orderData.FinalAmount > 0 ? _orderData.FinalAmount : _orderData.TotalAmount - discountAmount) + _additionalExpenses;
                decimal prepayment = _orderData.Prepayment;

                CreatePDFDocument(filePath, totalAmount, discountAmount, discountPercent, prepayment, finalAmount, isPreliminary: false);

                MessageBox.Show(
                    $"PDF-документ итогового заказа успешно создан!\n\n" +
                    $"Файл сохранен:\n{filePath}",
                    "Успех",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                OpenPDFFile(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании PDF-документа: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Основной метод создания PDF документа
        private void CreatePDFDocument(string filePath, decimal totalAmount, decimal discountAmount,
            decimal discountPercent, decimal prepayment, decimal finalAmount, bool isPreliminary)
        {
            using (PdfDocument document = new PdfDocument())
            {
                PdfPage page = document.AddPage();
                page.Width = XUnit.FromMillimeter(210);
                page.Height = XUnit.FromMillimeter(297);

                using (XGraphics gfx = XGraphics.FromPdfPage(page))
                {
                    XFont titleFont = new XFont("Arial", 18, XFontStyle.Bold);
                    XFont regularFont = new XFont("Arial", 10, XFontStyle.Regular);
                    XFont boldFont = new XFont("Arial", 10, XFontStyle.Bold);
                    XFont infoFont = new XFont("Arial", 8, XFontStyle.Italic);

                    float yPosition = 50;

                    // Заголовок
                    gfx.DrawString("БЛАНК ЗАКАЗА", titleFont, XBrushes.Black,
                        new XRect(0, yPosition, page.Width, 30), XStringFormats.TopCenter);
                    yPosition += 40;

                    // Информация о заказе
                    gfx.DrawString($"Номер заказа: {_orderData.NumberOrder}", regularFont, XBrushes.Black,
                        new XRect(50, yPosition, page.Width - 100, 20), XStringFormats.TopCenter);
                    yPosition += 20;

                    gfx.DrawString($"Дата создания: {_orderData.DateOrder}", regularFont, XBrushes.Black,
                        new XRect(50, yPosition, page.Width - 100, 20), XStringFormats.TopCenter);
                    yPosition += 20;

                    gfx.DrawString($"Клиент: {_orderData.NameClient}", regularFont, XBrushes.Black,
                        new XRect(50, yPosition, page.Width - 100, 20), XStringFormats.TopCenter);
                    yPosition += 20;

                    gfx.DrawString($"Телефон: {_orderData.NumberPhone}", regularFont, XBrushes.Black,
                        new XRect(50, yPosition, page.Width - 100, 20), XStringFormats.TopCenter);
                    yPosition += 20;

                    gfx.DrawString($"Мероприятие: {_orderData.Event}", regularFont, XBrushes.Black,
                        new XRect(50, yPosition, page.Width - 100, 20), XStringFormats.TopCenter);
                    yPosition += 20;

                    gfx.DrawString($"Дата проведения: {_orderData.Date}", regularFont, XBrushes.Black,
                        new XRect(50, yPosition, page.Width - 100, 20), XStringFormats.TopCenter);
                    yPosition += 20;

                    gfx.DrawString($"Время: {_orderData.Time}", regularFont, XBrushes.Black,
                        new XRect(50, yPosition, page.Width - 100, 20), XStringFormats.TopCenter);
                    yPosition += 50;

                    // Рисуем таблицу и получаем высоту, которую она заняла
                    float tableBottomY = DrawOrderTable(gfx, _cartItems, page, yPosition);

                    // Устанавливаем позицию после таблицы
                    yPosition = tableBottomY + 20;

                    // Финансовая информация
                    float pageWidth = (float)page.Width;
                    float rightEdge = pageWidth - 50;

                    gfx.DrawString("СУММА ЗАКАЗА", regularFont, XBrushes.Black,
                        new XRect(50, yPosition, 200, 20), XStringFormats.TopLeft);
                    gfx.DrawString($"{totalAmount:C2}", regularFont, XBrushes.Black,
                        new XRect(rightEdge - 200, yPosition, 200, 20), XStringFormats.TopRight);
                    yPosition += 20;

                    gfx.DrawString($"Скидка ({discountPercent:F0}%)", regularFont, XBrushes.Black,
                        new XRect(50, yPosition, 200, 20), XStringFormats.TopLeft);
                    gfx.DrawString($"{discountAmount:C2}", regularFont, XBrushes.Black,
                        new XRect(rightEdge - 200, yPosition, 200, 20), XStringFormats.TopRight);
                    yPosition += 20;

                    gfx.DrawString("ИТОГ", boldFont, XBrushes.Black,
                        new XRect(50, yPosition, 200, 20), XStringFormats.TopLeft);
                    gfx.DrawString($"{finalAmount:C2}", boldFont, XBrushes.Black,
                        new XRect(rightEdge - 200, yPosition, 200, 20), XStringFormats.TopRight);
                    yPosition += 20;

                    gfx.DrawString("ПРЕДОПЛАТА", regularFont, XBrushes.Black,
                        new XRect(50, yPosition, 200, 20), XStringFormats.TopLeft);
                    gfx.DrawString($"{prepayment:C2}", regularFont, XBrushes.Black,
                        new XRect(rightEdge - 200, yPosition, 200, 20), XStringFormats.TopRight);
                    yPosition += 20;

                    if (!isPreliminary)
                    {
                        gfx.DrawString("ДОП.РАСХОДЫ", regularFont, XBrushes.Black,
                            new XRect(50, yPosition, 200, 20), XStringFormats.TopLeft);
                        gfx.DrawString($"{_additionalExpenses:C2}", regularFont, XBrushes.Black,
                            new XRect(rightEdge - 200, yPosition, 200, 20), XStringFormats.TopRight);
                        yPosition += 20;
                    }

                    yPosition += 30;

                    // Служебная информация (по центру)
                    string fullname = Properties.Settings.Default.userName;
                    string formattedname = FormatFullName(fullname);

                    // Центрируем текст
                    XSize line1Size = gfx.MeasureString($"Документ сгенерирован: {DateTime.Now:dd.MM.yyyy HH:mm:ss}", infoFont);
                    float line1X = (float)((pageWidth - line1Size.Width) / 2);
                    gfx.DrawString($"Документ сгенерирован: {DateTime.Now:dd.MM.yyyy HH:mm:ss}", infoFont, XBrushes.Gray,
                        new XRect(line1X, yPosition, line1Size.Width, 15), XStringFormats.TopLeft);
                    yPosition += 18;

                    XSize line2Size = gfx.MeasureString($"Сотрудник: {formattedname}", infoFont);
                    float line2X = (float)((pageWidth - line2Size.Width) / 2);
                    gfx.DrawString($"Сотрудник: {formattedname}", infoFont, XBrushes.Gray,
                        new XRect(line2X, yPosition, line2Size.Width, 15), XStringFormats.TopLeft);
                    yPosition += 18;

                    if (!isPreliminary)
                    {
                        string formattedOrderCreator = FormatFullName(_orderData.NameUser ?? "Не указан");
                        XSize line3Size = gfx.MeasureString($"Заказ был оформлен: {formattedOrderCreator}", infoFont);
                        float line3X = (float)((pageWidth - line3Size.Width) / 2);
                        gfx.DrawString($"Заказ был оформлен: {formattedOrderCreator}", infoFont, XBrushes.Gray,
                            new XRect(line3X, yPosition, line3Size.Width, 15), XStringFormats.TopLeft);
                    }
                }

                document.Save(filePath);
            }
        }

        // Рисование таблицы с товарами (возвращает Y-координату низа таблицы)
        private float DrawOrderTable(XGraphics gfx, System.Data.DataTable items, PdfPage page, float startY)
        {
            if (items == null || items.Rows.Count == 0)
                return startY;

            // Настройки таблицы
            float[] columnWidths = { 30, 200, 80, 50, 80 };
            float rowHeight = 22;

            // Вычисляем общую ширину таблицы и стартовую позицию X для центрирования
            float totalTableWidth = columnWidths.Sum();
            float startX = (float)((page.Width - totalTableWidth) / 2);

            XFont headerFont = new XFont("Arial", 9, XFontStyle.Bold);
            XFont cellFont = new XFont("Arial", 8, XFontStyle.Regular);
            XPen pen = new XPen(XColors.Black, 0.5);

            // Заголовок таблицы (по центру)
            XSize headerSize = gfx.MeasureString("СОСТАВ ЗАКАЗА:", headerFont);
            float headerX = (float)((page.Width - headerSize.Width) / 2);
            gfx.DrawString("СОСТАВ ЗАКАЗА:", headerFont, XBrushes.Black,
                new XRect(headerX, startY - 25, headerSize.Width, 20), XStringFormats.TopLeft);

            // Заголовки столбцов
            string[] headers = { "№", "Наименование", "Цена", "Кол-во", "Сумма" };
            float currentX = startX;

            for (int i = 0; i < headers.Length; i++)
            {
                XRect rect = new XRect(currentX, startY, columnWidths[i], rowHeight);
                gfx.DrawRectangle(pen, rect);
                gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(240, 240, 240)), rect);
                gfx.DrawString(headers[i], headerFont, XBrushes.Black, rect, XStringFormats.Center);
                currentX += columnWidths[i];
            }

            float currentY = startY + rowHeight;

            // Заполняем строки таблицы
            for (int i = 0; i < items.Rows.Count; i++)
            {
                DataRow row = items.Rows[i];
                string name = row["Name"].ToString();
                decimal price = Convert.ToDecimal(row["Price"]);

                int quantity;
                if (items.Columns.Contains("Quantity"))
                    quantity = Convert.ToInt32(row["Quantity"]);
                else if (items.Columns.Contains("Count"))
                    quantity = Convert.ToInt32(row["Count"]);
                else
                    quantity = 0;

                decimal total = price * quantity;

                currentX = startX;

                // Номер
                XRect rect1 = new XRect(currentX, currentY, columnWidths[0], rowHeight);
                gfx.DrawRectangle(pen, rect1);
                gfx.DrawString((i + 1).ToString(), cellFont, XBrushes.Black, rect1, XStringFormats.Center);
                currentX += columnWidths[0];

                // Наименование
                XRect rect2 = new XRect(currentX, currentY, columnWidths[1], rowHeight);
                gfx.DrawRectangle(pen, rect2);
                string displayName = name.Length > 30 ? name.Substring(0, 27) + "..." : name;
                gfx.DrawString(displayName, cellFont, XBrushes.Black, rect2, XStringFormats.CenterLeft);
                currentX += columnWidths[1];

                // Цена
                XRect rect3 = new XRect(currentX, currentY, columnWidths[2], rowHeight);
                gfx.DrawRectangle(pen, rect3);
                gfx.DrawString(price.ToString("C2"), cellFont, XBrushes.Black, rect3, XStringFormats.Center);
                currentX += columnWidths[2];

                // Количество
                XRect rect4 = new XRect(currentX, currentY, columnWidths[3], rowHeight);
                gfx.DrawRectangle(pen, rect4);
                gfx.DrawString(quantity.ToString(), cellFont, XBrushes.Black, rect4, XStringFormats.Center);
                currentX += columnWidths[3];

                // Сумма
                XRect rect5 = new XRect(currentX, currentY, columnWidths[4], rowHeight);
                gfx.DrawRectangle(pen, rect5);
                gfx.DrawString(total.ToString("C2"), cellFont, XBrushes.Black, rect5, XStringFormats.Center);

                currentY += rowHeight;
            }

            // Возвращаем Y-координату низа таблицы
            return currentY;
        }

        // ========== МЕТОДЫ ДЛЯ ОТКРЫТИЯ PDF ==========

        // Основной метод открытия PDF с приоритетом Adobe Acrobat
        private void OpenPDFFile(string filePath)
        {
            // Сначала пробуем открыть в Adobe Acrobat
            if (OpenWithAdobeAcrobat(filePath))
                return;

            // Если не получилось, показываем форму выбора браузера
            ShowBrowserChoice(filePath);
        }

        // Открытие в Adobe Acrobat (универсальный поиск)
        private bool OpenWithAdobeAcrobat(string filePath)
        {
            try
            {
                string acrobatPath = FindProgramInSystem("Acrobat.exe");
                if (string.IsNullOrEmpty(acrobatPath))
                    acrobatPath = FindProgramInSystem("AcroRd32.exe");

                if (!string.IsNullOrEmpty(acrobatPath))
                {
                    System.Diagnostics.Process.Start(acrobatPath, $"\"{filePath}\"");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка открытия в Adobe: {ex.Message}");
                return false;
            }
        }

        // Показ выбора браузера
        private void ShowBrowserChoice(string filePath)
        {
            Form choiceForm = new Form();
            choiceForm.Text = "Выбор браузера";
            choiceForm.Size = new Size(400, 350);
            choiceForm.StartPosition = FormStartPosition.CenterParent;
            choiceForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            choiceForm.MaximizeBox = false;
            choiceForm.MinimizeBox = false;
            choiceForm.BackColor = Color.FloralWhite;
            choiceForm.Icon = new Icon("Иконка.ico");

            // Запрещаем закрытие по крестику
            choiceForm.ControlBox = true;
            choiceForm.FormClosing += (s, e) => {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    choiceForm.Hide();
                }
            };

            Label label = new Label();
            label.Text = $"Adobe Acrobat не найден в системе.\n" +
                         $"Выберите браузер для открытия:";
            label.Location = new System.Drawing.Point(20, 20);
            label.Size = new Size(350, 80);
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.Font = new System.Drawing.Font("Comic Sans MS", 12, FontStyle.Regular);
            label.BackColor = Color.FloralWhite;
            choiceForm.Controls.Add(label);

            int yPos = 110;

            // Microsoft Edge
            Button edgeButton = CreateChoiceButton("Microsoft Edge", yPos);
            edgeButton.Click += (s, e) => {
                OpenWithEdge(filePath);
                choiceForm.Close();
            };
            choiceForm.Controls.Add(edgeButton);
            yPos += 50;

            // Google Chrome
            Button chromeButton = CreateChoiceButton("Google Chrome", yPos);
            chromeButton.Click += (s, e) => {
                OpenWithChrome(filePath);
                choiceForm.Close();
            };
            choiceForm.Controls.Add(chromeButton);
            yPos += 50;

            // Mozilla Firefox
            Button firefoxButton = CreateChoiceButton("Mozilla Firefox", yPos);
            firefoxButton.Click += (s, e) => {
                OpenWithFirefox(filePath);
                choiceForm.Close();
            };
            choiceForm.Controls.Add(firefoxButton);
            yPos += 50;

            // Открыть папку с файлом
            Button folderButton = CreateChoiceButton("Открыть папку с файлом", yPos);
            folderButton.Click += (s, e) => {
                string arguments = $"/select, \"{filePath}\"";
                System.Diagnostics.Process.Start("explorer.exe", arguments);
                choiceForm.Close();
            };
            choiceForm.Controls.Add(folderButton);

            choiceForm.ShowDialog();
        }

        private Button CreateChoiceButton(string text, int yPos)
        {
            Button button = new Button();
            button.Text = text;
            button.Size = new Size(250, 40);
            button.Location = new System.Drawing.Point(75, yPos);
            button.BackColor = Color.FromArgb(217, 152, 22);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Color.Black;
            button.Font = new System.Drawing.Font("Comic Sans MS", 12, FontStyle.Regular);
            button.ForeColor = Color.Black;
            button.Cursor = Cursors.Hand;
            return button;
        }

        // Открытие через Microsoft Edge (универсальный поиск)
        private void OpenWithEdge(string filePath)
        {
            try
            {
                string edgePath = FindProgramInSystem("msedge.exe");
                if (!string.IsNullOrEmpty(edgePath))
                {
                    System.Diagnostics.Process.Start(edgePath, $"\"{filePath}\"");
                }
                else
                {
                    MessageBox.Show("Microsoft Edge не найден в системе.", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    OpenWithDefaultProgram(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии в Edge: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                OpenWithDefaultProgram(filePath);
            }
        }

        // Открытие через Google Chrome (универсальный поиск)
        private void OpenWithChrome(string filePath)
        {
            try
            {
                string chromePath = FindProgramInSystem("chrome.exe");
                if (!string.IsNullOrEmpty(chromePath))
                {
                    System.Diagnostics.Process.Start(chromePath, $"\"{filePath}\"");
                }
                else
                {
                    MessageBox.Show("Google Chrome не найден в системе.", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    OpenWithDefaultProgram(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии в Chrome: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                OpenWithDefaultProgram(filePath);
            }
        }

        // Открытие через Mozilla Firefox (универсальный поиск)
        private void OpenWithFirefox(string filePath)
        {
            try
            {
                string firefoxPath = FindProgramInSystem("firefox.exe");
                if (!string.IsNullOrEmpty(firefoxPath))
                {
                    System.Diagnostics.Process.Start(firefoxPath, $"\"{filePath}\"");
                }
                else
                {
                    MessageBox.Show("Mozilla Firefox не найден в системе.", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    OpenWithDefaultProgram(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии в Firefox: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                OpenWithDefaultProgram(filePath);
            }
        }

        // Открытие через программу по умолчанию
        private void OpenWithDefaultProgram(string filePath)
        {
            try
            {
                System.Diagnostics.Process.Start(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии: {ex.Message}\n\nФайл сохранен:\n{filePath}",
                               "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                string arguments = $"/select, \"{filePath}\"";
                System.Diagnostics.Process.Start("explorer.exe", arguments);
            }
        }

        // ========== УНИВЕРСАЛЬНЫЙ ПОИСК ПРОГРАММ В СИСТЕМЕ ==========

        // Универсальный метод для поиска программы в системе
        private string FindProgramInSystem(string programName)
        {
            string registryPath = FindProgramInRegistry(programName);
            if (!string.IsNullOrEmpty(registryPath))
                return registryPath;

            DriveInfo[] drives = DriveInfo.GetDrives();
            List<string> searchPaths = new List<string>();

            searchPaths.Add(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
            searchPaths.Add(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
            searchPaths.Add(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            searchPaths.Add(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles));
            searchPaths.Add(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86));

            foreach (DriveInfo drive in drives)
            {
                if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                {
                    searchPaths.Add(drive.RootDirectory.FullName + "Program Files");
                    searchPaths.Add(drive.RootDirectory.FullName + "Program Files (x86)");
                }
            }

            foreach (string path in searchPaths)
            {
                try
                {
                    if (Directory.Exists(path))
                    {
                        string foundPath = SearchFileRecursive(path, programName, 3);
                        if (!string.IsNullOrEmpty(foundPath))
                            return foundPath;
                    }
                }
                catch { }
            }

            return null;
        }

        // Поиск программы в реестре Windows
        private string FindProgramInRegistry(string programExeName)
        {
            try
            {
                string[] registryPaths = {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths",
                    @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths"
                };

                foreach (string registryPath in registryPaths)
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryPath))
                    {
                        if (key != null)
                        {
                            foreach (string subKeyName in key.GetSubKeyNames())
                            {
                                if (subKeyName.ToLower().Contains(programExeName.ToLower().Replace(".exe", "")))
                                {
                                    using (RegistryKey appKey = key.OpenSubKey(subKeyName))
                                    {
                                        string path = appKey?.GetValue("")?.ToString();
                                        if (!string.IsNullOrEmpty(path) && File.Exists(path))
                                            return path;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        // Рекурсивный поиск файла с ограничением глубины
        private string SearchFileRecursive(string directory, string fileName, int maxDepth = 3, int currentDepth = 0)
        {
            if (currentDepth > maxDepth)
                return null;

            try
            {
                string directPath = Path.Combine(directory, fileName);
                if (File.Exists(directPath))
                    return directPath;

                foreach (string subDir in Directory.GetDirectories(directory))
                {
                    try
                    {
                        string subPath = Path.Combine(subDir, fileName);
                        if (File.Exists(subPath))
                            return subPath;

                        string found = SearchFileRecursive(subDir, fileName, maxDepth, currentDepth + 1);
                        if (!string.IsNullOrEmpty(found))
                            return found;
                    }
                    catch { }
                }
            }
            catch { }

            return null;
        }

        // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========

        // Генерация предварительного документа Word
        private void GeneratePreliminaryWordTicket()
        {
            Microsoft.Office.Interop.Word.Application wordApp = null;
            Microsoft.Office.Interop.Word.Document doc = null;

            try
            {
                if (_orderData == null || _cartItems == null)
                {
                    MessageBox.Show("Данные заказа не загружены", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                wordApp = new Microsoft.Office.Interop.Word.Application();
                wordApp.Visible = true;

                string templatePath = GetTemplatePath("firstblank.docx");
                doc = wordApp.Documents.Open(templatePath, ReadOnly: false);
                doc.Activate();

                decimal totalAmount = CalculateTotalAmount(_cartItems);
                (decimal discountAmount, decimal discountPercent, decimal prepayment) = CalculateDiscountValues(totalAmount);
                decimal finalAmount = totalAmount - discountAmount;

                FillBookmark(doc, "NumberOrder", _orderData.NumberOrder);
                FillBookmark(doc, "DateOrder", _orderData.DateOrder);
                FillBookmark(doc, "NameClient", _orderData.NameClient);
                FillBookmark(doc, "NumberPhone", _orderData.NumberPhone);
                FillBookmark(doc, "Event", _orderData.Event);
                FillBookmark(doc, "DateCreate", _orderData.Date);
                FillBookmark(doc, "Time", _orderData.Time);
                FillBookmark(doc, "CountOrder", totalAmount.ToString("C"));
                FillBookmark(doc, "DiscountAmoust", discountAmount.ToString("C"));
                FillBookmark(doc, "CountOrderAmoust", finalAmount.ToString("C"));
                FillBookmark(doc, "Prepaymant", prepayment.ToString("C"));
                FillBookmark(doc, "Discount", discountPercent.ToString());

                ReplaceExampleTableWithActualData(doc, wordApp, _cartItems);
                AddServiceInfoToPreliminaryWord(doc);

                doc.Save();

                MessageBox.Show("Предварительный документ заказа создан.", "Успех",
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
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(doc);
                    if (wordApp != null)
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(wordApp);
                }
                catch { }
            }
        }

        // Генерация окончательного документа Word
        private void GenerateFinalWordTicket()
        {
            Microsoft.Office.Interop.Word.Application wordApp = null;
            Microsoft.Office.Interop.Word.Document doc = null;

            try
            {
                if (_orderData == null || _cartItems == null)
                {
                    MessageBox.Show("Данные заказа не загружены", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                wordApp = new Microsoft.Office.Interop.Word.Application();
                wordApp.Visible = true;

                string templatePath = GetTemplatePath("secondblank.docx");
                doc = wordApp.Documents.Open(templatePath, ReadOnly: false);
                doc.Activate();

                decimal totalAmount = _orderData.TotalAmount + _additionalExpenses;
                decimal discountAmount = _orderData.DiscountAmount;
                decimal finalAmount = (_orderData.FinalAmount > 0 ? _orderData.FinalAmount : _orderData.TotalAmount - discountAmount) + _additionalExpenses;
                decimal prepayment = _orderData.Prepayment;
                decimal discountPercent = totalAmount > 0 ? (discountAmount / totalAmount) * 100 : 0;

                FillBookmark(doc, "NumberOrder", _orderData.NumberOrder);
                FillBookmark(doc, "DateOrder", _orderData.DateOrder);
                FillBookmark(doc, "NameClient", _orderData.NameClient);
                FillBookmark(doc, "NumberPhone", _orderData.NumberPhone);
                FillBookmark(doc, "Event", _orderData.Event);
                FillBookmark(doc, "DateCreate", _orderData.Date);
                FillBookmark(doc, "Time", _orderData.Time);
                FillBookmark(doc, "CountOrder", totalAmount.ToString("C"));
                FillBookmark(doc, "DiscountAmoust", discountAmount.ToString("C"));
                FillBookmark(doc, "CountOrderAmoust", finalAmount.ToString("C"));
                FillBookmark(doc, "Prepaymant", prepayment.ToString("C"));
                FillBookmark(doc, "Discount", Math.Round(discountPercent).ToString());
                FillBookmark(doc, "AddExpenses", _additionalExpenses.ToString("C"));

                ReplaceExampleTableWithActualData(doc, wordApp, _cartItems);
                AddServiceInfoToFinalWord(doc, _orderData.NameUser ?? "Не указан");

                doc.Save();

                MessageBox.Show("Окончательный документ заказа создан.", "Успех",
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
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(doc);
                    if (wordApp != null)
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(wordApp);
                }
                catch { }
            }
        }

        private string GetTemplatePath(string templateName)
        {
            string[] possiblePaths = {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", templateName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, templateName),
                Path.Combine(Directory.GetCurrentDirectory(), "Resources", templateName),
                $@"Resources\{templateName}",
                $@"..\Resources\{templateName}"
            };

            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return Path.GetFullPath(path);
                }
            }

            throw new FileNotFoundException($"Шаблон {templateName} не найден. Проверьте наличие файла в папке Resources");
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при заполнении закладки '{bookmarkName}': {ex.Message}");
            }
        }

        private void ReplaceExampleTableWithActualData(Microsoft.Office.Interop.Word.Document doc, Microsoft.Office.Interop.Word.Application wordApp, System.Data.DataTable items)
        {
            try
            {
                if (doc.Tables.Count > 0)
                {
                    Microsoft.Office.Interop.Word.Table exampleTable = doc.Tables[1];
                    Microsoft.Office.Interop.Word.Range tableRange = exampleTable.Range;
                    exampleTable.Delete();
                    InsertActualOrderTable(doc, wordApp, tableRange, items);
                }
                else
                {
                    InsertActualOrderTable(doc, wordApp, null, items);
                }
            }
            catch (Exception ex)
            {
                InsertActualOrderTable(doc, wordApp, null, items);
            }
        }

        private void InsertActualOrderTable(Microsoft.Office.Interop.Word.Document doc, Microsoft.Office.Interop.Word.Application wordApp, Microsoft.Office.Interop.Word.Range targetRange, System.Data.DataTable items)
        {
            if (items.Rows.Count == 0)
            {
                Microsoft.Office.Interop.Word.Paragraph paragraph;
                if (targetRange != null)
                    paragraph = doc.Paragraphs.Add(targetRange);
                else
                    paragraph = doc.Paragraphs.Add();
                paragraph.Range.Text = "Заказ не содержит товаров";
                paragraph.Range.Font.Size = 12;
                paragraph.Range.InsertParagraphAfter();
                return;
            }

            Microsoft.Office.Interop.Word.Table table;

            if (targetRange != null)
                table = doc.Tables.Add(targetRange, items.Rows.Count + 1, 5);
            else
                table = doc.Tables.Add(doc.Range(doc.Content.End - 1), items.Rows.Count + 1, 5);

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

            for (int i = 0; i < items.Rows.Count; i++)
            {
                DataRow row = items.Rows[i];
                decimal price = Convert.ToDecimal(row["Price"]);

                int quantity;
                if (items.Columns.Contains("Quantity"))
                {
                    quantity = Convert.ToInt32(row["Quantity"]);
                }
                else if (items.Columns.Contains("Count"))
                {
                    quantity = Convert.ToInt32(row["Count"]);
                }
                else
                {
                    throw new Exception("Не найдена колонка с количеством товара (ни 'Quantity', ни 'Count')");
                }

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
                cell.Range.ParagraphFormat.Alignment = Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphCenter;
            foreach (Microsoft.Office.Interop.Word.Cell cell in table.Columns[4].Cells)
                cell.Range.ParagraphFormat.Alignment = Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphCenter;
            foreach (Microsoft.Office.Interop.Word.Cell cell in table.Columns[5].Cells)
                cell.Range.ParagraphFormat.Alignment = Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphCenter;
        }

        private void AddServiceInfoToPreliminaryWord(Microsoft.Office.Interop.Word.Document doc)
        {
            Microsoft.Office.Interop.Word.Range range = doc.Range(doc.Content.End - 1, doc.Content.End - 1);
            range.InsertParagraphAfter();
            range.InsertParagraphAfter();

            string fullname = Properties.Settings.Default.userName;
            string formattedname = FormatFullName(fullname);

            range.Text = $"Документ сгенерирован: {DateTime.Now:dd.MM.yyyy HH:mm:ss}\rСотрудник: {formattedname}";
            range.Font.Size = 10;
            range.Font.Italic = 1;
        }

        private void AddServiceInfoToFinalWord(Microsoft.Office.Interop.Word.Document doc, string orderCreatorName)
        {
            Microsoft.Office.Interop.Word.Range range = doc.Range(doc.Content.End - 1, doc.Content.End - 1);
            range.InsertParagraphAfter();
            range.InsertParagraphAfter();

            string fullname = Properties.Settings.Default.userName;
            string formattedname = FormatFullName(fullname);
            string formattedOrderCreator = FormatFullName(orderCreatorName);

            range.Text = $"Документ сгенерирован: {DateTime.Now:dd.MM.yyyy HH:mm:ss}\rСотрудник: {formattedname}\rЗаказ был оформлен: {formattedOrderCreator}";
            range.Font.Size = 10;
            range.Font.Italic = 1;
        }

        private string FormatFullName(string fullname)
        {
            if (string.IsNullOrEmpty(fullname)) return fullname;

            string[] parts = fullname.Split(' ');
            if (parts.Length == 3)
            {
                string lastname = parts[0];
                string firstname = parts[1].Substring(0, 1);
                string middle = parts[2].Substring(0, 1);
                return $"{lastname} {firstname}.{middle}.";
            }
            return fullname;
        }

        private (decimal discountAmount, decimal discountPercent, decimal prepayment) CalculateDiscountValues(decimal totalAmount)
        {
            decimal discountAmount = 0;
            decimal discountPercent = 0;

            if (totalAmount >= 40000)
                discountPercent = 15;
            else if (totalAmount >= 30000)
                discountPercent = 10;

            discountAmount = totalAmount * (discountPercent / 100m);
            decimal amountAfterDiscount = totalAmount - discountAmount;
            decimal prepayment = amountAfterDiscount * 0.1m;

            return (discountAmount, discountPercent, prepayment);
        }

        private decimal CalculateTotalAmount(System.Data.DataTable items)
        {
            if (items == null || items.Rows.Count == 0)
                return 0;

            decimal total = 0;
            foreach (DataRow row in items.Rows)
            {
                decimal price = Convert.ToDecimal(row["Price"]);

                int quantity;
                if (items.Columns.Contains("Quantity"))
                {
                    quantity = Convert.ToInt32(row["Quantity"]);
                }
                else if (items.Columns.Contains("Count"))
                {
                    quantity = Convert.ToInt32(row["Count"]);
                }
                else
                {
                    quantity = 0;
                }

                total += price * quantity;
            }
            return total;
        }
    }
}