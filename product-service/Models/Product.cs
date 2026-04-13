using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace product_service.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        
         [Column(TypeName = "decimal(10,2)")]
          public decimal Price { get; set; }

        public int Stock { get; set; }
    }
}