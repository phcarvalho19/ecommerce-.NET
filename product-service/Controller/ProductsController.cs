using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using product_service.Data;
using product_service.DTOs;
using product_service.Events;
using product_service.Models;
using product_service.Services;

namespace product_service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductsController(AppDbContext context)
    {
        _context = context;
    }
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll()
    {
        return await _context.Products.ToListAsync();
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetById(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
            return NotFound(new { message = "Produto não encontrado" });

        return product;
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id}/add-stock")]
    public async Task<IActionResult> AddStock(
        int id,
        AddStockDTO dto,
        [FromServices] KafkaProducerService kafkaProducer)
    {
        if (dto.Quantity <= 0)
            return BadRequest(new { message = "Quantidade inválida" });

        var product = await _context.Products.FindAsync(id);

        if (product == null)
            return NotFound(new { message = "Produto não encontrado" });

        // ✅ soma estoque
        product.Stock += dto.Quantity;

        await _context.SaveChangesAsync();

        // ✅ publica atualização no Kafka
        await kafkaProducer.PublishProductCreated(new ProductEvent
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            Stock = product.Stock
        });

        return Ok(new { message = $"Estoque adicionado com sucesso. Novo estoque: {product.Stock}", product });
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id}/reduce-stock")]
    public async Task<IActionResult> ReduceStock(
        int id,
        AddStockDTO dto,
        [FromServices] KafkaProducerService kafkaProducer)
    {
        if (dto.Quantity <= 0)
            return BadRequest(new { message = "Quantidade inválida. Deve ser maior que zero" });

        var product = await _context.Products.FindAsync(id);

        if (product == null)
            return NotFound(new { message = "Produto não encontrado" });

        if (product.Stock < dto.Quantity)
            return BadRequest(new
            {
                message = $"Estoque insuficiente. Disponível: {product.Stock}, Solicitado: {dto.Quantity}"
            });

        // ✅ reduz estoque com segurança
        product.Stock -= dto.Quantity;

        await _context.SaveChangesAsync();

        // ✅ publica atualização no Kafka
        await kafkaProducer.PublishProductCreated(new ProductEvent
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            Stock = product.Stock
        });

        return Ok(new { message = $"Estoque reduzido com sucesso. Novo estoque: {product.Stock}", product });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(
        Product product,
        [FromServices] KafkaProducerService kafkaProducer)
    {
        var existingProduct = await _context.Products.FirstOrDefaultAsync(p => p.Id == product.Id);

        if (existingProduct != null)
            return BadRequest(new { message = "Produto ja existe" });

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Publica evento no Kafka
        await kafkaProducer.PublishProductCreated(new ProductEvent
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            Stock = product.Stock
        });

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }



    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Product product)
    {
        if (id != product.Id)
            return BadRequest(new { message = "ID do produto não corresponde" });

        var existingProduct = await _context.Products.FindAsync(id);

        if (existingProduct == null)
            return NotFound(new { message = "Produto não encontrado" });

        // ✅ Validação: estoque não pode ser negativo
        if (product.Stock < 0)
            return BadRequest(new { message = "Estoque não pode ser negativo" });

        existingProduct.Name = product.Name;
        existingProduct.Description = product.Description;
        existingProduct.Price = product.Price;
        existingProduct.Stock = product.Stock;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Products.Any(e => e.Id == id))
                return NotFound(new { message = "Produto não encontrado" });
            else
                throw;
        }

        return NoContent();
    }
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
            return NotFound(new { message = "Produto não encontrado" });

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}