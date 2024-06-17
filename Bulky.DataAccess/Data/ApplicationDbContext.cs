using Bulky.Models;
using Microsoft.EntityFrameworkCore;

namespace Bulky.DataAccess.Data;

public class ApplicationDbContext : DbContext
{
    public DbSet<Category> Categories { get; set; }
    
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
        : base(options) 
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>().HasData(new List<Category>()
        {
            new(){Id = 1, Name = "Action", DisplayOrder = 1},
            new(){Id = 2, Name = "SciFi", DisplayOrder = 2},
            new(){Id = 3, Name = "History ", DisplayOrder = 3}
        });
    }
}