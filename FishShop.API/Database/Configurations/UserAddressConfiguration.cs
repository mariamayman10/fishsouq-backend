using FishShop.API.Entities;
using FishShop.API.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FishShop.API.Database.Configurations;

public class UserAddressConfiguration : IEntityTypeConfiguration<UserAddress>
{
    public void Configure(EntityTypeBuilder<UserAddress> builder)
    {
        builder.HasKey(ua => ua.Id);
        
        builder.Property(ua => ua.Name)
            .HasMaxLength(LengthConstants.AddressName)
            .IsRequired();

        builder.Property(ua => ua.Street)
            .HasMaxLength(LengthConstants.Street)
            .IsRequired();

        builder.Property(ua => ua.Governorate)
            .HasMaxLength(LengthConstants.Governorate)
            .IsRequired();

        builder.Property(ua => ua.BuildingNumber)
            .HasMaxLength(LengthConstants.BuildingNumber)
            .IsRequired();

        builder.Property(ua => ua.FloorNumber)
            .HasMaxLength(LengthConstants.FloorNumber)
            .IsRequired();

        builder.Property(ua => ua.AptNumber)
            .HasMaxLength(LengthConstants.AptNumber)
            .IsRequired();

        builder.HasOne(ua => ua.User)
            .WithMany(u => u.Addresses)
            .HasForeignKey(ua => ua.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}