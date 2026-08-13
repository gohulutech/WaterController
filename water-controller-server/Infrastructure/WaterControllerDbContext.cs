using Microsoft.EntityFrameworkCore;
using Model;

namespace Infrastructure;

public sealed class WaterControllerDbContext(DbContextOptions<WaterControllerDbContext> options)
    : DbContext(options)
{
    public DbSet<Measurement> Measurements => Set<Measurement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Measurement>(entity =>
        {
            entity.ToTable("measurements");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Id).ValueGeneratedOnAdd();
        });
    }
}
