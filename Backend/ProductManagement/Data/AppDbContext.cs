using Microsoft.EntityFrameworkCore;
using ProductManagementAPI.Entities;

namespace ProductManagementAPI.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<int>("ProductIdSeq")
            .StartsAt(100000)
            .IncrementsBy(1)
            .HasMin(100000)
            .HasMax(999999);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Id)
                .HasDefaultValueSql("NEXT VALUE FOR ProductIdSeq")
                .ValueGeneratedOnAdd();

            entity.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(p => p.Name);

            entity.Property(p => p.Price)
                .HasPrecision(18, 2);

            entity.ToTable(t => t.HasCheckConstraint("CK_Product_Stock_NonNegative", "[Stock] >= 0"));
        });
    }
}
