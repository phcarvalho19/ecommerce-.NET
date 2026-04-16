using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using order_service.Data;
using order_service.DTO;
using order_service.Kafka;
using order_service.Model;

namespace order_service.Service
{
    public class OrderService
    {
        private readonly AppDbContext _context;

        private readonly OrderProducerService _producer;


        public OrderService(AppDbContext context, OrderProducerService producer)
        {
            _context = context;
            _producer = producer;
        }

     public async Task<Order> CreateOrder(OrderDTO dto, int userId)
{
    if (dto.Quantity <= 0)
        throw new Exception("Quantidade inválida");

    var product = await _context.AvailableProducts
        .FirstOrDefaultAsync(p => p.Id == dto.ProductId);

    if (product == null)
        throw new Exception("Produto não encontrado");

    if (dto.Quantity > product.Stock)
        throw new Exception("Quantidade indisponível em estoque");

    var order = new Order
    {
        UserId = userId,
        ProductName = product.Name!,
        Price = product.Price,
        Quantity = dto.Quantity,
        CreatedAt = DateTime.UtcNow
    };

    _context.Orders.Add(order);

    await _context.SaveChangesAsync();

    // 🔥 PUBLICA EVENTO PARA O PRODUCT SERVICE
    await _producer.PublishOrderEvent(new OrderCreatedEvent
    {
        ProductId = product.Id,
        Quantity = dto.Quantity,
        Action = "Created"
    });

    return order;
}

        public async Task<List<Order>> GetOrdersByUser(int userId)
        {
            return await _context.Orders
                .Where(x => x.UserId == userId)
                .ToListAsync();
        }

        public async Task<List<Order>> GetAllOrders()
        {
            return await _context.Orders.ToListAsync();
        }

        public async Task<Order?> GetById(int id)
        {
            return await _context.Orders.FindAsync(id);
        }

        

        public async Task Update(Order order)
        {
            
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            
            
        }


    

    public async Task<AvailableProduct?> GetProductByName(string name)
{
    return await _context.AvailableProducts
        .FirstOrDefaultAsync(p => p.Name == name);
}

        public async Task Delete(Order order)
        {
            var product = await _context.AvailableProducts
                .FirstOrDefaultAsync(p => p.Name == order.ProductName);

            if (product == null)
                throw new Exception("Produto do pedido não encontrado para estorno de estoque");

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            await _producer.PublishOrderEvent(new OrderCreatedEvent
            {
                ProductId = product.Id,
                Quantity = order.Quantity,
                Action = "Cancelled"
            });
        }
    }
}