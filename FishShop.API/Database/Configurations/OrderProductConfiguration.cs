using FishShop.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class OrderProductConfiguration : IEntityTypeConfiguration<OrderProduct>
{
    public void Configure(EntityTypeBuilder<OrderProduct> builder)
    {
        // composite key: OrderId + ProductSizeId
        builder.HasKey(op => new { op.OrderId, op.ProductSizeId });

        builder.Property(op => op.Quantity)
            .IsRequired();

        builder.Property(op => op.UnitPrice)
            .IsRequired()
            .HasPrecision(18, 2);

        // FK to Order
        builder.HasOne(op => op.Order)
            .WithMany(o => o.OrderProducts)
            .HasForeignKey(op => op.OrderId);

        // FK to ProductSize
        builder.HasOne(op => op.ProductSize)
            .WithMany(ps => ps.OrderProducts)
            .HasForeignKey(op => op.ProductSizeId);
    }
}