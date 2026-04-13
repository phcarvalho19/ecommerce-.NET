using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using product_service.Data;
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
        return BadRequest();

    var existingProduct = await _context.Products.FindAsync(id);

    if (existingProduct == null)

return NotFound(new { message = "Produto não encontrado" });

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
            return NotFound();
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