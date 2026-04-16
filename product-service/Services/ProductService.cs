using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using product_service.Models;
using product_service.Repositories;

namespace product_service.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repository;

        public ProductService(IProductRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<Product>> GetProducts()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<Product> GetProduct(int id)
        {
            var product = await _repository.GetByIdAsync(id);

            if (product is null)
                throw new Exception("Product not found");

            return product;
        }

        public async Task<Product> CreateProduct(Product product)
        {
            return await _repository.CreateAsync(product);
        }

        public async Task UpdateProduct(Product product)
        {
            await _repository.UpdateAsync(product);
        }

        public async Task DeleteProduct(int id)
        {
            var product = await _repository.GetByIdAsync(id);

            if (product is null)
                throw new Exception("Product not found");

            await _repository.DeleteAsync(product);
        }

        public async Task<bool> ReduceStockAsync(int productId, int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantidade deve ser maior que zero", nameof(quantity));

            var product = await _repository.GetByIdAsync(productId);

            if (product is null)
                throw new Exception("Produto não encontrado");

            if (product.Stock < quantity)
                return false; // Estoque insuficiente

            product.Stock -= quantity;

            // Garantir que nunca fique negativo
            if (product.Stock < 0)
                product.Stock = 0;

            await _repository.UpdateAsync(product);
            return true;
        }

        public async Task<bool> IncreaseStockAsync(int productId, int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantidade deve ser maior que zero", nameof(quantity));

            var product = await _repository.GetByIdAsync(productId);

            if (product is null)
                throw new Exception("Produto não encontrado");

            product.Stock += quantity;
            await _repository.UpdateAsync(product);
            return true;
        }
    }
}