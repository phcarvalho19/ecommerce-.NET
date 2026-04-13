using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using product_service.Models;

namespace product_service.Services
{
    public interface IProductService
    {
    Task<List<Product>> GetProducts();
    Task<Product> GetProduct(int id);
    Task<Product> CreateProduct(Product product);
    Task UpdateProduct(Product product);
    Task DeleteProduct(int id);
    }
}