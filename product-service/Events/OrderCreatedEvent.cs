using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace product_service.Events
{
    public class OrderCreatedEvent
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string Action { get; set; } = "Created";
    }
}