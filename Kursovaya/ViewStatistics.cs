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
        public ViewStatistics()
        {
            InitializeComponent();

            button3.BackColor = System.Drawing.Color.FromArgb(217, 152, 22);

            // Настройка области графика
            chart1.ChartAreas.Clear();
            ChartArea area = new ChartArea("MainArea");
            chart1.ChartAreas.Add(area);

            // Добавление серии (линейный график)
            chart1.Series.Clear();
            Series series = new Series("Sales");
            series.ChartType = SeriesChartType.Pie;
            series.Points.AddXY("Янв", 120);
            series.Points.AddXY("Фев", 135);
            series.Points.AddXY("Мар", 150);
            series.Points.AddXY("Апр", 170);
            chart1.Series.Add(series);

            // Заголовок
            chart1.Titles.Add("Продажи по месяцам");
        }

        // ========== КНОПКИ НАВИГАЦИИ ==========

        private bool allowClose = false;

        private void button3_Click(object sender, EventArgs e)
        {
            //inactivityTimer.Stop();
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
