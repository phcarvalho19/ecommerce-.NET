using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace product_service.Events
{
    public class OrderRejectedEvent
    {
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int QuantityRequested { get; set; }
        public int StockAvailable { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
