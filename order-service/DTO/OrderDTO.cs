using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace order_service.DTO
{
    public class OrderDTO
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}