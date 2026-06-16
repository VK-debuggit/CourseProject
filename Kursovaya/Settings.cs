using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Kursovaya
{
    public partial class Settings : Form
    {
        string conString = $"host={Properties.Settings.Default.host};uid={Properties.Settings.Default.uid};pwd={Properties.Settings.Default.pwd};database={Properties.Settings.Default.database};";
        string serverConnectionString = $"host={Properties.Settings.Default.host};uid={Properties.Settings.Default.uid};pwd={Properties.Settings.Default.pwd};";
        string filePathForImport;
        string filePathForExport;

        //Получение пути к папке с бэкапами
        private string GetBackupsPath()
        {
            string exeDirectory = Path.GetDirectoryName(Application.ExecutablePath);
            return Path.Combine(exeDirectory, "CafeManagement", "Backups");
        }

        //Получение пути к папке с экспортами
        private string GetExportsPath()
        {
            string exeDirectory = Path.GetDirectoryName(Application.ExecutablePath);
            return Path.Combine(exeDirectory, "CafeManagement", "Exports");
        }

        public Settings()
        {
            InitializeComponent();

            comboBox2.SelectedIndexChanged += ComboBox2_SelectedIndexChanged;

            button1.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button2.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button3.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            button4.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
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

            SelectAllTablesFromDB();
            comboBox1.SelectedIndex = -1;
            comboBox2.SelectedIndex = -1;
        }

        //Восстановление базы данных из SQL файла
        private void RestoreDatabaseFromSqlFile()
        {
            try
            {
                string sqlFilePath = Path.Combine(Application.StartupPath, "Resources", "cafeoptionsStructure.sql");

                if (!File.Exists(sqlFilePath))
                {
                    MessageBox.Show($"Файл для восстановления БД не найден по пути:\n{sqlFilePath}\n\n" +
                                  "Убедитесь, что файл 'cafeoptionsStructure.sql' находится в папке 'Resources' программы.",
                                  "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string sqlScript = File.ReadAllText(sqlFilePath, Encoding.UTF8);
                string databaseName = Properties.Settings.Default.database;

                if (string.IsNullOrEmpty(databaseName))
                {
                    MessageBox.Show("Имя базы данных не указано в настройках",
                                  "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                Cursor = Cursors.WaitCursor;

                using (MySqlConnection connection = new MySqlConnection(serverConnectionString))
                {
                    connection.Open();

                    string dropDatabaseQuery = $"DROP DATABASE IF EXISTS `{databaseName}`;";
                    using (MySqlCommand dropCmd = new MySqlCommand(dropDatabaseQuery, connection))
                    {
                        dropCmd.ExecuteNonQuery();
                    }

                    string createDatabaseQuery = $"CREATE DATABASE `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;";
                    using (MySqlCommand createCmd = new MySqlCommand(createDatabaseQuery, connection))
                    {
                        createCmd.ExecuteNonQuery();
                    }

                    string useDatabaseQuery = $"USE `{databaseName}`;";
                    using (MySqlCommand useCmd = new MySqlCommand(useDatabaseQuery, connection))
                    {
                        useCmd.ExecuteNonQuery();
                    }

                    using (MySqlCommand disableFK = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 0;", connection))
                    {
                        disableFK.ExecuteNonQuery();
                    }

                    string[] lines = sqlScript.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    StringBuilder currentCommand = new StringBuilder();
                    int executedCount = 0;
                    int errorCount = 0;
                    List<string> errorMessages = new List<string>();
                    List<string> successfulTables = new List<string>();

                    foreach (string line in lines)
                    {
                        string trimmedLine = line.Trim();

                        if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("--") || trimmedLine.StartsWith("/*"))
                            continue;

                        currentCommand.Append(line);

                        if (trimmedLine.EndsWith(";"))
                        {
                            string command = currentCommand.ToString().Trim();
                            currentCommand.Clear();

                            if (!string.IsNullOrEmpty(command))
                            {
                                try
                                {
                                    using (MySqlCommand cmd = new MySqlCommand(command, connection))
                                    {
                                        cmd.ExecuteNonQuery();
                                        executedCount++;

                                        if (command.ToUpper().Contains("CREATE TABLE"))
                                        {
                                            string tableName = ExtractTableName(command);
                                            if (!string.IsNullOrEmpty(tableName))
                                                successfulTables.Add(tableName);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    errorCount++;
                                    errorMessages.Add($"Ошибка: {ex.Message}\nКоманда: {command.Substring(0, Math.Min(100, command.Length))}...");
                                }
                            }
                        }
                    }

                    using (MySqlCommand enableFK = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 1;", connection))
                    {
                        enableFK.ExecuteNonQuery();
                    }

                    string resultMessage = $"Восстановление базы данных завершено!";

                    if (errorMessages.Count > 0)
                    {
                        resultMessage += $"Ошибки:\n{string.Join("\n", errorMessages.Take(3))}";
                        if (errorMessages.Count > 3)
                            resultMessage += $"\n... и еще {errorMessages.Count - 3} ошибок";
                    }

                    MessageBox.Show(resultMessage,
                                   errorCount > 0 ? "Восстановление завершено с ошибками" : "Успех",
                                   MessageBoxButtons.OK,
                                   errorCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

                    conString = $"host={Properties.Settings.Default.host};uid={Properties.Settings.Default.uid};pwd={Properties.Settings.Default.pwd};database={databaseName};";
                    SelectAllTablesFromDB();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при восстановлении базы данных:\n\n{ex.Message}\n\n{ex.StackTrace}",
                               "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        //Извлечение имени таблицы из команды CREATE TABLE
        private string ExtractTableName(string createCommand)
        {
            try
            {
                int startIndex = createCommand.ToUpper().IndexOf("CREATE TABLE");
                if (startIndex == -1) return "";

                string afterCreate = createCommand.Substring(startIndex + 12);
                string[] parts = afterCreate.Trim().Split(new[] { ' ', '(', '`' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length > 0)
                {
                    string tableName = parts[0].Trim('`', ' ', '\t');
                    return tableName;
                }
            }
            catch { }
            return "";
        }

        //Автоматическое формирование пути при выборе таблицы для экспорта
        private void ComboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox2.SelectedItem != null)
            {
                string russianTableName = comboBox2.SelectedItem.ToString();
                string tableName = GetEnglishTableName(russianTableName);

                string defaultFileName = $"Export_{tableName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                string exportFolder = GetExportsPath();

                if (!Directory.Exists(exportFolder))
                {
                    Directory.CreateDirectory(exportFolder);
                }

                filePathForExport = Path.Combine(exportFolder, defaultFileName);
                textBox1.Text = filePathForExport;
            }
        }

        //Получение списка всех таблиц
        void SelectAllTablesFromDB()
        {
            try
            {
                List<string> allTables = new List<string>
                {
                    "Roles",
                    "Users",
                    "Categories",
                    "Events",
                    "Schedule",
                    "Status",
                    "Clients",
                    "Dishes",
                    "Orders",
                    "OrderComposition"
                };

                List<string> filteredTables = new List<string>();

                if (label1.Text == "По умолчанию")
                {
                    if (allTables.Contains("Roles"))
                    {
                        filteredTables.Add("Roles");
                    }
                }
                else
                {
                    filteredTables = allTables.Where(t => t != "Roles").ToList();
                }

                comboBox1.Items.Clear();
                comboBox2.Items.Clear();

                foreach (string englishName in filteredTables)
                {
                    string russianName = GetRussianTableName(englishName);
                    comboBox1.Items.Add(russianName);
                    comboBox2.Items.Add(russianName);
                }

                if (comboBox1.Items.Count > 0)
                    comboBox1.SelectedIndex = -1;
                if (comboBox2.Items.Count > 0)
                    comboBox2.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                comboBox1.Items.Clear();
                comboBox2.Items.Clear();

                if (label1.Text == "По умолчанию")
                {
                    comboBox1.Items.Add("Роли");
                    comboBox2.Items.Add("Роли");
                }
                else
                {
                    string[] defaultTables = {
                "Пользователи", "Категории", "Мероприятия",
                "Расписание", "Статусы", "Клиенты",
                "Блюда", "Заказы", "Состав заказа"
            };
                    comboBox1.Items.AddRange(defaultTables);
                    comboBox2.Items.AddRange(defaultTables);
                }

                comboBox1.SelectedIndex = -1;
                comboBox2.SelectedIndex = -1;

                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
        }

        //Получение русского названия таблицы
        private string GetRussianTableName(string englishTableName)
        {
            Dictionary<string, string> tableNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Roles", "Роли" },
                { "Users", "Пользователи" },
                { "Categories", "Категории" },
                { "Events", "Мероприятия" },
                { "Schedule", "Расписание" },
                { "Status", "Статусы" },
                { "Clients", "Клиенты" },
                { "Dishes", "Блюда" },
                { "Orders", "Заказы" },
                { "OrderComposition", "Состав заказа" }
            };

            if (tableNames.ContainsKey(englishTableName))
                return tableNames[englishTableName];

            return englishTableName;
        }

        //Получение английского названия таблицы по русскому
        private string GetEnglishTableName(string russianTableName)
        {
            Dictionary<string, string> tableNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Роли", "Roles" },
                { "Пользователи", "Users" },
                { "Категории", "Categories" },
                { "Мероприятия", "Events" },
                { "Расписание", "Schedule" },
                { "Статусы", "Status" },
                { "Клиенты", "Clients" },
                { "Блюда", "Dishes" },
                { "Заказы", "Orders" },
                { "Состав заказа", "OrderComposition" }
            };

            if (tableNames.ContainsKey(russianTableName))
                return tableNames[russianTableName];

            return russianTableName;
        }

        //Получение схемы таблицы
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

        //Получение первичного ключа таблицы
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

        //Определение кодировки файла
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

        //Парсинг CSV строки
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
                else if (c == ';' && !inQuotes)
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

        //Получение значения из CSV с учетом типа данных
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

        //Выбор файла для импорта
        private void button2_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "CSV files (*.csv)|*.csv";
                openFileDialog.Title = "Выберите CSV файл для импорта";

                string exportsPath = GetExportsPath();
                if (Directory.Exists(exportsPath))
                {
                    openFileDialog.InitialDirectory = exportsPath;
                }
                else
                {
                    Directory.CreateDirectory(exportsPath);
                    openFileDialog.InitialDirectory = exportsPath;
                }

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filePathForImport = openFileDialog.FileName;
                    textBox2.Text = openFileDialog.FileName;
                }
            }
        }

        //Импорт данных в таблицу
        private void button3_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите таблицу для импорта", "Информирование", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string russianTableName = comboBox1.SelectedItem.ToString();
            string tableName = GetEnglishTableName(russianTableName);

            if (string.IsNullOrEmpty(filePathForImport) || !File.Exists(filePathForImport))
            {
                MessageBox.Show("Пожалуйста, выберите CSV файл для импорта", "Информирование", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;

                using (MySqlConnection connection = new MySqlConnection(conString))
                {
                    connection.Open();

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

                    Encoding fileEncoding = DetectFileEncoding(filePathForImport);
                    var allLines = File.ReadAllLines(filePathForImport, fileEncoding);

                    if (allLines.Length < 2)
                    {
                        MessageBox.Show("CSV файл не содержит данных", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    string[] csvHeaders = ParseCSVLine(allLines[0]);

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

                    if (!csvColumnMapping.ContainsKey(primaryKey))
                    {
                        MessageBox.Show($"В CSV файле не найден столбец первичного ключа '{primaryKey}'. Импорт невозможен.",
                                      "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

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
                        using (MySqlCommand cmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 0", connection, transaction))
                        {
                            cmd.ExecuteNonQuery();
                        }

                        string truncateQuery = $"TRUNCATE TABLE `{tableName}`";
                        using (MySqlCommand cmd = new MySqlCommand(truncateQuery, connection, transaction))
                        {
                            cmd.ExecuteNonQuery();
                        }

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

                        using (MySqlCommand cmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 1", connection, transaction))
                        {
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();

                        MessageBox.Show($"Импорт таблицы успешно завершен!\n" +
                                       $"Добавлено записей: {insertedCount}\n" +
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

        //Построение запроса INSERT
        private string BuildInsertQuery(string tableName, List<string> columns)
        {
            string columnsList = string.Join(", ", columns.Select(c => $"`{c}`"));
            string parametersList = string.Join(", ", columns.Select(c => $"@{c}"));
            return $"INSERT INTO `{tableName}` ({columnsList}) VALUES ({parametersList})";
        }

        //Экспорт данных из таблицы
        private void button5_Click(object sender, EventArgs e)
        {
            if (comboBox2.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите таблицу для экспорта", "Информирование", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string russianTableName = comboBox2.SelectedItem.ToString();
            string tableName = GetEnglishTableName(russianTableName);

            if (string.IsNullOrEmpty(filePathForExport))
            {
                MessageBox.Show("Путь для сохранения не указан", "Информирование", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;

                DataTable dataTable = new DataTable();

                using (MySqlConnection connection = new MySqlConnection(conString))
                {
                    connection.Open();

                    string query = $"SELECT * FROM `{tableName}`";
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection))
                    {
                        adapter.Fill(dataTable);
                    }

                    connection.Close();
                }

                if (dataTable.Rows.Count == 0)
                {
                    MessageBox.Show($"В таблице '{tableName}' нет данных для экспорта.",
                                  "Информирование", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string directory = Path.GetDirectoryName(filePathForExport);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (StreamWriter writer = new StreamWriter(filePathForExport, false, Encoding.UTF8))
                {
                    for (int i = 0; i < dataTable.Columns.Count; i++)
                    {
                        writer.Write(dataTable.Columns[i].ColumnName);
                        if (i < dataTable.Columns.Count - 1)
                            writer.Write(";");
                    }
                    writer.WriteLine();

                    for (int rowIndex = 0; rowIndex < dataTable.Rows.Count; rowIndex++)
                    {
                        DataRow row = dataTable.Rows[rowIndex];
                        for (int colIndex = 0; colIndex < dataTable.Columns.Count; colIndex++)
                        {
                            object value = row[colIndex];
                            string strValue = "";

                            if (value == DBNull.Value)
                            {
                                strValue = "";
                            }
                            else if (value is DateTime dateValue)
                            {
                                strValue = dateValue.ToString("yyyy-MM-dd");
                            }
                            else
                            {
                                strValue = value.ToString();
                            }

                            if (strValue.Contains(";") || strValue.Contains("\"") || strValue.Contains("\n") || strValue.Contains("\r"))
                            {
                                strValue = "\"" + strValue.Replace("\"", "\"\"") + "\"";
                            }

                            writer.Write(strValue);
                            if (colIndex < dataTable.Columns.Count - 1)
                                writer.Write(";");
                        }
                        writer.WriteLine();
                    }
                }

                MessageBox.Show($"Экспорт таблицы успешно завершен!\n" +
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

        //Восстановление базы данных
        private void button1_Click(object sender, EventArgs e)
        {
            if (label1.Text == "По умолчанию")
            {
                RestoreDatabaseFromSqlFile();
            }
            else
            {
                RestoreFullDatabaseFromFile();
            }
        }

        //Преобразование формата даты
        private string FixDateFormat(string sqlScript)
        {
            Regex dateRegex = new Regex(@"'(\d{2})\.(\d{2})\.(\d{4})(?:\s+\d{1,2}:\d{2}:\d{2})?'");

            string fixedScript = dateRegex.Replace(sqlScript, match =>
            {
                string day = match.Groups[1].Value;
                string month = match.Groups[2].Value;
                string year = match.Groups[3].Value;
                return $"'{year}-{month}-{day}'";
            });

            return fixedScript;
        }

        //Полное восстановление базы данных из файла
        private void RestoreFullDatabaseFromFile()
        {
            try
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "SQL files (*.sql)|*.sql|All files (*.*)|*.*";
                    openFileDialog.Title = "Выберите файл резервной копии для восстановления";

                    string backupsPath = GetBackupsPath();
                    if (Directory.Exists(backupsPath))
                    {
                        openFileDialog.InitialDirectory = backupsPath;
                    }
                    else
                    {
                        Directory.CreateDirectory(backupsPath);
                        openFileDialog.InitialDirectory = backupsPath;
                    }

                    if (openFileDialog.ShowDialog() != DialogResult.OK)
                        return;

                    string sqlFilePath = openFileDialog.FileName;

                    string sqlScript = File.ReadAllText(sqlFilePath, Encoding.UTF8);
                    sqlScript = FixDateFormat(sqlScript);

                    string databaseName = Properties.Settings.Default.database;

                    Cursor = Cursors.WaitCursor;

                    using (MySqlConnection connection = new MySqlConnection(serverConnectionString))
                    {
                        connection.Open();

                        using (MySqlCommand cmd = new MySqlCommand($"DROP DATABASE IF EXISTS `{databaseName}`;", connection))
                            cmd.ExecuteNonQuery();

                        using (MySqlCommand cmd = new MySqlCommand($"CREATE DATABASE `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;", connection))
                            cmd.ExecuteNonQuery();

                        using (MySqlCommand cmd = new MySqlCommand($"USE `{databaseName}`;", connection))
                            cmd.ExecuteNonQuery();

                        using (MySqlCommand cmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 0;", connection))
                            cmd.ExecuteNonQuery();

                        string[] lines = sqlScript.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        StringBuilder currentCommand = new StringBuilder();
                        int executedCount = 0;
                        int errorCount = 0;
                        List<string> errorMessages = new List<string>();
                        List<string> successfulTables = new List<string>();

                        foreach (string line in lines)
                        {
                            string trimmedLine = line.Trim();

                            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("--") || trimmedLine.StartsWith("/*"))
                                continue;

                            currentCommand.Append(line);

                            if (trimmedLine.EndsWith(";"))
                            {
                                string command = currentCommand.ToString().Trim();
                                currentCommand.Clear();

                                if (!string.IsNullOrEmpty(command))
                                {
                                    try
                                    {
                                        using (MySqlCommand cmd = new MySqlCommand(command, connection))
                                        {
                                            cmd.ExecuteNonQuery();
                                            executedCount++;

                                            if (command.ToUpper().Contains("CREATE TABLE"))
                                            {
                                                string tableName = ExtractTableName(command);
                                                if (!string.IsNullOrEmpty(tableName))
                                                    successfulTables.Add(tableName);
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        errorCount++;
                                        errorMessages.Add($"Ошибка: {ex.Message}\nКоманда: {command.Substring(0, Math.Min(100, command.Length))}...");
                                    }
                                }
                            }
                        }

                        using (MySqlCommand cmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 1;", connection))
                            cmd.ExecuteNonQuery();

                        string resultMessage = $"Восстановление базы данных завершено!\n" +
                                               $"Ошибок: {errorCount}\n" +
                                               $"Создано таблиц: {successfulTables.Count}\n";

                        if (errorMessages.Count > 0)
                        {
                            resultMessage += $"Ошибки:\n{string.Join("\n", errorMessages.Take(3))}";
                            if (errorMessages.Count > 3)
                                resultMessage += $"\n... и еще {errorMessages.Count - 3} ошибок";
                        }

                        MessageBox.Show(resultMessage,
                                       errorCount > 0 ? "Восстановление завершено с ошибками" : "Успех",
                                       MessageBoxButtons.OK,
                                       errorCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

                        conString = $"host={Properties.Settings.Default.host};uid={Properties.Settings.Default.uid};pwd={Properties.Settings.Default.pwd};database={databaseName};";
                        SelectAllTablesFromDB();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при восстановлении: {ex.Message}\n\n{ex.StackTrace}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        //Возврат в главное меню
        private void button6_Click(object sender, EventArgs e)
        {
            this.Visible = false;
            MainFormAdmin mainFormAdmin = new MainFormAdmin();
            mainFormAdmin.ShowDialog();
            this.Close();
        }

        //Обработчик загрузки формы
        private void Settings_Load(object sender, EventArgs e)
        {
            if (label1.Text == "По умолчанию")
            {
                label4.Visible = false;
                label5.Visible = false;
                comboBox2.Visible = false;
                textBox1.Visible = false;
                button4.Visible = false;
                button5.Visible = false;
                Height = 372;
                label3.Location = new Point(label3.Location.X, label3.Location.Y - 54);
                comboBox1.Location = new Point(comboBox1.Location.X, comboBox1.Location.Y - 54);
                label6.Location = new Point(label6.Location.X, label6.Location.Y - 54);
                textBox2.Location = new Point(textBox2.Location.X, textBox2.Location.Y - 54);
                button2.Location = new Point(button2.Location.X, button2.Location.Y - 54);
                button3.Location = new Point(button3.Location.X, button3.Location.Y - 54);

                comboBox1.Items.Clear();
                comboBox1.Items.Add("Роли");
                comboBox1.SelectedIndex = -1;
            }
        }

        //Создание резервной копии
        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                using (MySqlConnection testCon = new MySqlConnection(conString))
                {
                    testCon.Open();
                    testCon.Close();
                }

                string backupBasePath = GetBackupsPath();
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string backupFolder = Path.Combine(backupBasePath, $"Manual_Backup_{timestamp}");

                bool success = BackupManager.CreateManualBackupToFolder(backupFolder);

                if (success)
                {
                    MessageBox.Show(
                        $"Резервная копия успешно создана!\n" +
                        $"Папка с бэкапом: {backupFolder}\n\n" +
                        $"Содержит:\n" +
                        $"- backup.sql\n" +
                        $"- CSV-файлы всех таблиц",
                        "Успех",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                else
                {
                    MessageBox.Show(
                        "Ошибка при создании резервной копии.\n" +
                        "Проверьте подключение к базе данных.",
                        "Ошибка",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}",
                               "Ошибка",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Error);
            }
        }
    }
}