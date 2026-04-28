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
        public decimal TotalAmount { get; set; }
        public decimal Prepayment { get; set; }

        // Добавленные свойства для работы с заказами
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public string Status { get; set; }
        public string NameUser { get; set; }
    }
}