using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Kursovaya
{
    public partial class Settings : Form
    {
        string conString = $"host={Properties.Settings.Default.host};uid={Properties.Settings.Default.uid};pwd={Properties.Settings.Default.pwd};database={Properties.Settings.Default.database};";
        string filePathForImport;
        string filePathForExport;

        public Settings()
        {
            InitializeComponent();

            SelectAllTablesFromDB();

            // Подписываемся на событие выбора таблицы для экспорта
            comboBox2.SelectedIndexChanged += ComboBox2_SelectedIndexChanged;

            button2.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button3.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button5.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button6.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            textBox1.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            textBox2.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            comboBox1.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);
            comboBox2.BackColor = System.Drawing.Color.FromArgb(255, 221, 153);

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
        }

        private string GetAutoIncrementColumn(MySqlConnection connection, string tableName)
        {
            try
            {
                string query = $@"
            SELECT COLUMN_NAME 
            FROM INFORMATION_SCHEMA.COLUMNS 
            WHERE TABLE_SCHEMA = @databaseName 
              AND TABLE_NAME = @tableName 
              AND EXTRA LIKE '%auto_increment%'
            LIMIT 1";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@databaseName", Properties.Settings.Default.database);
                    cmd.Parameters.AddWithValue("@tableName", tableName);

                    object result = cmd.ExecuteScalar();
                    return result?.ToString() ?? "";
                }
            }
            catch
            {
                return "";
            }
        }

        // Автоматическое формирование пути при выборе таблицы для экспорта
        private void ComboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox2.SelectedItem != null)
            {
                string tableName = comboBox2.SelectedItem.ToString();
                string defaultFileName = $"Export_{tableName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                string exportFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CafeExport");

                if (!Directory.Exists(exportFolder))
                {
                    Directory.CreateDirectory(exportFolder);
                }

                filePathForExport = Path.Combine(exportFolder, defaultFileName);
                textBox1.Text = filePathForExport;
            }
        }

        // Получение списка всех таблиц из БД
        void SelectAllTablesFromDB()
        {
            string query = @"
                SELECT table_name 
                FROM information_schema.tables 
                WHERE table_schema = @databaseName 
                  AND table_type = 'BASE TABLE'
                ORDER BY table_name";

            using (MySqlConnection con = new MySqlConnection(conString))
            using (MySqlCommand cmd = new MySqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@databaseName", Properties.Settings.Default.database);
                con.Open();

                comboBox1.Items.Clear();
                comboBox2.Items.Clear();

                using (MySqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        comboBox1.Items.Add(rdr["table_name"].ToString());
                        comboBox2.Items.Add(rdr["table_name"].ToString());
                    }
                }
            }
        }

        // Получение информации о столбцах таблицы
        private List<Dictionary<string, object>> GetTableSchema(MySqlConnection connection, string tableName)
        {
            var schema = new List<Dictionary<string, object>>();

            try
            {
                string query = $"SHOW COLUMNS FROM `{tableName}`";

                using (var cmd = new MySqlCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var columnInfo = new Dictionary<string, object>();
                        columnInfo["ColumnName"] = reader.GetString("Field");
                        columnInfo["DataType"] = reader.GetString("Type");
                        columnInfo["IsNullable"] = reader.GetString("Null") == "YES";
                        columnInfo["Key"] = reader.GetString("Key");
                        columnInfo["Default"] = reader["Default"] == DBNull.Value ? null : reader["Default"].ToString();
                        columnInfo["Extra"] = reader.GetString("Extra");

                        schema.Add(columnInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения схемы: {ex.Message}");
            }

            return schema;
        }

        // Получение первичного ключа таблицы
        private string GetPrimaryKey(MySqlConnection connection, string tableName)
        {
            try
            {
                string query = @"
                    SELECT COLUMN_NAME 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = @databaseName 
                      AND TABLE_NAME = @tableName 
                      AND COLUMN_KEY = 'PRI'
                    LIMIT 1";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@databaseName", Properties.Settings.Default.database);
                    cmd.Parameters.AddWithValue("@tableName", tableName);

                    object result = cmd.ExecuteScalar();
                    return result?.ToString() ?? "";
                }
            }
            catch
            {
                return "";
            }
        }

        // Определение кодировки файла
        private Encoding DetectFileEncoding(string filePath)
        {
            try
            {
                byte[] fileBytes = File.ReadAllBytes(filePath);

                if (fileBytes.Length >= 3 && fileBytes[0] == 0xEF && fileBytes[1] == 0xBB && fileBytes[2] == 0xBF)
                    return Encoding.UTF8;

                string utf8Text = Encoding.UTF8.GetString(fileBytes);
                if (utf8Text.Contains("Администратор") || utf8Text.Contains("Менеджер") ||
                    utf8Text.Contains("Клиент") || utf8Text.Contains("Article"))
                {
                    return Encoding.UTF8;
                }

                string win1251Text = Encoding.GetEncoding(1251).GetString(fileBytes);
                if (win1251Text.Contains("Администратор") || win1251Text.Contains("Менеджер") ||
                    win1251Text.Contains("Клиент") || win1251Text.Contains("Article"))
                {
                    return Encoding.GetEncoding(1251);
                }

                return Encoding.UTF8;
            }
            catch
            {
                return Encoding.UTF8;
            }
        }

        // Парсинг CSV строки с учетом кавычек
        private string[] ParseCSVLine(string line)
        {
            List<string> result = new List<string>();
            bool inQuotes = false;
            StringBuilder currentField = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentField.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }

            result.Add(currentField.ToString());
            return result.ToArray();
        }

        // Получение значения из CSV с учетом типа данных
        private object GetValueFromCSV(string[] dataRow, int csvIndex, List<Dictionary<string, object>> tableSchema, string columnName)
        {
            if (csvIndex >= dataRow.Length)
                return DBNull.Value;

            string value = dataRow[csvIndex].Trim();

            if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                value = value.Substring(1, value.Length - 2);
            }

            if (string.IsNullOrEmpty(value))
                return DBNull.Value;

            var columnSchema = tableSchema.FirstOrDefault(c => string.Equals(c["ColumnName"].ToString(), columnName, StringComparison.OrdinalIgnoreCase));

            if (columnSchema != null)
            {
                string dataType = columnSchema["DataType"].ToString().ToLower();

                if (dataType.Contains("int") || dataType.Contains("bigint") || dataType.Contains("tinyint"))
                {
                    if (int.TryParse(value, out int intValue))
                        return intValue;
                    return DBNull.Value;
                }
                else if (dataType.Contains("decimal") || dataType.Contains("float") || dataType.Contains("double"))
                {
                    if (decimal.TryParse(value, out decimal decValue))
                        return decValue;
                    return DBNull.Value;
                }
                else if (dataType.Contains("datetime") || dataType.Contains("timestamp"))
                {
                    if (DateTime.TryParse(value, out DateTime dateValue))
                        return dateValue;
                    return DBNull.Value;
                }
                else if (dataType.Contains("bit") || dataType.Contains("bool"))
                {
                    if (bool.TryParse(value, out bool boolValue))
                        return boolValue;
                    if (value == "1" || value.ToLower() == "true")
                        return true;
                    if (value == "0" || value.ToLower() == "false")
                        return false;
                    return DBNull.Value;
                }
            }

            return value;
        }

        // Кнопка для выбора файла импорта
        private void button2_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "CSV files (*.csv)|*.csv";
                openFileDialog.Title = "Выберите CSV файл для импорта";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filePathForImport = openFileDialog.FileName;
                    textBox2.Text = openFileDialog.FileName;
                }
            }
        }

        // Кнопка для выполнения импорта (ПОЛНАЯ СИНХРОНИЗАЦИЯ)
        // Кнопка для выполнения импорта (с сохранением ID из CSV)
        private void button3_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите таблицу для импорта", "Информирование", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrEmpty(filePathForImport) || !File.Exists(filePathForImport))
            {
                MessageBox.Show("Пожалуйста, выберите CSV файл для импорта", "Информирование", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string tableName = comboBox1.SelectedItem.ToString();

            // Подтверждение операции
            DialogResult result = MessageBox.Show($"ВНИМАНИЕ! Импорт в таблицу '{tableName}'.\n\n" +
                                                  "Все данные в таблице будут УДАЛЕНЫ и заменены данными из CSV.\n" +
                                                  "ID записей будут сохранены такие же, как в CSV файле.\n\n" +
                                                  "Вы уверены, что хотите продолжить?",
                                                  "Подтверждение полной замены данных",
                                                  MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            try
            {
                Cursor = Cursors.WaitCursor;

                using (MySqlConnection connection = new MySqlConnection(conString))
                {
                    connection.Open();

                    // Получаем схему таблицы
                    var tableSchema = GetTableSchema(connection, tableName);
                    if (tableSchema.Count == 0)
                    {
                        MessageBox.Show($"Не найдены столбцы в таблице {tableName}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    string primaryKey = GetPrimaryKey(connection, tableName);

                    if (string.IsNullOrEmpty(primaryKey))
                    {
                        MessageBox.Show($"В таблице {tableName} не определен первичный ключ. Импорт невозможен.",
                                      "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Читаем CSV файл
                    Encoding fileEncoding = DetectFileEncoding(filePathForImport);
                    var allLines = File.ReadAllLines(filePathForImport, fileEncoding);

                    if (allLines.Length < 2)
                    {
                        MessageBox.Show("CSV файл не содержит данных", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Получаем заголовки CSV
                    string[] csvHeaders = ParseCSVLine(allLines[0]);

                    // Сопоставление столбцов
                    Dictionary<string, int> csvColumnMapping = new Dictionary<string, int>();
                    List<string> allDbColumns = tableSchema.Select(s => s["ColumnName"].ToString()).ToList();

                    for (int i = 0; i < csvHeaders.Length; i++)
                    {
                        string csvColumn = csvHeaders[i].Trim().Replace("\"", "");
                        var matchingDbColumn = allDbColumns.FirstOrDefault(dbCol =>
                            string.Equals(dbCol, csvColumn, StringComparison.OrdinalIgnoreCase));

                        if (matchingDbColumn != null)
                        {
                            csvColumnMapping[matchingDbColumn] = i;
                        }
                    }

                    // Проверяем, что первичный ключ найден в CSV
                    if (!csvColumnMapping.ContainsKey(primaryKey))
                    {
                        MessageBox.Show($"В CSV файле не найден столбец первичного ключа '{primaryKey}'. Импорт невозможен.",
                                      "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Читаем данные из CSV
                    List<Dictionary<string, object>> csvData = new List<Dictionary<string, object>>();

                    for (int lineIndex = 1; lineIndex < allLines.Length; lineIndex++)
                    {
                        if (string.IsNullOrWhiteSpace(allLines[lineIndex])) continue;

                        string[] fields = ParseCSVLine(allLines[lineIndex]);

                        var rowData = new Dictionary<string, object>();

                        foreach (var column in allDbColumns)
                        {
                            if (csvColumnMapping.ContainsKey(column))
                            {
                                int csvIndex = csvColumnMapping[column];
                                object value = GetValueFromCSV(fields, csvIndex, tableSchema, column);
                                rowData[column] = value;
                            }
                            else
                            {
                                rowData[column] = DBNull.Value;
                            }
                        }

                        csvData.Add(rowData);
                    }

                    if (csvData.Count == 0)
                    {
                        MessageBox.Show("CSV файл не содержит данных для импорта", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    using (var transaction = connection.BeginTransaction())
                    {
                        // 1. Отключаем проверку внешних ключей (если есть)
                        using (MySqlCommand cmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 0", connection, transaction))
                        {
                            cmd.ExecuteNonQuery();
                        }

                        // 2. Очищаем таблицу
                        string truncateQuery = $"TRUNCATE TABLE `{tableName}`";
                        using (MySqlCommand cmd = new MySqlCommand(truncateQuery, connection, transaction))
                        {
                            cmd.ExecuteNonQuery();
                        }

                        // 3. Вставляем данные с сохранением ID из CSV
                        string insertSql = BuildInsertQuery(tableName, allDbColumns);
                        int insertedCount = 0;

                        foreach (var rowData in csvData)
                        {
                            using (MySqlCommand cmd = new MySqlCommand(insertSql, connection, transaction))
                            {
                                foreach (var column in allDbColumns)
                                {
                                    object value = rowData.ContainsKey(column) ? rowData[column] : DBNull.Value;
                                    cmd.Parameters.AddWithValue($"@{column}", value);
                                }

                                cmd.ExecuteNonQuery();
                                insertedCount++;
                            }
                        }

                        // 5. Включаем обратно проверку внешних ключей
                        using (MySqlCommand cmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 1", connection, transaction))
                        {
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();

                        MessageBox.Show($"Импорт таблицы '{tableName}' успешно завершен!\n\n" +
                                       $"Добавлено записей: {insertedCount}\n" +
                                       $"Все ID сохранены из CSV файла.\n\n" +
                                       $"Таблица полностью заменена данными из CSV.",
                                       "Результат импорта", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }

                textBox2.Text = "";
                comboBox1.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка импорта: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private string BuildUpdateQuery(string tableName, List<string> columns, string primaryKey)
        {
            var setClauses = new List<string>();
            foreach (var column in columns)
            {
                if (!string.Equals(column, primaryKey, StringComparison.OrdinalIgnoreCase))
                {
                    setClauses.Add($"`{column}` = @{column}");
                }
            }
            string setClause = string.Join(", ", setClauses);
            return $"UPDATE `{tableName}` SET {setClause} WHERE `{primaryKey}` = @{primaryKey}";
        }

        private string BuildInsertQuery(string tableName, List<string> columns)
        {
            string columnsList = string.Join(", ", columns.Select(c => $"`{c}`"));
            string parametersList = string.Join(", ", columns.Select(c => $"@{c}"));
            return $"INSERT INTO `{tableName}` ({columnsList}) VALUES ({parametersList})";
        }

        // Кнопка для выполнения экспорта
        // Кнопка для выполнения экспорта - ГАРАНТИРОВАННО РАБОЧАЯ ВЕРСИЯ
        private void button5_Click(object sender, EventArgs e)
        {
            if (comboBox2.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите таблицу для экспорта", "Информирование", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrEmpty(filePathForExport))
            {
                MessageBox.Show("Путь для сохранения не указан", "Информирование", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string tableName = comboBox2.SelectedItem.ToString();

            try
            {
                Cursor = Cursors.WaitCursor;

                // Создаем DataTable для данных
                DataTable dataTable = new DataTable();

                using (MySqlConnection connection = new MySqlConnection(conString))
                {
                    connection.Open();

                    // Получаем данные
                    string query = $"SELECT * FROM `{tableName}`";
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection))
                    {
                        adapter.Fill(dataTable);
                    }

                    connection.Close();
                }

                // Проверяем, есть ли данные
                if (dataTable.Rows.Count == 0)
                {
                    MessageBox.Show($"В таблице '{tableName}' нет данных для экспорта.",
                                  "Информирование", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Создаем директорию для сохранения
                string directory = Path.GetDirectoryName(filePathForExport);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Записываем в CSV
                using (StreamWriter writer = new StreamWriter(filePathForExport, false, Encoding.UTF8))
                {
                    // Записываем заголовки
                    for (int i = 0; i < dataTable.Columns.Count; i++)
                    {
                        writer.Write(dataTable.Columns[i].ColumnName);
                        if (i < dataTable.Columns.Count - 1)
                            writer.Write(",");
                    }
                    writer.WriteLine();

                    // Записываем данные
                    for (int rowIndex = 0; rowIndex < dataTable.Rows.Count; rowIndex++)
                    {
                        DataRow row = dataTable.Rows[rowIndex];
                        for (int colIndex = 0; colIndex < dataTable.Columns.Count; colIndex++)
                        {
                            object value = row[colIndex];
                            string strValue = (value == DBNull.Value) ? "" : value.ToString();

                            // Экранируем специальные символы
                            if (strValue.Contains(",") || strValue.Contains("\"") || strValue.Contains("\n"))
                            {
                                strValue = "\"" + strValue.Replace("\"", "\"\"") + "\"";
                            }

                            writer.Write(strValue);
                            if (colIndex < dataTable.Columns.Count - 1)
                                writer.Write(",");
                        }
                        writer.WriteLine();
                    }
                }

                MessageBox.Show($"Экспорт таблицы '{tableName}' успешно завершен!\n\n" +
                               $"Сохранено записей: {dataTable.Rows.Count}\n" +
                               $"Файл сохранен: {filePathForExport}",
                               "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

                textBox1.Text = "";
                comboBox2.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}\n\nStack Trace: {ex.StackTrace}",
                               "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        // Кнопка возврата в главное меню
        private void button6_Click(object sender, EventArgs e)
        {
            this.Visible = false;
            MainFormAdmin mainFormAdmin = new MainFormAdmin();
            mainFormAdmin.ShowDialog();
            this.Close();
        }
    }
}