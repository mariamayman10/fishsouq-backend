using FishShop.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FishShop.API.Database.Configurations;

public class AdminPrivilegesConfiguration: IEntityTypeConfiguration<AdminPrivileges>
{
    public void Configure(EntityTypeBuilder<AdminPrivileges> builder)
    {
        builder.HasKey(p => p.AdminId);

        builder.HasOne(p => p.Admin)
            .WithOne()
            .HasForeignKey<AdminPrivileges>(p => p.AdminId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}