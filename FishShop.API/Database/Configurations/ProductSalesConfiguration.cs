using FishShop.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FishShop.API.Database.Configurations;

public class ProductSalesConfiguration : IEntityTypeConfiguration<ProductSales>
{
    public void Configure(EntityTypeBuilder<ProductSales> builder)
    {
        builder.HasKey(c => c.ProductId);

        builder.HasOne<Product>()
            .WithOne(p => p.ProductSales)
            .HasForeignKey<ProductSales>(ps => ps.ProductId);

        builder.Property(c => c.TotalQuantitySold)
            .IsRequired();

        builder.Property(c => c.TotalRevenue)
            .HasPrecision(18, 2);
    }
}