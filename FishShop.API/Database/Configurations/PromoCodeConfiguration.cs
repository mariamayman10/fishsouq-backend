using FishShop.API.Entities;
using FishShop.API.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FishShop.API.Database.Configurations;

public class PromoCodeConfiguration : IEntityTypeConfiguration<PromoCode>
{
    public void Configure(EntityTypeBuilder<PromoCode> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code)
            .IsRequired()
            .HasMaxLength(LengthConstants.PromoCode);
        builder.HasIndex(p => p.Code)
            .IsUnique();

        builder.Property(p => p.DiscountType)
            .IsRequired();

        builder.Property(p => p.DiscountValue)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.TimesUsed)
            .HasDefaultValue(0);
        

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true);

        builder.Property(p => p.CreatedAt)
            .IsRequired();
        
        builder.Property(p => p.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(p => p.DeletedAt);

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}