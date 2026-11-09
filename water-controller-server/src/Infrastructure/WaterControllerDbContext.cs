using Microsoft.EntityFrameworkCore;
using Model;

namespace Infrastructure;

public sealed class WaterControllerDbContext(DbContextOptions<WaterControllerDbContext> options)
    : DbContext(options)
{
    public DbSet<DeviceType> DeviceTypes => Set<DeviceType>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Metric> Metrics => Set<Metric>();
    public DbSet<DeviceMetric> DeviceMetrics => Set<DeviceMetric>();
    public DbSet<Measurement> Measurements => Set<Measurement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeviceType>(entity =>
        {
            entity.ToTable("device_types");
            entity.HasKey(e => e.DeviceTypeId);
            entity.Property(e => e.DeviceTypeId).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.ToTable("devices");
            entity.HasKey(e => e.DeviceId);
            entity.Property(e => e.DeviceId).HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity
                .HasOne(e => e.DeviceType)
                .WithMany(t => t.Devices)
                .HasForeignKey(e => e.DeviceTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Metric>(entity =>
        {
            entity.ToTable("metrics");
            entity.HasKey(e => e.MetricId);
            entity.Property(e => e.MetricId).ValueGeneratedOnAdd();
            entity.Property(e => e.Code).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Unit).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Code).IsUnique();
        });

        modelBuilder.Entity<DeviceMetric>(entity =>
        {
            entity.ToTable("device_metrics");
            entity.HasKey(e => new { e.DeviceId, e.MetricId });
            entity
                .HasOne(e => e.Device)
                .WithMany(d => d.DeviceMetrics)
                .HasForeignKey(e => e.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne(e => e.Metric)
                .WithMany(m => m.DeviceMetrics)
                .HasForeignKey(e => e.MetricId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Measurement>(entity =>
        {
            entity.ToTable("measurements");
            entity.HasKey(e => e.MeasurementId);
            entity.Property(e => e.MeasurementId).ValueGeneratedOnAdd();
            entity.Property(e => e.DeviceId).IsRequired();
            entity.Property(e => e.MetricId).IsRequired();
            entity
                .HasOne(e => e.Device)
                .WithMany(d => d.Measurements)
                .HasForeignKey(e => e.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne(e => e.Metric)
                .WithMany(m => m.Measurements)
                .HasForeignKey(e => e.MetricId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.DeviceId, e.MetricId, e.Timestamp });
        });
    }
}
