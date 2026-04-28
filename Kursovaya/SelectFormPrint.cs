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
        private OrderData _orderData;  // поле класса
        private decimal _discountAmountValue;
        private DocumentType _documentType;
        private ViewingAnOrder _viewingAnOrderForm;
        private decimal _additionalExpenses;

        // Конструктор для предварительного документа (из ViewingAnOrderForMeneger)
        public SelectFormPrint(System.Data.DataTable cartItems, OrderData orderData, decimal discountAmountValue, DocumentType type)
        {
            InitializeComponent();
            this._cartItems = cartItems;
            this._orderData = orderData;
            this._discountAmountValue = discountAmountValue;
            this._documentType = type;
            this._viewingAnOrderForm = null;
            button1.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button2.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
        }

        // Конструктор для окончательного документа (из ViewingAnOrder)
        public SelectFormPrint(OrderData orderData, System.Data.DataTable orderItems, DocumentType type, ViewingAnOrder parentForm, decimal additionalExpenses)
        {
            InitializeComponent();
            this._orderData = orderData;
            this._cartItems = orderItems;
            this._documentType = type;
            this._viewingAnOrderForm = parentForm;
            this._additionalExpenses = additionalExpenses;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (_documentType == DocumentType.Preliminary)
            {
                GeneratePreliminaryWordTicket();
            }
            else
            {
                GenerateFinalWordTicket();
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        // Генерация предварительного документа (firstblank.docx)
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
                AddServiceInfoToPreliminaryWord(doc); // Используем отдельный метод для предварительного

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

        // Генерация окончательного документа (secondblank.docx)
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

                // Используем данные из заказа с учетом дополнительных расходов
                decimal totalAmount = _orderData.TotalAmount + _additionalExpenses;
                decimal discountAmount = _orderData.DiscountAmount;
                decimal finalAmount = (_orderData.FinalAmount > 0 ? _orderData.FinalAmount : _orderData.TotalAmount - discountAmount) + _additionalExpenses;
                decimal prepayment = _orderData.Prepayment;

                // Рассчитываем процент скидки от новой общей суммы
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
                AddServiceInfoToFinalWord(doc, _orderData.NameUser ?? "Не указан"); // Используем отдельный метод для окончательного

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

                // Определяем название колонки для количества
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

        // Метод для предварительного документа (только информация о генерации)
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

        // Метод для окончательного документа (с информацией о том, кто оформил заказ)
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

                // Определяем название колонки для количества
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