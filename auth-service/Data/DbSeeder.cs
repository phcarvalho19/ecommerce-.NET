using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using auth_service.Models;
using auth_service.Security;
using Microsoft.EntityFrameworkCore;

namespace auth_service.Data
{
    public class DbSeeder
    {
        public static async Task SeedAdmin(AuthDbContext context, PasswordHasher hasher)
        {
            if (await context.Users.AnyAsync(u => u.Role == "Admin"))
                return;

            var admin = new User
            {
                Name = "Admin",
                Email = "admin@admin.com",
                PasswordHash = hasher.HashPassword("admin123"),
                Role = "Admin"
            };

            context.Users.Add(admin);
            await context.SaveChangesAsync();
        }
    }
}