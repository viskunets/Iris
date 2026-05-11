using Microsoft.EntityFrameworkCore;
using EstimateApp.Models;

namespace EstimateApp.Data;

public class AppDbContext : DbContext
{
    public DbSet<Equipment> Equipments => Set<Equipment>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=equipment.db");
    }
}

public class Equipment
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public string Category { get; set; } = "General";
}
