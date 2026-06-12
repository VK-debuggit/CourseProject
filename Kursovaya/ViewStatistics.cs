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

        // Параметры фильтрации
        private DateTime _startDate;
        private DateTime _endDate;
        private string _selectedEmployee;
        private List<string> _selectedStatuses;
        private string _searchOrderNumber;

        // Ссылки на диаграммы для динамического изменения размера
        private Chart _chartPopular;
        private Chart _chartUnpopular;
        private Chart _chartEvent;
        private Chart _chartProfit;

        // Отступы (в пикселях)
        private const int MARGIN_TOP = 40;
        private const int MARGIN_BOTTOM = 40;
        private const int MARGIN_LEFT = 40;
        private const int MARGIN_RIGHT = 40;
        private const int GAP_BETWEEN = 30; // Расстояние между диаграммами

        // Базовые размеры для расчета шрифтов
        private const int BASE_WIDTH = 500;
        private const int BASE_HEIGHT = 380;
        private const float BASE_TITLE_FONT_SIZE = 14f;
        private const float BASE_LEGEND_TITLE_FONT_SIZE = 12f;
        private const float BASE_LEGEND_FONT_SIZE = 10f;
        private const float BASE_SERIES_FONT_SIZE = 12f;
        private const float BASE_AXIS_TITLE_FONT_SIZE = 14f;
        private const float BASE_AXIS_LABEL_FONT_SIZE = 14f;
        private const float BASE_PROFIT_TITLE_FONT_SIZE = 16f;

        public ViewStatistics(DateTime startDate, DateTime endDate, string selectedEmployee, List<string> selectedStatuses, string searchOrderNumber)
        {
            InitializeComponent();

            // Развернуть форму на весь экран
            this.WindowState = FormWindowState.Maximized;

            _startDate = startDate;
            _endDate = endDate;
            _selectedEmployee = selectedEmployee;
            _selectedStatuses = selectedStatuses;
            _searchOrderNumber = searchOrderNumber;

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

            // Подписываемся на событие изменения размера
            this.Resize += ViewStatistics_Resize;

            // Загружаем все диаграммы с учетом фильтров
            LoadTopPopularDishes();
            LoadTopUnpopularDishes();
            LoadPopularEvent();
            LoadMonthlyProfit();
        }

        // Метод для вычисления коэффициента масштабирования шрифта
        private float GetFontScaleFactor(Chart chart)
        {
            // Вычисляем коэффициент на основе размера диаграммы относительно базового
            float widthScale = chart.Width / (float)BASE_WIDTH;
            float heightScale = chart.Height / (float)BASE_HEIGHT;
            // Берем минимальный коэффициент, чтобы шрифт не стал слишком большим
            return Math.Min(widthScale, heightScale);
        }

        // Обновление шрифтов на диаграмме
        private void UpdateChartFonts(Chart chart, float baseTitleSize, float baseLegendTitleSize,
                                       float baseLegendSize, float baseSeriesSize,
                                       float baseAxisTitleSize = 0, float baseAxisLabelSize = 0)
        {
            if (chart == null) return;

            float scale = GetFontScaleFactor(chart);
            scale = Math.Max(scale, 0.6f); // Минимум 60% от базового
            scale = Math.Min(scale, 1.5f); // Максимум 150% от базового

            // Обновляем заголовок
            if (chart.Titles.Count > 0)
            {
                chart.Titles[0].Font = new Font("Arial", baseTitleSize * scale, FontStyle.Bold);
            }

            // Обновляем легенду
            if (chart.Legends.Count > 0)
            {
                chart.Legends[0].TitleFont = new Font("Arial", baseLegendTitleSize * scale, FontStyle.Bold);
                chart.Legends[0].Font = new Font("Arial", baseLegendSize * scale);
            }

            // Обновляем серии
            foreach (var series in chart.Series)
            {
                series.Font = new Font("Arial", baseSeriesSize * scale);
            }

            // Обновляем оси (если есть)
            if (chart.ChartAreas.Count > 0)
            {
                if (baseAxisTitleSize > 0 && chart.ChartAreas[0].AxisY.TitleFont != null)
                {
                    chart.ChartAreas[0].AxisY.TitleFont = new Font("Arial", baseAxisTitleSize * scale, FontStyle.Bold);
                }
                if (baseAxisLabelSize > 0 && chart.ChartAreas[0].AxisX.LabelStyle.Font != null)
                {
                    chart.ChartAreas[0].AxisX.LabelStyle.Font = new Font("Arial", baseAxisLabelSize * scale);
                    chart.ChartAreas[0].AxisY.LabelStyle.Font = new Font("Arial", baseAxisLabelSize * scale);
                }
            }
        }

        // Обновление всех шрифтов на всех диаграммах
        private void UpdateAllFonts()
        {
            // Популярные блюда (круговая диаграмма)
            UpdateChartFonts(_chartPopular, BASE_TITLE_FONT_SIZE, BASE_LEGEND_TITLE_FONT_SIZE,
                            BASE_LEGEND_FONT_SIZE, BASE_SERIES_FONT_SIZE);

            // Непопулярные блюда (круговая диаграмма)
            UpdateChartFonts(_chartUnpopular, BASE_TITLE_FONT_SIZE, BASE_LEGEND_TITLE_FONT_SIZE,
                            BASE_LEGEND_FONT_SIZE, BASE_SERIES_FONT_SIZE);

            // Мероприятия (круговая диаграмма)
            UpdateChartFonts(_chartEvent, BASE_TITLE_FONT_SIZE, BASE_LEGEND_TITLE_FONT_SIZE,
                            BASE_LEGEND_FONT_SIZE, BASE_SERIES_FONT_SIZE);

            // Прибыль по месяцам (столбчатая диаграмма) - с осями
            UpdateChartFonts(_chartProfit, BASE_PROFIT_TITLE_FONT_SIZE, BASE_LEGEND_TITLE_FONT_SIZE,
                            BASE_LEGEND_FONT_SIZE, BASE_SERIES_FONT_SIZE,
                            BASE_AXIS_TITLE_FONT_SIZE, BASE_AXIS_LABEL_FONT_SIZE);
        }

        // Обработчик изменения размера окна
        private void ViewStatistics_Resize(object sender, EventArgs e)
        {
            UpdateChartsLayout();
            UpdateAllFonts(); // Обновляем шрифты после изменения размера
        }

        // Метод для обновления расположения и размеров диаграмм
        private void UpdateChartsLayout()
        {
            if (_chartPopular == null || _chartUnpopular == null ||
                _chartEvent == null || _chartProfit == null) return;

            // Вычисляем доступное пространство
            int availableWidth = this.ClientSize.Width - MARGIN_LEFT - MARGIN_RIGHT;
            int availableHeight = this.ClientSize.Height - MARGIN_TOP - MARGIN_BOTTOM;

            // Ширина одной диаграммы (половина доступной ширины минус половина промежутка)
            int chartWidth = (availableWidth - GAP_BETWEEN) / 2;
            // Высота одной диаграммы (половина доступной высоты минус половина промежутка)
            int chartHeight = (availableHeight - GAP_BETWEEN) / 2;

            // Минимальные размеры (чтобы диаграммы не сжимались слишком сильно)
            int minWidth = 350;
            int minHeight = 280;
            chartWidth = Math.Max(chartWidth, minWidth);
            chartHeight = Math.Max(chartHeight, minHeight);

            // ========== ВЕРХНИЕ ДИАГРАММЫ ==========
            // Левая верхняя (популярные блюда)
            _chartPopular.Size = new Size(chartWidth, chartHeight);
            _chartPopular.Location = new Point(MARGIN_LEFT, MARGIN_TOP);

            // Правая верхняя (непопулярные блюда)
            _chartUnpopular.Size = new Size(chartWidth, chartHeight);
            _chartUnpopular.Location = new Point(MARGIN_LEFT + chartWidth + GAP_BETWEEN, MARGIN_TOP);

            // ========== НИЖНИЕ ДИАГРАММЫ ==========
            int bottomY = MARGIN_TOP + chartHeight + GAP_BETWEEN;

            // Левая нижняя (мероприятия)
            _chartEvent.Size = new Size(chartWidth, chartHeight);
            _chartEvent.Location = new Point(MARGIN_LEFT, bottomY);

            // Правая нижняя (прибыль по месяцам)
            _chartProfit.Size = new Size(chartWidth, chartHeight);
            _chartProfit.Location = new Point(MARGIN_LEFT + chartWidth + GAP_BETWEEN, bottomY);
        }

        // Вспомогательный метод для построения WHERE условий
        private string BuildFilterConditions()
        {
            List<string> conditions = new List<string>();

            // Фильтр по датам
            string startDateStr = _startDate.ToString("yyyy-MM-dd");
            string endDateStr = _endDate.ToString("yyyy-MM-dd");
            conditions.Add($"(o.DateEvent >= '{startDateStr}' AND o.DateEvent <= '{endDateStr}')");

            // Фильтр по сотруднику
            if (!string.IsNullOrEmpty(_selectedEmployee) && _selectedEmployee != "Все сотрудники")
            {
                conditions.Add($"w.FullName = '{_selectedEmployee.Replace("'", "''")}'");
            }

            // Фильтр по статусам
            if (_selectedStatuses != null && _selectedStatuses.Count > 0)
            {
                List<string> statusConditions = new List<string>();
                foreach (string status in _selectedStatuses)
                {
                    statusConditions.Add($"s.Status = '{status.Replace("'", "''")}'");
                }
                conditions.Add("(" + string.Join(" OR ", statusConditions) + ")");
            }

            // Фильтр по номеру заказа
            if (!string.IsNullOrEmpty(_searchOrderNumber))
            {
                conditions.Add($"o.NumberOrder LIKE '{_searchOrderNumber}%'");
            }

            if (conditions.Count > 0)
            {
                return " AND " + string.Join(" AND ", conditions);
            }

            return "";
        }

        // ========== ДИАГРАММА 1: ТОП-5 САМЫХ ПОПУЛЯРНЫХ БЛЮД ==========
        private void LoadTopPopularDishes()
        {
            try
            {
                string filterConditions = BuildFilterConditions();

                string query = $@"
            SELECT 
                d.Name,
                SUM(od.Count) as TotalQuantity
            FROM CafeActivities.OrderComposition od
            JOIN CafeActivities.Orders o ON od.IdOrder = o.NumberOrder
            JOIN CafeActivities.Dishes d ON od.IdDish = d.Article
            LEFT JOIN CafeActivities.Users w ON o.IdUser = w.IDuser
            LEFT JOIN CafeActivities.Status s ON o.IdStatus = s.IDstatus
            WHERE 1=1 {filterConditions}
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

                _chartPopular = new Chart();
                _chartPopular.ChartAreas.Add(new ChartArea());

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

                _chartPopular.Series.Add(series);
                _chartPopular.Titles.Clear();
                _chartPopular.Titles.Add("Топ-5 самых популярных блюд");

                _chartPopular.Legends.Clear();
                Legend legendPopular = new Legend("Legend1");
                legendPopular.Docking = Docking.Right;
                legendPopular.Alignment = StringAlignment.Center;
                legendPopular.Title = "Блюда";
                legendPopular.IsTextAutoFit = false;
                legendPopular.TextWrapThreshold = 25;

                _chartPopular.Legends.Add(legendPopular);

                this.Controls.Add(_chartPopular);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке популярных блюд: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ========== ДИАГРАММА 2: ТОП-5 САМЫХ НЕПОПУЛЯРНЫХ БЛЮД ==========
        private void LoadTopUnpopularDishes()
        {
            try
            {
                string filterConditions = BuildFilterConditions();

                string query = $@"
            SELECT 
                d.Name,
                SUM(od.Count) as TotalQuantity
            FROM CafeActivities.OrderComposition od
            JOIN CafeActivities.Orders o ON od.IdOrder = o.NumberOrder
            JOIN CafeActivities.Dishes d ON od.IdDish = d.Article
            LEFT JOIN CafeActivities.Users w ON o.IdUser = w.IDuser
            LEFT JOIN CafeActivities.Status s ON o.IdStatus = s.IDstatus
            WHERE 1=1 {filterConditions}
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

                _chartUnpopular = new Chart();
                _chartUnpopular.ChartAreas.Add(new ChartArea());

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

                _chartUnpopular.Series.Add(series);
                _chartUnpopular.Titles.Clear();
                _chartUnpopular.Titles.Add("Топ-5 самых непопулярных блюд");

                _chartUnpopular.Legends.Clear();
                Legend legendUnpopular = new Legend("Legend1");
                legendUnpopular.Docking = Docking.Right;
                legendUnpopular.Alignment = StringAlignment.Center;
                legendUnpopular.Title = "Блюда";
                legendUnpopular.IsTextAutoFit = false;
                legendUnpopular.TextWrapThreshold = 25;

                _chartUnpopular.Legends.Add(legendUnpopular);

                this.Controls.Add(_chartUnpopular);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке непопулярных блюд: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ========== ДИАГРАММА 3: РАСПРЕДЕЛЕНИЕ ЗАКАЗОВ ПО МЕРОПРИЯТИЯМ ==========
        private void LoadPopularEvent()
        {
            try
            {
                string filterConditions = BuildFilterConditions();

                string query = $@"
            SELECT 
                e.Event,
                COUNT(o.NumberOrder) as OrdersCount
            FROM CafeActivities.Orders o
            JOIN CafeActivities.Events e ON o.IdEvent = e.IDevent
            LEFT JOIN CafeActivities.Users w ON o.IdUser = w.IDuser
            LEFT JOIN CafeActivities.Status s ON o.IdStatus = s.IDstatus
            WHERE 1=1 {filterConditions}
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

                _chartEvent = new Chart();
                _chartEvent.ChartAreas.Add(new ChartArea());

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

                _chartEvent.Series.Add(series);
                _chartEvent.Titles.Clear();
                _chartEvent.Titles.Add("Распределение заказов по мероприятиям");

                _chartEvent.Legends.Clear();
                Legend legendEvent = new Legend("Legend1");
                legendEvent.Docking = Docking.Right;
                legendEvent.Alignment = StringAlignment.Center;
                legendEvent.Title = "Мероприятия";
                legendEvent.IsTextAutoFit = false;
                legendEvent.TextWrapThreshold = 20;

                legendEvent.CellColumns.Clear();
                legendEvent.TableStyle = LegendTableStyle.Wide;

                _chartEvent.Legends.Add(legendEvent);

                this.Controls.Add(_chartEvent);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке статистики мероприятий: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ========== ДИАГРАММА 4: ПРИБЫЛЬ ПО МЕСЯЦАМ ==========
        private void LoadMonthlyProfit()
        {
            try
            {
                string filterConditions = BuildFilterConditions();

                string query = $@"
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
            LEFT JOIN CafeActivities.Users w ON o.IdUser = w.IDuser
            WHERE o.DateOfConclusion >= DATE_SUB(NOW(), INTERVAL 12 MONTH) {filterConditions}
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

                _chartProfit = new Chart();
                _chartProfit.ChartAreas.Add(new ChartArea());

                Series series = new Series("Прибыль");
                series.ChartType = SeriesChartType.Column;
                series.Label = "#VAL";
                series.ToolTip = "#VALX: #VAL";
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

                _chartProfit.ChartAreas[0].AxisY.Title = "Прибыль (руб.)";
                _chartProfit.ChartAreas[0].AxisX.LabelStyle.Angle = 0;
                _chartProfit.ChartAreas[0].AxisX.Interval = 1;
                _chartProfit.ChartAreas[0].AxisX.LabelStyle.IsStaggered = true;

                _chartProfit.Series.Add(series);
                _chartProfit.Titles.Clear();
                _chartProfit.Titles.Add("Прибыль по месяцам");

                _chartProfit.Legends.Clear();
                Legend legendProfit = new Legend("Legend1");
                legendProfit.Docking = Docking.Top;
                legendProfit.Alignment = StringAlignment.Center;
                _chartProfit.Legends.Add(legendProfit);

                this.Controls.Add(_chartProfit);
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