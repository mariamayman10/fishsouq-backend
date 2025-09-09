using System.Transactions;
using FishShop.API.Database.Configurations;
using FishShop.API.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FishShop.API.Database;

public class AppDbContext(DbContextOptions options) : IdentityDbContext<User>(options)
{
    private static readonly TransactionOptions TransactionOptions = new()
    {
        IsolationLevel = IsolationLevel.Snapshot,
        Timeout = TimeSpan.FromSeconds(10)
    };

    public DbSet<Category> Categories { get; init; }
    public DbSet<Product> Products { get; init; }
    public DbSet<Order> Orders { get; init; }
    public DbSet<OrderProduct> OrderProducts { get; init; }
    public DbSet<ProductSales> ProductSales { get; init; }
    public DbSet<RefreshToken> RefreshTokens { get; init; }
    public DbSet<UserAddress> UserAddresses { get; init; }
    public DbSet<AdminPrivileges> AdminPrivileges { get; init; }
    public DbSet<PromoCode> PromoCodes { get; init; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new OrderConfiguration());
        modelBuilder.ApplyConfiguration(new OrderProductConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        modelBuilder.ApplyConfiguration(new UserAddressConfiguration());
        modelBuilder.ApplyConfiguration(new AdminPrivilegesConfiguration());
        modelBuilder.ApplyConfiguration(new PromoCodeConfiguration());
    }

    public override int SaveChanges()
    {
        using (var scope = new TransactionScope(TransactionScopeOption.Required, TransactionOptions,
                   TransactionScopeAsyncFlowOption.Enabled))
        {
            var x = base.SaveChanges();

            scope.Complete();

            return x;
        }
    }
}