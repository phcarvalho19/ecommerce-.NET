using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using auth_service.Data;
using auth_service.DTOs;
using auth_service.Models;
using auth_service.Security;
using Microsoft.EntityFrameworkCore;

namespace auth_service.Services
{
   public class AuthService
{
    private readonly AuthDbContext _context;
    private readonly PasswordHasher _hasher;
    private readonly JwtTokenGenerator _jwt;

    public AuthService(AuthDbContext context, PasswordHasher hasher, JwtTokenGenerator jwt)
    {
        _context = context;
        _hasher = hasher;
        _jwt = jwt;
    }

  public async Task<User> Register(RegisterDTO dto)
{
    var normalizedEmail = dto.Email.Trim().ToLower();

    var exists = await _context.Users
        .AnyAsync(x => x.Email == normalizedEmail);

    if (exists)
        throw new Exception("Email already registered");

    var user = new User
    {
        Name = dto.Name.Trim(),
        Email = normalizedEmail,
        PasswordHash = _hasher.HashPassword(dto.Password)
    };

    _context.Users.Add(user);
    await _context.SaveChangesAsync();

    return user;
}

    public async Task<string?> Login(LoginDTO dto)
    {
    var email = dto.Email.Trim().ToLower();

    var user = await _context.Users
    .FirstOrDefaultAsync(x => x.Email == email);
        if (user == null)
            return null;

        if (!_hasher.VerifyPassword(dto.Password, user.PasswordHash))
            return null;

        return _jwt.GenerateToken(user);
    }
}
}