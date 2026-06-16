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
    public partial class Categories : Form
    {
        string conString = $"host={Properties.Settings.Default.host};uid={Properties.Settings.Default.uid};pwd={Properties.Settings.Default.pwd};database={Properties.Settings.Default.database};";
        private int selectedProductRowIndex = -1;
        private int rowCount = 0;
        private int? _lastInsertedCategoryId = null;

        public Categories()
        {
            InitializeComponent();

            FillDataGridViewCategory();

            categoryInsert.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            updateCategory.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            deleteCategory.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button4.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            deleteCategory.Enabled = false;
            textBox1.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
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

            if (dataGridView1.CurrentRow == null)
            {
                deleteCategory.Enabled = false;
            }
        }

        private bool allowClose = false;

        //Обработчик кнопки возврата в справочники
        private void button4_Click(object sender, EventArgs e)
        {
            allowClose = true;
            this.Visible = false;
            Directories directories = new Directories();
            directories.ShowDialog();
            this.Close();
        }

        //Обработчик закрытия формы
        private void Categories_FormClosing(object sender, FormClosingEventArgs e)
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

        //Загрузка данных категорий в DataGridView
        void FillDataGridViewCategory()
        {
            string SelectQuery = @"SELECT IDcategory, Category FROM CafeActivities.Categories ORDER BY Category ASC;";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                con.Open();

                using (MySqlCommand cmd = new MySqlCommand(SelectQuery, con))
                using (MySqlDataReader rdr = cmd.ExecuteReader())
                {
                    dataGridView1.Rows.Clear();
                    dataGridView1.Columns.Clear();

                    dataGridView1.Columns.Add("IDcategory", "Id");
                    dataGridView1.Columns["IDcategory"].Visible = false;
                    dataGridView1.Columns.Add("Category", "Категория");

                    rowCount = 0;

                    var categories = new List<(int Id, string Name)>();

                    while (rdr.Read())
                    {
                        int categoryId = Convert.ToInt32(rdr[0]);
                        string categoryName = rdr[1].ToString();
                        categories.Add((categoryId, categoryName));
                        rowCount++;
                    }

                    if (_lastInsertedCategoryId.HasValue)
                    {
                        var newCategory = categories.FirstOrDefault(c => c.Id == _lastInsertedCategoryId.Value);
                        if (newCategory.Id != 0)
                        {
                            categories.Remove(newCategory);
                            categories.Insert(0, newCategory);
                        }
                        _lastInsertedCategoryId = null;
                    }

                    foreach (var category in categories)
                    {
                        dataGridView1.Rows.Add(category.Id, category.Name);
                    }

                    label5.Text = rowCount.ToString();

                    if (rowCount == 0)
                    {
                        MessageBox.Show("Данные не найдены", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        //Ограничение ввода в текстовое поле
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox tb = (TextBox)sender;

            if (char.IsControl(e.KeyChar))
                return;

            if (tb.Text.Length > 0 && char.IsLower(tb.Text[0]))
            {
                int cursorPos = tb.SelectionStart;

                string newText = char.ToUpper(tb.Text[0]) + tb.Text.Substring(1);

                if (tb.Text != newText)
                {
                    tb.Text = newText;
                    tb.SelectionStart = cursorPos;
                }
            }

            if (e.KeyChar == ' ')
            {
                if (tb.Text.Length == 0)
                {
                    e.Handled = true;
                    return;
                }

                if (tb.Text.Length > 0 && tb.Text[tb.Text.Length - 1] == ' ')
                {
                    e.Handled = true;
                    return;
                }

                return;
            }

            if ((e.KeyChar >= 'А' && e.KeyChar <= 'Я') ||
                (e.KeyChar >= 'а' && e.KeyChar <= 'я') ||
                e.KeyChar == 'Ё' || e.KeyChar == 'ё')
                return;

            e.Handled = true;
        }

        //Проверка существования категории в базе данных
        private bool IsCategoryExists(string categoryName)
        {
            string query = "SELECT COUNT(*) FROM Categories WHERE Category = @category;";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@category", categoryName.Trim());

                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка проверки категории: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return true;
                }
            }
        }

        //Проверка существования категории исключая текущую запись
        private bool IsCategoryExistsExceptCurrent(int categoryId, string categoryName)
        {
            string query = "SELECT COUNT(*) FROM Categories WHERE Category = @category AND IDcategory != @categoryId;";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@category", categoryName.Trim());
                        cmd.Parameters.AddWithValue("@categoryId", categoryId);

                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка проверки категории: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return true;
                }
            }
        }

        //Обновление состояния кнопок
        void UpdateButtonsState()
        {
            categoryInsert.Enabled = !string.IsNullOrWhiteSpace(textBox1.Text);
            string currentText = textBox1.Text.Trim();
            bool hasText = !string.IsNullOrWhiteSpace(currentText);

            if (dataGridView1.CurrentRow != null && hasText)
            {
                string originalStatus = dataGridView1.CurrentRow.Cells["Category"].Value?.ToString() ?? "";
                updateCategory.Enabled = (currentText != originalStatus);
            }
            else
            {
                updateCategory.Enabled = false;
            }

            deleteCategory.Enabled = (dataGridView1.CurrentRow != null);
        }

        //Обработчик изменения выделения в DataGridView
        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow != null && dataGridView1.CurrentRow.Index >= 0)
            {
                try
                {
                    DataGridViewRow selectedRow = dataGridView1.CurrentRow;
                    textBox1.Text = selectedRow.Cells["Category"].Value?.ToString() ?? "";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при заполнении полей: {ex.Message}");
                }

                UpdateButtonsState();
            }
        }

        //Проверка использования категории в других таблицах
        private bool IsCategoryInUse(int categoryId)
        {
            string checkQueries = @"SELECT COUNT(*) FROM Dishes WHERE IdCategory = @categoryId;";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();

                    using (MySqlCommand cmd = new MySqlCommand(checkQueries, con))
                    {
                        cmd.Parameters.AddWithValue("@categoryId", categoryId);
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        if (count > 0)
                        {
                            return true;
                        }
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка проверки использования категории: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return true;
                }
            }
        }

        //Очистка всех полей формы
        private void ClearAllFields()
        {
            dataGridView1.ClearSelection();
            dataGridView1.CurrentCell = null;
            textBox1.Text = "";
            dataGridView1.Focus();
            UpdateButtonsState();
        }

        //Обработчик загрузки формы
        private void Categories_Load(object sender, EventArgs e)
        {
            ClearAllFields();
            _lastInsertedCategoryId = null;
        }

        //Обработчик кнопки добавления категории
        private void categoryInsert_Click(object sender, EventArgs e)
        {
            string categoryName = textBox1.Text.Trim();

            if (IsCategoryExists(categoryName))
            {
                MessageBox.Show("Категория с таким наименованием уже существует", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(categoryName))
            {
                MessageBox.Show("Заполните поле категории", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string query = "INSERT INTO Categories (Category) VALUES (@category); SELECT LAST_INSERT_ID();";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@category", categoryName);
                        int newId = Convert.ToInt32(cmd.ExecuteScalar());

                        _lastInsertedCategoryId = newId;

                        MessageBox.Show("Категория успешно добавлена", "Успех",
                                      MessageBoxButtons.OK, MessageBoxIcon.Information);
                        textBox1.Clear();
                        FillDataGridViewCategory();
                        ClearAllFields();

                        if (dataGridView1.Rows.Count > 0)
                        {
                            dataGridView1.Rows[0].Selected = true;
                            dataGridView1.FirstDisplayedScrollingRowIndex = 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка добавления категории: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        //Обработчик кнопки обновления категории
        private void updateCategory_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null)
            {
                MessageBox.Show("Выберите категорию для редактирования", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int selectedId = Convert.ToInt32(dataGridView1.CurrentRow.Cells["IDcategory"].Value);
            string newCategoryName = textBox1.Text.Trim();

            if (IsCategoryExistsExceptCurrent(selectedId, newCategoryName))
            {
                MessageBox.Show("Категория с таким наименованием уже существует", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string query = "UPDATE Categories SET Category = @category WHERE IDcategory = @categoryId";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@category", newCategoryName);
                        cmd.Parameters.AddWithValue("@categoryId", selectedId);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            _lastInsertedCategoryId = null;

                            MessageBox.Show("Категория успешно обновлена", "Успех",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);
                            textBox1.Clear();
                            FillDataGridViewCategory();
                            ClearAllFields();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка обновления категории: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        //Обработчик кнопки удаления категории
        private void deleteCategory_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null)
            {
                MessageBox.Show("Выберите категорию для удаления", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int selectedId = Convert.ToInt32(dataGridView1.CurrentRow.Cells["IDcategory"].Value);
            string categoryName = dataGridView1.CurrentRow.Cells["Category"].Value.ToString();

            DialogResult result = MessageBox.Show(
                $"Вы уверены, что хотите удалить категорию \"{categoryName}\"?",
                "Подтверждение удаления",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            if (IsCategoryInUse(selectedId))
            {
                MessageBox.Show("Невозможно удалить категорию, так как она используется в других таблицах",
                              "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string query = "DELETE FROM Categories WHERE IDcategory = @categoryId";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@categoryId", selectedId);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            _lastInsertedCategoryId = null;

                            MessageBox.Show("Категория успешно удалена", "Успех",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);
                            textBox1.Clear();
                            FillDataGridViewCategory();
                            ClearAllFields();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления категории: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        //Обработчик клика по ячейке DataGridView
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                selectedProductRowIndex = e.RowIndex;
                deleteCategory.Enabled = true;
            }
        }

        //Обработчик завершения привязки данных
        private void dataGridView1_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            dataGridView1.ClearSelection();
        }

        //Установка русской раскладки при установке курсора в текстовое поле
        private void textBox1_Enter(object sender, EventArgs e)
        {
            foreach (InputLanguage lang in InputLanguage.InstalledInputLanguages)
            {
                if (lang.Culture.TwoLetterISOLanguageName == "ru")
                {
                    InputLanguage.CurrentInputLanguage = lang;
                    break;
                }
            }
        }
    }
}