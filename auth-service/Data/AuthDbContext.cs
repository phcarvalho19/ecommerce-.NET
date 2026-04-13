using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using auth_service.Models;
using Microsoft.EntityFrameworkCore;

namespace auth_service.Data
{
    public class AuthDbContext : DbContext
    {
       public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    }
}