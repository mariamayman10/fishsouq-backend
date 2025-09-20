using FishShop.API.Entities;
using FishShop.API.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FishShop.API.Database.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(LengthConstants.ProductName);

        builder.Property(p => p.Description)
            .HasMaxLength(LengthConstants.ProductDescription);

        builder.Property(p => p.Price)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.Quantity)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.ImageUrl)
            .IsRequired()
            .HasMaxLength(LengthConstants.ImageUrl);

        builder.HasOne(p => p.ProductSales)
            .WithOne(ps => ps.Product)
            .HasForeignKey<ProductSales>(ps => ps.ProductId);

        builder.HasQueryFilter(e => !EF.Property<bool>(e, "IsDeleted"));
    }
}