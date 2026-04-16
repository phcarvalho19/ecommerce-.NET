using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using order_service.DTO;
using order_service.Model;
using order_service.Service;

namespace order_service.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly OrderService _service;

        public OrderController(OrderService service)
        {
            _service = service;
        }

    private int GetUserId()
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
              ?? User.FindFirst("sub")?.Value;

    if (string.IsNullOrEmpty(userId))
        throw new UnauthorizedAccessException("User not authenticated");

    return int.Parse(userId);
}

        [HttpPost]
        [Authorize(Policy = "User")]
        public async Task<IActionResult> Create(OrderDTO dto)
        {
            if (dto.ProductId <= 0)
                throw new Exception("Produto inválido");

            try
            {
                var userId = GetUserId();
                var result = await _service.CreateOrder(dto, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

        }

        [HttpGet]
    public async Task<IActionResult> GetMyOrders()
    {
        var userId = GetUserId();

        List<Order> orders;

        if (User.IsInRole("Admin"))
            orders = await _service.GetAllOrders();
        else
            orders = await _service.GetOrdersByUser(userId);

        if (!orders.Any())
            return NotFound(new { message = "Nenhum pedido feito" });

        var result = orders.Select(o => new OrderResponseDTO
        {
            Id = o.Id,
            ProductName = o.ProductName,
            Price = o.Price,
            Quantity = o.Quantity
        });

        return Ok(result);
    }

     [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetUserId();

        var order = await _service.GetById(id);

        if (order == null)
            return NotFound(new { message = "Pedido não encontrado" });

        var isAdmin = User.IsInRole("Admin");

        if (!isAdmin && order.UserId != userId)
            return Forbid();

        var result = new OrderResponseDTO
        {
            Id = order.Id,
            ProductName = order.ProductName,
            Price = order.Price,
            Quantity = order.Quantity
        };

        return Ok(result);
    }

       [HttpPut("{id}")]
[Authorize(Policy = "User")]
public async Task<IActionResult> Update(int id, UpdateOrderDTO dto)
{
    try
    {
        var userId = GetUserId();

        var order = await _service.GetById(id);

        if (order == null)
            return NotFound(new { message = "Pedido não encontrado" });

        var isAdmin = User.IsInRole("Admin");

        // 🔐 segurança
        if (!isAdmin && order.UserId != userId)
            return Forbid();

        if (dto.Quantity <= 0)
            return BadRequest(new { message = "Quantidade inválida" });

        // 🔥 valida estoque
        var product = await _service.GetProductByName(order.ProductName);

        if (product == null)
            return BadRequest(new { message = "Produto não encontrado" });

        if (dto.Quantity > product.Stock)
            return BadRequest(new { message = "Quantidade indisponível em estoque" });

        order.Quantity = dto.Quantity;

        await _service.Update(order);

        return NoContent();
    }
    catch (Exception ex)
    {
        return BadRequest(new { message = ex.Message });
    }
}

     [HttpDelete("{id}")]
    [Authorize(Policy = "User")]
    public async Task<IActionResult> Delete(int id)
{
    var userId = GetUserId();

    var order = await _service.GetById(id);

    if (order == null)
        return NotFound(new { message = "Pedido não encontrado" });

    var isAdmin = User.IsInRole("Admin");

    if (!isAdmin && order.UserId != userId)
        return Forbid();

    await _service.Delete(order);

    return NoContent();
}
    }
}