using ConsumerB.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConsumerB.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ProductEntity> Products => Set<ProductEntity>();

    // La generazione dell'Id (chiave string) ora vive qui, a livello di
    // accesso dati, e non più nel controller: il controller CRUD generico
    // della libreria non sa nulla di come Consumer B genera le sue chiavi.
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<ProductEntity>())
        {
            if (entry.State == EntityState.Added && string.IsNullOrWhiteSpace(entry.Entity.Id))
            {
                entry.Entity.Id = Guid.NewGuid().ToString("N");
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
