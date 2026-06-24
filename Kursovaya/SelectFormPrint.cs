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
    //Перечисление типов документов
    public enum DocumentType
    {
        Preliminary,
        Final
    }

    public partial class SelectFormPrint : Form
    {
        private System.Data.DataTable _cartItems;
        private bool _shouldOpenMakingAnOrder;
        private OrderData _orderData;
        private decimal _discountAmountValue;
        private DocumentType _documentType;
        private ViewingAnOrder _viewingAnOrderForm;
        private decimal _additionalExpenses;
        private Form _previousForm;

        private bool _isClosingProgrammatically = false;

        //Конструктор для предварительного документа
        public SelectFormPrint(System.Data.DataTable cartItems, OrderData orderData, decimal discountAmountValue, DocumentType type, Form previousForm = null)
        {
            InitializeComponent();
            this._cartItems = cartItems;
            this._orderData = orderData;
            this._discountAmountValue = discountAmountValue;
            this._documentType = type;
            this._viewingAnOrderForm = null;
            this._previousForm = previousForm;
            this._shouldOpenMakingAnOrder = true;
            button1.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button2.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button3.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
        }

        //Конструктор для окончательного документа
        public SelectFormPrint(OrderData orderData, System.Data.DataTable orderItems, DocumentType type, ViewingAnOrder parentForm, decimal additionalExpenses, Form previousForm = null)
        {
            InitializeComponent();
            this._orderData = orderData;
            this._cartItems = orderItems;
            this._documentType = type;
            this._viewingAnOrderForm = parentForm;
            this._additionalExpenses = additionalExpenses;
            this._previousForm = previousForm;
            this._shouldOpenMakingAnOrder = false;
            button1.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button2.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button3.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
        }

        //Обработчик кнопки возврата
        private void button3_Click(object sender, EventArgs e)
        {
            _isClosingProgrammatically = true;

            if (_previousForm != null && !_previousForm.IsDisposed)
            {
                _previousForm.Show();
            }

            this.Close();
        }

        //Обработчик закрытия формы
        private void SelectFormPrint_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isClosingProgrammatically)
                return;

            e.Cancel = true;
        }

        //Обработчик кнопки создания Word документа
        private void button2_Click(object sender, EventArgs e)
        {
            _isClosingProgrammatically = true;

            if (_documentType == DocumentType.Preliminary)
            {
                GeneratePreliminaryWordTicket();
                this.Visible = false;
                MakingAnOrder makingAnOrder = new MakingAnOrder();
                makingAnOrder.ShowDialog();
                this.Close();
            }
            else
            {
                GenerateFinalWordTicket();
                this.Visible = false;
                _isClosingProgrammatically = true;

                if (_previousForm != null && !_previousForm.IsDisposed)
                {
                    _previousForm.Show();
                }

                this.Close();
            }
        }

        //Обработчик кнопки создания PDF документа
        private void button1_Click(object sender, EventArgs e)
        {
            _isClosingProgrammatically = true;

            if (_documentType == DocumentType.Preliminary)
            {
                GeneratePreliminaryPDFTicket();
                this.Visible = false;
                MakingAnOrder makingAnOrder = new MakingAnOrder();
                makingAnOrder.ShowDialog();
                this.Close();
            }
            else
            {
                GenerateFinalPDFTicket();
                this.Visible = false;
                _isClosingProgrammatically = true;

                if (_previousForm != null && !_previousForm.IsDisposed)
                {
                    _previousForm.Show();
                }

                this.Close();
            }
        }

        //Получение пути к папке Documents
        private static string GetDocumentsPath()
        {
            // Используем тот же принцип, что и в BackupManager
            string exeDirectory = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
            string docsFolder = Path.Combine(exeDirectory, "CafeManagement", "Documents");

            if (!Directory.Exists(docsFolder))
            {
                Directory.CreateDirectory(docsFolder);
            }

            return docsFolder;
        }

        //Генерация PDF предварительного документа
        private void GeneratePreliminaryPDFTicket()
        {
            try
            {
                string docsFolder = GetDocumentsPath();

                string fileName = $"Предварительный_документ_заказ_{_orderData.NumberOrder}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                string filePath = Path.Combine(docsFolder, fileName);

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

        //Генерация PDF итогового документа
        private void GenerateFinalPDFTicket()
        {
            try
            {
                string docsFolder = GetDocumentsPath();

                string fileName = $"Итоговый_документ_заказ_{_orderData.NumberOrder}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                string filePath = Path.Combine(docsFolder, fileName);

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

        //Создание PDF документа
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
                    XFont cancelledFont = new XFont("Arial", 16, XFontStyle.Bold);
                    XFont subtitleFont = new XFont("Arial", 14, XFontStyle.Bold);
                    XFont regularFont = new XFont("Arial", 10, XFontStyle.Regular);
                    XFont boldFont = new XFont("Arial", 10, XFontStyle.Bold);
                    XFont infoFont = new XFont("Arial", 8, XFontStyle.Italic);
                    XFont labelFont = new XFont("Arial", 10, XFontStyle.Bold);

                    float yPosition = 50;
                    float pageWidth = (float)page.Width;
                    float leftColumnX = 50;
                    float rightColumnX = pageWidth - 200;
                    float rowHeight = 22;

                    if (!string.IsNullOrEmpty(_orderData.Status) && _orderData.Status == "Отменен")
                    {
                        string cancelledText = "ЗАКАЗ ОТМЕНЕН";
                        XSize cancelledSize = gfx.MeasureString(cancelledText, cancelledFont);
                        float cancelledX = (float)((pageWidth - cancelledSize.Width) / 2);

                        gfx.DrawString(cancelledText, cancelledFont, XBrushes.Red,
                            new XRect(cancelledX, yPosition, cancelledSize.Width, 30), XStringFormats.TopLeft);
                        yPosition += 35;
                    }

                    string titleText = "БЛАНК ЗАКАЗА";
                    XSize titleSize = gfx.MeasureString(titleText, titleFont);
                    float titleX = (float)((pageWidth - titleSize.Width) / 2);
                    gfx.DrawString(titleText, titleFont, XBrushes.Black,
                        new XRect(titleX, yPosition, titleSize.Width, 30), XStringFormats.TopLeft);
                    yPosition += 40;

                    gfx.DrawString("Номер заказа:", labelFont, XBrushes.Black,
                        new XRect(leftColumnX, yPosition, 100, rowHeight), XStringFormats.TopLeft);
                    gfx.DrawString(_orderData.NumberOrder.ToString(), regularFont, XBrushes.Black,
                        new XRect(leftColumnX + 100, yPosition, 80, rowHeight), XStringFormats.TopLeft);

                    gfx.DrawString("Клиент:", labelFont, XBrushes.Black,
                        new XRect(rightColumnX, yPosition, 55, rowHeight), XStringFormats.TopLeft);
                    gfx.DrawString(_orderData.NameClient, regularFont, XBrushes.Black,
                        new XRect(rightColumnX + 55, yPosition, 140, rowHeight), XStringFormats.TopLeft);
                    yPosition += rowHeight;

                    gfx.DrawString("Дата создания:", labelFont, XBrushes.Black,
                        new XRect(leftColumnX, yPosition, 100, rowHeight), XStringFormats.TopLeft);
                    gfx.DrawString(_orderData.DateOrder, regularFont, XBrushes.Black,
                        new XRect(leftColumnX + 100, yPosition, 80, rowHeight), XStringFormats.TopLeft);

                    gfx.DrawString("Телефон:", labelFont, XBrushes.Black,
                        new XRect(rightColumnX, yPosition, 60, rowHeight), XStringFormats.TopLeft);
                    gfx.DrawString(_orderData.NumberPhone, regularFont, XBrushes.Black,
                        new XRect(rightColumnX + 60, yPosition, 140, rowHeight), XStringFormats.TopLeft);
                    yPosition += rowHeight;

                    gfx.DrawString("Мероприятие:", labelFont, XBrushes.Black,
                        new XRect(leftColumnX, yPosition, 100, rowHeight), XStringFormats.TopLeft);

                    string eventName = _orderData.Event;
                    float eventMaxWidth = 180;
                    if (gfx.MeasureString(eventName, regularFont).Width > eventMaxWidth)
                    {
                        string[] words = eventName.Split(' ');
                        string line1 = "", line2 = "";
                        foreach (string word in words)
                        {
                            if (gfx.MeasureString((line1 == "" ? "" : line1 + " ") + word, regularFont).Width <= eventMaxWidth)
                                line1 += (line1 == "" ? "" : " ") + word;
                            else
                                line2 += (line2 == "" ? "" : " ") + word;
                        }
                        gfx.DrawString(line1, regularFont, XBrushes.Black,
                            new XRect(leftColumnX + 100, yPosition, eventMaxWidth, rowHeight), XStringFormats.TopLeft);
                        if (!string.IsNullOrEmpty(line2))
                        {
                            gfx.DrawString(line2, regularFont, XBrushes.Black,
                                new XRect(leftColumnX + 100, yPosition + 15, eventMaxWidth, rowHeight), XStringFormats.TopLeft);
                            yPosition += 15;
                        }
                    }
                    else
                    {
                        gfx.DrawString(eventName, regularFont, XBrushes.Black,
                            new XRect(leftColumnX + 100, yPosition, eventMaxWidth, rowHeight), XStringFormats.TopLeft);
                    }

                    gfx.DrawString("Дата проведения:", labelFont, XBrushes.Black,
                        new XRect(rightColumnX, yPosition, 115, rowHeight), XStringFormats.TopLeft);
                    gfx.DrawString(_orderData.Date, regularFont, XBrushes.Black,
                        new XRect(rightColumnX + 115, yPosition, 85, rowHeight), XStringFormats.TopLeft);
                    yPosition += rowHeight;

                    gfx.DrawString("Время:", labelFont, XBrushes.Black,
                        new XRect(rightColumnX, yPosition, 55, rowHeight), XStringFormats.TopLeft);
                    gfx.DrawString(_orderData.Time, regularFont, XBrushes.Black,
                        new XRect(rightColumnX + 55, yPosition, 140, rowHeight), XStringFormats.TopLeft);
                    yPosition += rowHeight + 20;

                    XSize headerSize = gfx.MeasureString("СОСТАВ ЗАКАЗА", subtitleFont);
                    float headerX = (float)((pageWidth - headerSize.Width) / 2);
                    gfx.DrawString("СОСТАВ ЗАКАЗА", subtitleFont, XBrushes.Black,
                        new XRect(headerX, yPosition, headerSize.Width, 25), XStringFormats.TopLeft);

                    yPosition += 35;

                    float tableBottomY = DrawOrderTable(gfx, _cartItems, page, yPosition);
                    yPosition = tableBottomY + 20;

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

                    yPosition += 20;

                    XPen linePen = new XPen(XColors.LightGray, 0.5);
                    gfx.DrawLine(linePen, 50, yPosition, pageWidth - 50, yPosition);
                    yPosition += 15;

                    if (isPreliminary)
                    {
                        XFont warningFont = new XFont("Arial", 10, XFontStyle.Bold | XFontStyle.Italic);
                        string warningText = "ВНИМАНИЕ: Предоплата в случае отмены заказа НЕ ВОЗВРАЩАЕТСЯ!";
                        XSize warningSize = gfx.MeasureString(warningText, warningFont);
                        float warningX = (float)((pageWidth - warningSize.Width) / 2);

                        gfx.DrawString(warningText, warningFont, XBrushes.DarkGray,
                            new XRect(warningX, yPosition, warningSize.Width, 15), XStringFormats.TopLeft);
                        yPosition += 20;
                    }

                    string fullname = Properties.Settings.Default.userName;
                    string formattedname = FormatFullName(fullname);

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

        //Рисование таблицы с товарами
        private float DrawOrderTable(XGraphics gfx, System.Data.DataTable items, PdfPage page, float startY)
        {
            if (items == null || items.Rows.Count == 0)
                return startY;

            float[] columnWidths = { 30, 200, 80, 50, 80 };
            float rowHeight = 22;

            float totalTableWidth = columnWidths.Sum();
            float startX = (float)((page.Width - totalTableWidth) / 2);

            XFont headerFont = new XFont("Arial", 9, XFontStyle.Bold);
            XFont cellFont = new XFont("Arial", 8, XFontStyle.Regular);
            XPen pen = new XPen(XColors.Black, 0.5);

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

                XRect rect1 = new XRect(currentX, currentY, columnWidths[0], rowHeight);
                gfx.DrawRectangle(pen, rect1);
                gfx.DrawString((i + 1).ToString(), cellFont, XBrushes.Black, rect1, XStringFormats.Center);
                currentX += columnWidths[0];

                XRect rect2 = new XRect(currentX, currentY, columnWidths[1], rowHeight);
                gfx.DrawRectangle(pen, rect2);
                string displayName = name.Length > 30 ? name.Substring(0, 27) + "..." : name;
                gfx.DrawString(displayName, cellFont, XBrushes.Black, rect2, XStringFormats.CenterLeft);
                currentX += columnWidths[1];

                XRect rect3 = new XRect(currentX, currentY, columnWidths[2], rowHeight);
                gfx.DrawRectangle(pen, rect3);
                gfx.DrawString(price.ToString("C2"), cellFont, XBrushes.Black, rect3, XStringFormats.Center);
                currentX += columnWidths[2];

                XRect rect4 = new XRect(currentX, currentY, columnWidths[3], rowHeight);
                gfx.DrawRectangle(pen, rect4);
                gfx.DrawString(quantity.ToString(), cellFont, XBrushes.Black, rect4, XStringFormats.Center);
                currentX += columnWidths[3];

                XRect rect5 = new XRect(currentX, currentY, columnWidths[4], rowHeight);
                gfx.DrawRectangle(pen, rect5);
                gfx.DrawString(total.ToString("C2"), cellFont, XBrushes.Black, rect5, XStringFormats.Center);

                currentY += rowHeight;
            }

            return currentY;
        }

        //Открытие PDF файла
        private void OpenPDFFile(string filePath)
        {
            if (OpenWithAdobeAcrobat(filePath))
                return;

            ShowBrowserChoice(filePath);
        }

        //Открытие в Adobe Acrobat
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

        //Показ выбора браузера
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

            Button edgeButton = CreateChoiceButton("Microsoft Edge", yPos);
            edgeButton.Click += (s, e) => {
                OpenWithEdge(filePath);
                choiceForm.Close();
            };
            choiceForm.Controls.Add(edgeButton);
            yPos += 50;

            Button chromeButton = CreateChoiceButton("Google Chrome", yPos);
            chromeButton.Click += (s, e) => {
                OpenWithChrome(filePath);
                choiceForm.Close();
            };
            choiceForm.Controls.Add(chromeButton);
            yPos += 50;

            Button firefoxButton = CreateChoiceButton("Mozilla Firefox", yPos);
            firefoxButton.Click += (s, e) => {
                OpenWithFirefox(filePath);
                choiceForm.Close();
            };
            choiceForm.Controls.Add(firefoxButton);
            yPos += 50;

            Button folderButton = CreateChoiceButton("Открыть папку с файлом", yPos);
            folderButton.Click += (s, e) => {
                string arguments = $"/select, \"{filePath}\"";
                System.Diagnostics.Process.Start("explorer.exe", arguments);
                choiceForm.Close();
            };
            choiceForm.Controls.Add(folderButton);

            choiceForm.ShowDialog();
        }

        //Создание кнопки выбора
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

        //Открытие через Microsoft Edge
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

        //Открытие через Google Chrome
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

        //Открытие через Mozilla Firefox
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

        //Открытие программой по умолчанию
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

        //Поиск программы в системе
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

        //Поиск программы в реестре
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

        //Рекурсивный поиск файла
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

        //Замена текста в документе Word
        private void ReplaceTextInDocument(Microsoft.Office.Interop.Word.Document doc, string placeholder, string value)
        {
            try
            {
                Microsoft.Office.Interop.Word.Range range = doc.Content;
                range.Find.ClearFormatting();
                range.Find.Replacement.ClearFormatting();
                range.Find.Text = placeholder;
                range.Find.Replacement.Text = value;
                range.Find.Execute(Replace: Microsoft.Office.Interop.Word.WdReplace.wdReplaceAll);
                System.Diagnostics.Debug.WriteLine($"✓ Заменен плейсхолдер '{placeholder}' на '{value}'");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка замены '{placeholder}': {ex.Message}");
            }
        }

        //Генерация предварительного Word документа
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

                ReplaceTextInDocument(doc, "{NumberOrder}", _orderData.NumberOrder.ToString());
                ReplaceTextInDocument(doc, "{DateOrder}", _orderData.DateOrder);
                ReplaceTextInDocument(doc, "{NameClient}", _orderData.NameClient);
                ReplaceTextInDocument(doc, "{NumberPhone}", _orderData.NumberPhone);
                ReplaceTextInDocument(doc, "{Event}", _orderData.Event);
                ReplaceTextInDocument(doc, "{DateCreate}", _orderData.Date);
                ReplaceTextInDocument(doc, "{Time}", _orderData.Time);
                ReplaceTextInDocument(doc, "{CountOrder}", totalAmount.ToString("C2"));
                ReplaceTextInDocument(doc, "{DiscountAmount}", discountAmount.ToString("C2"));
                ReplaceTextInDocument(doc, "{CountOrderAmount}", finalAmount.ToString("C2"));
                ReplaceTextInDocument(doc, "{Prepayment}", prepayment.ToString("C2"));
                ReplaceTextInDocument(doc, "{Discount}", discountPercent.ToString("F0"));

                ReplaceTable2WithOrderItems(doc, wordApp, _cartItems);

                Microsoft.Office.Interop.Word.Table totalsTable = FindTableByText(doc, "ИТОГ");
                if (totalsTable != null)
                {
                    Microsoft.Office.Interop.Word.Range tableRange = totalsTable.Range;
                    ReplaceTextInRange(tableRange, "{CountOrder}", totalAmount.ToString("C2"));
                    ReplaceTextInRange(tableRange, "{DiscountAmoust}", discountAmount.ToString("C2"));
                    ReplaceTextInRange(tableRange, "{CountOrderAmoust}", finalAmount.ToString("C2"));
                    ReplaceTextInRange(tableRange, "{Prepayment}", prepayment.ToString("C2"));
                    ReplaceTextInRange(tableRange, "{Discount}", discountPercent.ToString("F0"));
                    MakeBoldInTable(totalsTable, finalAmount.ToString("C2"));
                }

                AddServiceInfoToPreliminaryWord(doc);

                MessageBox.Show("Предварительный документ заказа создан.\n\n" +
                               "После просмотра документа вы можете сохранить его в нужное место.\n" +
                               "Закройте документ Word для продолжения работы.",
                               "Успех",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Information);
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
                    if (doc != null) System.Runtime.InteropServices.Marshal.ReleaseComObject(doc);
                    if (wordApp != null) System.Runtime.InteropServices.Marshal.ReleaseComObject(wordApp);
                }
                catch { }
            }
        }

        //Генерация итогового Word документа
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

                if (_orderData.Status == "Отменен")
                {
                    Microsoft.Office.Interop.Word.Range startRange = doc.Range(0, 0);
                    startRange.Text = "ЗАКАЗ ОТМЕНЕН\n";
                    startRange.Font.Bold = 1;
                    startRange.Font.Size = 22;
                    startRange.Font.Color = Microsoft.Office.Interop.Word.WdColor.wdColorRed;
                    startRange.ParagraphFormat.Alignment = Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphCenter;
                }

                decimal totalAmount = _orderData.TotalAmount + _additionalExpenses;
                decimal discountAmount = _orderData.DiscountAmount;
                decimal finalAmount = (_orderData.FinalAmount > 0 ? _orderData.FinalAmount : _orderData.TotalAmount - discountAmount) + _additionalExpenses;
                decimal prepayment = _orderData.Prepayment;
                decimal discountPercent = totalAmount > 0 ? (discountAmount / totalAmount) * 100 : 0;

                ReplaceTextInDocument(doc, "{NumberOrder}", _orderData.NumberOrder.ToString());
                ReplaceTextInDocument(doc, "{DateOrder}", _orderData.DateOrder);
                ReplaceTextInDocument(doc, "{NameClient}", _orderData.NameClient);
                ReplaceTextInDocument(doc, "{NumberPhone}", _orderData.NumberPhone);
                ReplaceTextInDocument(doc, "{Event}", _orderData.Event);
                ReplaceTextInDocument(doc, "{DateCreate}", _orderData.Date);
                ReplaceTextInDocument(doc, "{Time}", _orderData.Time);
                ReplaceTextInDocument(doc, "{CountOrder}", totalAmount.ToString("C2"));
                ReplaceTextInDocument(doc, "{DiscountAmount}", discountAmount.ToString("C2"));
                ReplaceTextInDocument(doc, "{CountOrderAmount}", finalAmount.ToString("C2"));
                ReplaceTextInDocument(doc, "{Prepayment}", prepayment.ToString("C2"));
                ReplaceTextInDocument(doc, "{AddExpenses}", _additionalExpenses.ToString("C2"));
                ReplaceTextInDocument(doc, "{Discount}", discountPercent.ToString("F0"));

                ReplaceTable2WithOrderItems(doc, wordApp, _cartItems);

                Microsoft.Office.Interop.Word.Table totalsTable = FindTableByText(doc, "ИТОГ");
                if (totalsTable != null)
                {
                    Microsoft.Office.Interop.Word.Range tableRange = totalsTable.Range;
                    ReplaceTextInRange(tableRange, "{CountOrder}", totalAmount.ToString("C2"));
                    ReplaceTextInRange(tableRange, "{DiscountAmoust}", discountAmount.ToString("C2"));
                    ReplaceTextInRange(tableRange, "{CountOrderAmoust}", finalAmount.ToString("C2"));
                    ReplaceTextInRange(tableRange, "{Prepayment}", prepayment.ToString("C2"));
                    ReplaceTextInRange(tableRange, "{AddExpenses}", _additionalExpenses.ToString("C2"));
                    ReplaceTextInRange(tableRange, "{Discount}", discountPercent.ToString("F0"));

                    MakeBoldInTable(totalsTable, finalAmount.ToString("C2"));
                }

                AddServiceInfoToFinalWord(doc, _orderData.NameUser ?? "Не указан");

                MessageBox.Show("Окончательный документ заказа создан.\n\n" +
                               "После просмотра документа вы можете сохранить его в нужное место.\n" +
                               "Закройте документ Word для продолжения работы.",
                               "Успех",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Information);
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
                    if (doc != null) System.Runtime.InteropServices.Marshal.ReleaseComObject(doc);
                    if (wordApp != null) System.Runtime.InteropServices.Marshal.ReleaseComObject(wordApp);
                }
                catch { }
            }
        }

        //Замена текста в диапазоне Word
        private void ReplaceTextInRange(Microsoft.Office.Interop.Word.Range range, string placeholder, string value)
        {
            try
            {
                range.Find.ClearFormatting();
                range.Find.Replacement.ClearFormatting();
                range.Find.Text = placeholder;
                range.Find.Replacement.Text = value;
                range.Find.Execute(Replace: Microsoft.Office.Interop.Word.WdReplace.wdReplaceAll);
                System.Diagnostics.Debug.WriteLine($"✓ Заменен '{placeholder}' на '{value}' в диапазоне");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка замены '{placeholder}' в диапазоне: {ex.Message}");
            }
        }

        //Поиск таблицы по тексту в Word
        private Microsoft.Office.Interop.Word.Table FindTableByText(Microsoft.Office.Interop.Word.Document doc, string searchText)
        {
            try
            {
                foreach (Microsoft.Office.Interop.Word.Table table in doc.Tables)
                {
                    Microsoft.Office.Interop.Word.Range tableRange = table.Range;
                    tableRange.Find.ClearFormatting();
                    tableRange.Find.Text = searchText;
                    if (tableRange.Find.Execute())
                    {
                        return table;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка поиска таблицы: {ex.Message}");
            }
            return null;
        }

        //Утолщение шрифта в таблице Word
        private void MakeBoldInTable(Microsoft.Office.Interop.Word.Table table, string textToBold)
        {
            try
            {
                foreach (Microsoft.Office.Interop.Word.Row row in table.Rows)
                {
                    foreach (Microsoft.Office.Interop.Word.Cell cell in row.Cells)
                    {
                        string cellText = cell.Range.Text.Trim();
                        cellText = cellText.Replace("\r", "").Replace("\a", "").Replace("\x0007", "");
                        if (cellText.Equals(textToBold, StringComparison.OrdinalIgnoreCase))
                        {
                            cell.Range.Font.Bold = 1;
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при утолщении шрифта в таблице: {ex.Message}");
            }
        }

        //Замена таблицы с товарами в Word
        private void ReplaceTable2WithOrderItems(Microsoft.Office.Interop.Word.Document doc, Microsoft.Office.Interop.Word.Application wordApp, System.Data.DataTable items)
        {
            try
            {
                if (doc.Tables.Count < 2)
                {
                    InsertOrderTableAtEnd(doc, wordApp, items);
                    return;
                }

                Microsoft.Office.Interop.Word.Table oldTable = doc.Tables[2];
                Microsoft.Office.Interop.Word.Range position = oldTable.Range;
                position.Collapse(Microsoft.Office.Interop.Word.WdCollapseDirection.wdCollapseStart);
                oldTable.Delete();

                if (items == null || items.Rows.Count == 0)
                {
                    position.Text = "Заказ не содержит товаров";
                    position.InsertParagraphAfter();
                    return;
                }

                Microsoft.Office.Interop.Word.Table table = doc.Tables.Add(position, items.Rows.Count + 1, 5);
                FormatOrderTable(table, wordApp, items);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка замены таблицы 2: {ex.Message}");
                InsertOrderTableAtEnd(doc, wordApp, items);
            }
        }

        //Вставка таблицы с товарами в конец документа
        private void InsertOrderTableAtEnd(Microsoft.Office.Interop.Word.Document doc, Microsoft.Office.Interop.Word.Application wordApp, System.Data.DataTable items)
        {
            if (items == null || items.Rows.Count == 0) return;

            Microsoft.Office.Interop.Word.Range endRange = doc.Range(doc.Content.End - 1, doc.Content.End - 1);
            Microsoft.Office.Interop.Word.Table table = doc.Tables.Add(endRange, items.Rows.Count + 1, 5);
            FormatOrderTable(table, wordApp, items);
        }

        //Форматирование таблицы в Word
        private void FormatOrderTable(Microsoft.Office.Interop.Word.Table table, Microsoft.Office.Interop.Word.Application wordApp, System.Data.DataTable items)
        {
            table.PreferredWidth = wordApp.CentimetersToPoints(16);
            table.AllowAutoFit = true;

            table.Columns[1].PreferredWidth = wordApp.CentimetersToPoints(1);
            table.Columns[2].PreferredWidth = wordApp.CentimetersToPoints(8);
            table.Columns[3].PreferredWidth = wordApp.CentimetersToPoints(2.5f);
            table.Columns[4].PreferredWidth = wordApp.CentimetersToPoints(2);
            table.Columns[5].PreferredWidth = wordApp.CentimetersToPoints(2.5f);

            table.Cell(1, 1).Range.Text = "№";
            table.Cell(1, 2).Range.Text = "Наименование";
            table.Cell(1, 3).Range.Text = "Цена";
            table.Cell(1, 4).Range.Text = "Кол-во";
            table.Cell(1, 5).Range.Text = "Сумма";

            table.Rows[1].Range.Font.Bold = 1;
            table.Rows[1].Range.Font.Size = 10;
            table.Rows[1].Range.Font.Name = "Arial";
            table.Rows[1].Shading.BackgroundPatternColor = Microsoft.Office.Interop.Word.WdColor.wdColorGray15;

            for (int col = 1; col <= 5; col++)
            {
                table.Cell(1, col).Range.ParagraphFormat.Alignment = Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphCenter;
                table.Cell(1, col).VerticalAlignment = Microsoft.Office.Interop.Word.WdCellVerticalAlignment.wdCellAlignVerticalCenter;
            }

            for (int i = 0; i < items.Rows.Count; i++)
            {
                DataRow row = items.Rows[i];
                decimal price = Convert.ToDecimal(row["Price"]);

                int quantity;
                if (items.Columns.Contains("Quantity"))
                    quantity = Convert.ToInt32(row["Quantity"]);
                else if (items.Columns.Contains("Count"))
                    quantity = Convert.ToInt32(row["Count"]);
                else
                    quantity = 0;

                decimal total = price * quantity;

                int rowIdx = i + 2;
                table.Cell(rowIdx, 1).Range.Text = (i + 1).ToString();
                table.Cell(rowIdx, 2).Range.Text = row["Name"].ToString();
                table.Cell(rowIdx, 3).Range.Text = price.ToString("C2");
                table.Cell(rowIdx, 4).Range.Text = quantity.ToString();
                table.Cell(rowIdx, 5).Range.Text = total.ToString("C2");

                table.Cell(rowIdx, 1).Range.ParagraphFormat.Alignment = Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphCenter;
                table.Cell(rowIdx, 2).Range.ParagraphFormat.Alignment = Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphLeft;
                table.Cell(rowIdx, 3).Range.ParagraphFormat.Alignment = Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphCenter;
                table.Cell(rowIdx, 4).Range.ParagraphFormat.Alignment = Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphCenter;
                table.Cell(rowIdx, 5).Range.ParagraphFormat.Alignment = Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphCenter;

                for (int col = 1; col <= 5; col++)
                {
                    table.Cell(rowIdx, col).VerticalAlignment = Microsoft.Office.Interop.Word.WdCellVerticalAlignment.wdCellAlignVerticalCenter;
                    table.Cell(rowIdx, col).Range.Font.Size = 9;
                    table.Cell(rowIdx, col).Range.Font.Name = "Arial";
                }
            }

            table.Borders.Enable = 1;
            table.Borders.InsideLineStyle = Microsoft.Office.Interop.Word.WdLineStyle.wdLineStyleSingle;
            table.Borders.OutsideLineStyle = Microsoft.Office.Interop.Word.WdLineStyle.wdLineStyleSingle;
            table.Borders.InsideLineWidth = Microsoft.Office.Interop.Word.WdLineWidth.wdLineWidth050pt;
            table.Borders.OutsideLineWidth = Microsoft.Office.Interop.Word.WdLineWidth.wdLineWidth050pt;

            for (int col = 1; col <= 5; col++)
            {
                table.Cell(1, col).Range.Font.Size = 10;
                table.Cell(1, col).Range.Font.Name = "Arial";
            }
        }

        //Получение пути к шаблону
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

        //Добавление служебной информации в предварительный документ
        private void AddServiceInfoToPreliminaryWord(Microsoft.Office.Interop.Word.Document doc)
        {
            Microsoft.Office.Interop.Word.Range range = doc.Range(doc.Content.End - 1, doc.Content.End - 1);
            range.InsertParagraphAfter();

            range.Text = new string('_', 80);
            range.Font.Size = 8;
            range.Font.Color = Microsoft.Office.Interop.Word.WdColor.wdColorGray50;
            range.InsertParagraphAfter();

            range = doc.Range(doc.Content.End - 1, doc.Content.End - 1);
            range.Text = "ВНИМАНИЕ: Предоплата в случае отмены заказа НЕ ВОЗВРАЩАЕТСЯ!";
            range.Font.Size = 12;
            range.Font.Bold = 1;
            range.Font.Italic = 1;
            range.Font.Color = Microsoft.Office.Interop.Word.WdColor.wdColorGray50;
            range.ParagraphFormat.Alignment = Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphCenter;
            range.InsertParagraphAfter();

            string fullname = Properties.Settings.Default.userName;
            string formattedname = FormatFullName(fullname);

            range = doc.Range(doc.Content.End - 1, doc.Content.End - 1);
            range.Text = $"Документ сгенерирован: {DateTime.Now:dd.MM.yyyy HH:mm:ss}\rСотрудник: {formattedname}";
            range.Font.Size = 10;
            range.Font.Italic = 1;
            range.Font.Bold = 0;
            range.Font.Color = Microsoft.Office.Interop.Word.WdColor.wdColorGray50;
            range.ParagraphFormat.Alignment = Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphCenter;
        }

        //Добавление служебной информации в итоговый документ
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

        //Форматирование ФИО
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

        //Расчет скидки
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

        //Расчет общей суммы
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