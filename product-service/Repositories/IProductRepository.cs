using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using product_service.Models;

namespace product_service.Repositories
{
    public interface IProductRepository
    {
     Task<List<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(int id);
    Task<Product> CreateAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(Product product);
    }
}