using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kursovaya
{
    class BackupManager
    {
        private static string GetConnectionString()
        {
            return $"host={Properties.Settings.Default.host};uid={Properties.Settings.Default.uid};pwd={Properties.Settings.Default.pwd};database={Properties.Settings.Default.database}";
        }

        public static void CreateBackupOnExit()
        {
            try
            {
                string programDirectory = Application.StartupPath;
                string folderName = "Exit_Backup_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string backupPath = Path.Combine(programDirectory, "Backups", folderName);

                Directory.CreateDirectory(backupPath);

                // Создаем CSV файлы
                var csvTask = Task.Run(() => SaveDatabaseToCsv(backupPath));

                // Создаем SQL файл
                var sqlTask = Task.Run(() => SaveDatabaseToSql(backupPath));

                // Ждем завершения обоих задач максимум 5 секунд
                bool csvCompleted = csvTask.Wait(5000);
                bool sqlCompleted = sqlTask.Wait(5000);

                if (csvCompleted && sqlCompleted)
                {
                    LogBackupOperation($"Полная резервная копия при выходе создана: {backupPath}");
                }
                else if (csvCompleted)
                {
                    LogBackupOperation($"Резервная копия при выходе создана (только CSV): {backupPath}");
                }
                else if (sqlCompleted)
                {
                    LogBackupOperation($"Резервная копия при выходе создана (только SQL): {backupPath}");
                }
                else
                {
                    LogBackupOperation($"Резервная копия при выходе создана частично (таймаут): {backupPath}");
                }
            }
            catch (Exception ex)
            {
                LogBackupOperation($"Ошибка при создании резервной копии при выходе: {ex.Message}");
            }
        }

        public static bool CreateManualBackup(string selectedPath)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string backupFolder = Path.Combine(selectedPath, $"Backup_{timestamp}");
                Directory.CreateDirectory(backupFolder);

                SaveDatabaseToCsv(backupFolder);
                SaveDatabaseToSql(backupFolder);

                LogBackupOperation($"Ручная резервная копия создана: {backupFolder}");
                return true;
            }
            catch (Exception ex)
            {
                LogBackupOperation($"Ошибка при создании ручной резервной копии: {ex.Message}");
                return false;
            }
        }

        private static void SaveDatabaseToCsv(string directoryPath)
        {
            try
            {
                var dataTables = GetAllDataTables();

                foreach (var table in dataTables)
                {
                    string filePath = Path.Combine(directoryPath, $"{table.TableName}.csv");

                    using (StreamWriter writer = new StreamWriter(filePath, false, new UTF8Encoding(true)))
                    {
                        writer.WriteLine(string.Join(";",
                            table.Columns.Cast<DataColumn>()
                                .Select(col => EscapeCsvValue(col.ColumnName))));

                        foreach (DataRow row in table.Rows)
                        {
                            writer.WriteLine(string.Join(";",
                                row.ItemArray.Select(cell => EscapeCsvValue(cell?.ToString() ?? ""))));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogBackupOperation($"Ошибка при сохранении в CSV: {ex.Message}");
            }
        }

        private static void SaveDatabaseToSql(string directoryPath)
        {
            try
            {
                string sqlFilePath = Path.Combine(directoryPath, "backup.sql");

                using (StreamWriter writer = new StreamWriter(sqlFilePath, false, new UTF8Encoding(true)))
                using (MySqlConnection connection = new MySqlConnection(GetConnectionString()))
                {
                    connection.Open();

                    DataTable schema = connection.GetSchema("Tables");
                    List<string> tableNames = new List<string>();

                    foreach (DataRow row in schema.Rows)
                    {
                        string tableName = row["TABLE_NAME"].ToString();
                        tableNames.Add(tableName);

                        writer.WriteLine($"DROP TABLE IF EXISTS `{tableName}`;");

                        using (MySqlCommand cmd = new MySqlCommand($"SHOW CREATE TABLE `{tableName}`", connection))
                        {
                            using (MySqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    writer.WriteLine(reader.GetString(1) + ";");
                                }
                            }
                        }
                        writer.WriteLine();
                    }

                    foreach (string tableName in tableNames)
                    {
                        writer.WriteLine($"-- Данные таблицы: {tableName}");

                        using (MySqlCommand cmd = new MySqlCommand($"SELECT * FROM `{tableName}`", connection))
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                List<string> columnValues = new List<string>();
                                List<string> columnNames = new List<string>();

                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    columnNames.Add($"`{reader.GetName(i)}`");
                                }

                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    if (reader.IsDBNull(i))
                                    {
                                        columnValues.Add("NULL");
                                    }
                                    else
                                    {
                                        string value = reader.GetValue(i).ToString();
                                        value = MySqlHelper.EscapeString(value);
                                        columnValues.Add($"'{value}'");
                                    }
                                }

                                writer.WriteLine($"INSERT INTO `{tableName}` ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", columnValues)});");
                            }
                        }
                        writer.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                LogBackupOperation($"Ошибка при сохранении в SQL: {ex.Message}");
            }
        }

        private static List<DataTable> GetAllDataTables()
        {
            List<DataTable> tables = new List<DataTable>();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(GetConnectionString()))
                {
                    connection.Open();

                    DataTable schema = connection.GetSchema("Tables");
                    foreach (DataRow row in schema.Rows)
                    {
                        string tableName = row["TABLE_NAME"].ToString();

                        string query = $"SELECT * FROM `{tableName}`";
                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);
                            dt.TableName = tableName;
                            tables.Add(dt);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogBackupOperation($"Ошибка при получении данных таблиц: {ex.Message}");
            }

            return tables;
        }

        private static string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            if (value.Contains(";") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                value = value.Replace("\"", "\"\"");
                return $"\"{value}\"";
            }

            return value;
        }

        private static void LogBackupOperation(string message)
        {
            try
            {
                string logPath = Path.Combine(Application.StartupPath, "backup_log.txt");
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n";
                File.AppendAllText(logPath, logEntry, Encoding.UTF8);
            }
            catch
            {
            }
        }
    }
}
