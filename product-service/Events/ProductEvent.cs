using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace product_service.Events
{
    public class ProductEvent
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }
}