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
using System.Windows.Forms.DataVisualization.Charting;

namespace Kursovaya
{
    public partial class ViewStatistics : Form
    {
        string conString = $"host={Properties.Settings.Default.host};uid={Properties.Settings.Default.uid};pwd={Properties.Settings.Default.pwd};database={Properties.Settings.Default.database};";

        public ViewStatistics()
        {
            InitializeComponent();

            button3.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);

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

            // Загружаем все диаграммы
            LoadTopPopularDishes();
            LoadTopUnpopularDishes();
            LoadPopularEvent();
            LoadMonthlyProfit();
        }

        // ========== ДИАГРАММА 1: ТОП-5 САМЫХ ПОПУЛЯРНЫХ БЛЮД (КРУГОВАЯ) ==========
        private void LoadTopPopularDishes()
        {
            try
            {
                string query = @"
                    SELECT 
                        d.Name,
                        SUM(od.Count) as TotalQuantity
                    FROM CafeActivities.OrderComposition od
                    JOIN CafeActivities.Dishes d ON od.IdDish = d.Article
                    GROUP BY d.Article, d.Name
                    ORDER BY TotalQuantity DESC
                    LIMIT 5";

                DataTable data = new DataTable();
                using (MySqlConnection con = new MySqlConnection(conString))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                    {
                        adapter.Fill(data);
                    }
                }

                // Настройка диаграммы
                Chart chartPopular = new Chart();
                chartPopular.Size = new Size(450, 380);
                chartPopular.Location = new Point(30, 40);
                chartPopular.ChartAreas.Add(new ChartArea());

                Series series = new Series("Популярные блюда");
                series.ChartType = SeriesChartType.Pie;
                series.Label = "#PERCENT{P0}";
                series.LabelToolTip = "#VALX: #PERCENT{P0} (#VAL шт.)";
                series.ToolTip = "#VALX: #PERCENT{P0} (#VAL шт.)";
                series.LegendText = "#VALX";

                if (data.Rows.Count > 0)
                {
                    foreach (DataRow row in data.Rows)
                    {
                        string name = row["Name"].ToString();
                        int quantity = Convert.ToInt32(row["TotalQuantity"]);
                        series.Points.AddXY(name, quantity);
                    }
                }
                else
                {
                    series.Points.AddXY("Нет данных", 1);
                }

                chartPopular.Series.Add(series);
                chartPopular.Titles.Clear();
                chartPopular.Titles.Add("Топ-5 самых популярных блюд");
                chartPopular.Titles[0].Font = new Font("Arial", 12, FontStyle.Bold);

                // Настройка легенды
                chartPopular.Legends.Clear();
                Legend legendPopular = new Legend("Legend1");
                legendPopular.Docking = Docking.Right;
                legendPopular.Alignment = StringAlignment.Center;
                legendPopular.Title = "Блюда";
                legendPopular.TitleFont = new Font("Arial", 10, FontStyle.Bold);
                legendPopular.Font = new Font("Arial", 9);
                chartPopular.Legends.Add(legendPopular);

                // Добавляем на форму
                this.Controls.Add(chartPopular);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке популярных блюд: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ========== ДИАГРАММА 2: ТОП-5 САМЫХ НЕПОПУЛЯРНЫХ БЛЮД (КРУГОВАЯ) ==========
        private void LoadTopUnpopularDishes()
        {
            try
            {
                string query = @"
                    SELECT 
                        d.Name,
                        SUM(od.Count) as TotalQuantity
                    FROM CafeActivities.OrderComposition od
                    JOIN CafeActivities.Dishes d ON od.IdDish = d.Article
                    GROUP BY d.Article, d.Name
                    ORDER BY TotalQuantity ASC
                    LIMIT 5";

                DataTable data = new DataTable();
                using (MySqlConnection con = new MySqlConnection(conString))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                    {
                        adapter.Fill(data);
                    }
                }

                // Настройка диаграммы
                Chart chartUnpopular = new Chart();
                chartUnpopular.Size = new Size(500, 380);
                chartUnpopular.Location = new Point(500, 40);
                chartUnpopular.ChartAreas.Add(new ChartArea());

                Series series = new Series("Непопулярные блюда");
                series.ChartType = SeriesChartType.Pie;
                series.Label = "#PERCENT{P0}";
                series.LabelToolTip = "#VALX: #PERCENT{P0} (#VAL шт.)";
                series.ToolTip = "#VALX: #PERCENT{P0} (#VAL шт.)";
                series.LegendText = "#VALX";

                if (data.Rows.Count > 0)
                {
                    foreach (DataRow row in data.Rows)
                    {
                        string name = row["Name"].ToString();
                        int quantity = Convert.ToInt32(row["TotalQuantity"]);
                        series.Points.AddXY(name, quantity);
                    }
                }
                else
                {
                    series.Points.AddXY("Нет данных", 1);
                }

                chartUnpopular.Series.Add(series);
                chartUnpopular.Titles.Clear();
                chartUnpopular.Titles.Add("Топ-5 самых непопулярных блюд");
                chartUnpopular.Titles[0].Font = new Font("Arial", 12, FontStyle.Bold);

                // Настройка легенды
                chartUnpopular.Legends.Clear();
                Legend legendUnpopular = new Legend("Legend1");
                legendUnpopular.Docking = Docking.Right;
                legendUnpopular.Alignment = StringAlignment.Center;
                legendUnpopular.Title = "Блюда";
                legendUnpopular.TitleFont = new Font("Arial", 10, FontStyle.Bold);
                legendUnpopular.Font = new Font("Arial", 9);
                chartUnpopular.Legends.Add(legendUnpopular);

                // Добавляем на форму
                this.Controls.Add(chartUnpopular);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке непопулярных блюд: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ========== ДИАГРАММА 3: РАСПРЕДЕЛЕНИЕ ЗАКАЗОВ ПО МЕРОПРИЯТИЯМ (КРУГОВАЯ) ==========
        private void LoadPopularEvent()
        {
            try
            {
                string query = @"
                    SELECT 
                        e.Event,
                        COUNT(o.NumberOrder) as OrdersCount
                    FROM CafeActivities.Orders o
                    JOIN CafeActivities.Events e ON o.IdEvent = e.IDevent
                    GROUP BY e.IDevent, e.Event
                    ORDER BY OrdersCount DESC";

                DataTable data = new DataTable();
                using (MySqlConnection con = new MySqlConnection(conString))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                    {
                        adapter.Fill(data);
                    }
                }

                // Настройка диаграммы
                Chart chartEvent = new Chart();
                chartEvent.Size = new Size(450, 380);
                chartEvent.Location = new Point(30, 420);
                chartEvent.ChartAreas.Add(new ChartArea());

                Series series = new Series("Мероприятия");
                series.ChartType = SeriesChartType.Pie;
                series.Label = "#PERCENT{P0}";
                series.LabelToolTip = "#VALX: #PERCENT{P0} (#VAL заказов)";
                series.ToolTip = "#VALX: #PERCENT{P0} (#VAL заказов)";
                series.LegendText = "#VALX";

                if (data.Rows.Count > 0)
                {
                    foreach (DataRow row in data.Rows)
                    {
                        string eventName = row["Event"].ToString();
                        int count = Convert.ToInt32(row["OrdersCount"]);
                        series.Points.AddXY(eventName, count);
                    }
                }
                else
                {
                    series.Points.AddXY("Нет данных", 1);
                }

                chartEvent.Series.Add(series);
                chartEvent.Titles.Clear();
                chartEvent.Titles.Add("Распределение заказов по мероприятиям");
                chartEvent.Titles[0].Font = new Font("Arial", 12, FontStyle.Bold);

                // Настройка легенды
                chartEvent.Legends.Clear();
                Legend legendEvent = new Legend("Legend1");
                legendEvent.Docking = Docking.Right;
                legendEvent.Alignment = StringAlignment.Center;
                legendEvent.Title = "Мероприятия";
                legendEvent.TitleFont = new Font("Arial", 10, FontStyle.Bold);
                legendEvent.Font = new Font("Arial", 9);
                chartEvent.Legends.Add(legendEvent);

                // Добавляем на форму
                this.Controls.Add(chartEvent);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке статистики мероприятий: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ========== ДИАГРАММА 4: ПРИБЫЛЬ ПО МЕСЯЦАМ (СТОЛБЧАТАЯ) ==========
        private void LoadMonthlyProfit()
        {
            try
            {
                string query = @"
                SET lc_time_names = 'ru_RU';
            
                SELECT 
                    DATE_FORMAT(o.DateOfConclusion, '%Y-%m') as Month,
                    DATE_FORMAT(o.DateOfConclusion, '%M %Y') as MonthName,
                    SUM(
                        CASE 
                            WHEN s.Status = 'Принят' THEN o.Prepayment
                            WHEN s.Status = 'Оплачен' THEN o.PriceAll
                            ELSE 0
                        END
                    ) as TotalProfit
                FROM CafeActivities.Orders o
                JOIN CafeActivities.Status s ON o.IdStatus = s.IDstatus
                WHERE o.DateOfConclusion >= DATE_SUB(NOW(), INTERVAL 12 MONTH)
                GROUP BY DATE_FORMAT(o.DateOfConclusion, '%Y-%m'), DATE_FORMAT(o.DateOfConclusion, '%M %Y')
                ORDER BY DATE_FORMAT(o.DateOfConclusion, '%Y-%m') ASC";

                DataTable data = new DataTable();
                using (MySqlConnection con = new MySqlConnection(conString))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                    {
                        adapter.Fill(data);
                    }
                }

                // Настройка диаграммы
                Chart chartProfit = new Chart();
                chartProfit.Size = new Size(500, 380);
                chartProfit.Location = new Point(500, 420);
                chartProfit.ChartAreas.Add(new ChartArea());

                Series series = new Series("Прибыль");
                series.ChartType = SeriesChartType.Column;
                series.Label = "#VAL";  // Только число
                series.ToolTip = "#VALX: #VAL";  // В подсказке показываем с рублями
                series.IsValueShownAsLabel = true;

                if (data.Rows.Count > 0)
                {
                    foreach (DataRow row in data.Rows)
                    {
                        string monthName = row["MonthName"].ToString();
                        decimal profit = Convert.ToDecimal(row["TotalProfit"]);
                        series.Points.AddXY(monthName, profit);
                    }
                }
                else
                {
                    series.Points.AddXY("Нет данных", 0);
                }

                // Настройка осей
                chartProfit.ChartAreas[0].AxisY.Title = "Прибыль (руб.)";
                chartProfit.ChartAreas[0].AxisY.TitleFont = new Font("Arial", 10, FontStyle.Bold);
                chartProfit.ChartAreas[0].AxisX.LabelStyle.Angle = 0;
                chartProfit.ChartAreas[0].AxisX.Interval = 1;
                chartProfit.ChartAreas[0].AxisX.LabelStyle.IsStaggered = true;

                chartProfit.Series.Add(series);
                chartProfit.Titles.Clear();
                chartProfit.Titles.Add("Прибыль по месяцам (за последние 12 месяцев)");
                chartProfit.Titles[0].Font = new Font("Arial", 12, FontStyle.Bold);

                // Добавляем легенду для столбчатой диаграммы
                chartProfit.Legends.Clear();
                Legend legendProfit = new Legend("Legend1");
                legendProfit.Docking = Docking.Top;
                legendProfit.Alignment = StringAlignment.Center;
                legendProfit.Font = new Font("Arial", 9);
                chartProfit.Legends.Add(legendProfit);

                // Добавляем на форму
                this.Controls.Add(chartProfit);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке прибыли по месяцам: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ========== КНОПКИ НАВИГАЦИИ ==========

        private bool allowClose = false;

        private void button3_Click(object sender, EventArgs e)
        {
            allowClose = true;
            this.Visible = false;
            ViewingOrderAccounting viewingOrderAccounting = new ViewingOrderAccounting();
            viewingOrderAccounting.ShowDialog();
            this.Close();
        }

        private void ViewStatistics_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.ApplicationExitCall)
                return;

            if (!allowClose)
                e.Cancel = true;
        }
    }
}