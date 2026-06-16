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
    public partial class Roles : Form
    {
        string conString = $"host={Properties.Settings.Default.host};uid={Properties.Settings.Default.uid};pwd={Properties.Settings.Default.pwd};database={Properties.Settings.Default.database};";
        private int rowCount = 0;

        public Roles()
        {
            InitializeComponent();

            FillDataGridView();

            button4.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            dataGridView1.BackgroundColor = System.Drawing.Color.FromArgb(255, 221, 153);
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dataGridView1.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(217, 152, 22);
            string fullname = Properties.Settings.Default.userName;
            string formattedname = fullname;

            string[] parts = fullname.Split(' ');

            label5.Text = rowCount.ToString();

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

        private bool allowClose = false;

        //Обработчик закрытия формы
        private void Roles_FormClosing(object sender, FormClosingEventArgs e)
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

        //Обработчик кнопки возврата в справочники
        private void button4_Click(object sender, EventArgs e)
        {
            allowClose = true;
            this.Visible = false;
            Directories directories = new Directories();
            directories.ShowDialog();
            this.Close();
        }

        //Загрузка данных ролей в DataGridView
        void FillDataGridView()
        {
            string SelectQuery = @"SELECT IDrole, `Role` FROM CafeActivities.Roles ORDER BY `Role` ASC;";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                con.Open();

                using (MySqlCommand cmd = new MySqlCommand(SelectQuery, con))
                using (MySqlDataReader rdr = cmd.ExecuteReader())
                {
                    dataGridView1.Rows.Clear();
                    dataGridView1.Columns.Clear();

                    dataGridView1.Columns.Add("IDrole", "Id");
                    dataGridView1.Columns["IDrole"].Visible = false;
                    dataGridView1.Columns.Add("Role", "Роль");

                    rowCount = 0;

                    while (rdr.Read())
                    {
                        int rowIndex = dataGridView1.Rows.Add(
                            rdr[0].ToString(),
                            rdr[1].ToString()
                        );

                        rowCount++;
                    }

                    label5.Text = rowCount.ToString();

                    if (rowCount == 0)
                    {
                        MessageBox.Show("Данные не найдены", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        //Проверка использования роли в других таблицах
        private bool IsRoleInUse(int statusId)
        {
            string checkQueries = @"SELECT COUNT(*) FROM Users WHERE IdRole = @roleId;";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();

                    using (MySqlCommand cmd = new MySqlCommand(checkQueries, con))
                    {
                        cmd.Parameters.AddWithValue("@roleId", statusId);
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
                    MessageBox.Show($"Ошибка проверки использования роли: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return true;
                }
            }
        }

        //Обработчик изменения выделения в DataGridView
        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow != null && dataGridView1.CurrentRow.Index >= 0)
            {
                try
                {
                    DataGridViewRow selectedRow = dataGridView1.CurrentRow;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при заполнении полей: {ex.Message}");
                }
            }
        }

        //Обработчик кнопки удаления роли
        private void button3_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null)
            {
                MessageBox.Show("Выберите роль для удаления", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int selectedId = Convert.ToInt32(dataGridView1.CurrentRow.Cells["IDrole"].Value);
            string statusName = dataGridView1.CurrentRow.Cells["Role"].Value.ToString();

            DialogResult result = MessageBox.Show(
                $"Вы уверены, что хотите удалить роль \"{statusName}\"?",
                "Подтверждение удаления",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            if (IsRoleInUse(selectedId))
            {
                MessageBox.Show("Невозможно удалить роль, так как она используется в других таблицах",
                              "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string query = "DELETE FROM Roles WHERE IDrole = @roleId";

            using (MySqlConnection con = new MySqlConnection(conString))
            {
                try
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@roleId", selectedId);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Роль успешно удалена", "Успех",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);
                            FillDataGridView();
                            ClearAllFields();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления роли: {ex.Message}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        //Очистка всех полей формы
        private void ClearAllFields()
        {
            dataGridView1.ClearSelection();
            dataGridView1.CurrentCell = null;
        }

        //Обработчик загрузки формы
        private void Roles_Load(object sender, EventArgs e)
        {
            ClearAllFields();
        }
    }
}