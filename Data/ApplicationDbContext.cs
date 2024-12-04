using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Products> Products { get; set; }

    public DbSet<Warehouses> Warehouses { get; set; }

    public DbSet<products_inventory> Products_inventory { get; set; }

    public DbSet<sales> Sales { get; set; }

    public DbSet<sales_detail> Sales_detail { get; set; }

    public DbSet<log_sales> Log_sales { get; set; }



}
