using ConsumerA.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConsumerA.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ProductEntity> Products => Set<ProductEntity>();
}
