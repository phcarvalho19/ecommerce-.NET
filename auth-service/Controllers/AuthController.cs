using auth_service.Data;
using auth_service.DTOs;
using auth_service.Models;
using auth_service.Security;
using Microsoft.EntityFrameworkCore;

using auth_service.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace auth_service.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _service;
    private readonly AuthDbContext _context;

    public AuthController(AuthService service, AuthDbContext context)
    {
        _service = service;
        _context = context;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginDTO dto)
    {
        var token = await _service.Login(dto);

        if (token == null)
            return Unauthorized();

        return Ok(new { token });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("promote-admin/{userId}")]
    public async Task<IActionResult> PromoteToAdmin(int userId)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return NotFound();

        user.Role = "Admin";

        await _context.SaveChangesAsync();

        return Ok("User promoted to admin");
    }
    [HttpPost("register")]
[AllowAnonymous]
public async Task<IActionResult> Register(RegisterDTO dto)
{
    try
    {
        var user = await _service.Register(dto);
        return Ok(new { user.Id, user.Email });
    }
    catch (Exception ex)
    {
        return BadRequest(ex.Message);
    }
}
}