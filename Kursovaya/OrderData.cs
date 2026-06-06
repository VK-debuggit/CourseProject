using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kursovaya
{
    public class OrderData
    {
        public string NumberOrder { get; set; }
        public string NumberPhone { get; set; }
        public string NameClient { get; set; }
        public string DateOrder { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string Category { get; set; }
        public string Event { get; set; }
        public string Weight { get; set; }
        public string Dec { get; set; }
        public Image Photo { get; set; }
        public int TotalAmount { get; set; }
        public int Prepayment { get; set; }

        // Добавленные свойства для работы с заказами
        public int DiscountAmount { get; set; }
        public int FinalAmount { get; set; }
        public string Status { get; set; }
        public string NameUser { get; set; }
    }
}