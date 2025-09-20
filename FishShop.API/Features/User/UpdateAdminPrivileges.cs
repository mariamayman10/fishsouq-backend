using Carter;
using FishShop.API.Database;
using FishShop.API.Entities;
using FishShop.API.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FishShop.API.Features;
public class UpdatePrivilegesRequest
{
    public bool CanAddProduct { get; set; }
    public bool CanUpdateProduct { get; set; }
    public bool CanDeleteProduct { get; set; }

    public bool CanAddCategory { get; set; }
    public bool CanUpdateCategory { get; set; }
    public bool CanDeleteCategory { get; set; }

    public bool CanUpdateOrderStatus { get; set; }
}
public class UpdateAdminPrivileges
{
    public record Command(string AdminId, UpdatePrivilegesRequest Privileges) : IRequest<Result>;

    internal sealed class Handler(AppDbContext db) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken ct)
        {
            var entity = await db.AdminPrivileges
                .FirstOrDefaultAsync(p => p.AdminId == request.AdminId, ct);

            if (entity is null)
            {
                // Create record if not exists
                entity = new AdminPrivileges
                {
                    AdminId = request.AdminId
                };
                db.AdminPrivileges.Add(entity);
            }

            entity.CanAddProduct = request.Privileges.CanAddProduct;
            entity.CanUpdateProduct = request.Privileges.CanUpdateProduct;
            entity.CanDeleteProduct = request.Privileges.CanDeleteProduct;

            entity.CanAddCategory = request.Privileges.CanAddCategory;
            entity.CanUpdateCategory = request.Privileges.CanUpdateCategory;
            entity.CanDeleteCategory = request.Privileges.CanDeleteCategory;

            entity.CanUpdateOrderStatus = request.Privileges.CanUpdateOrderStatus;

            await db.SaveChangesAsync(ct);

            return Result.Success();
        }
    }
}
public class UpdateAdminPrivilegesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("api/admins/{adminId}/privileges", async (
                string adminId,
                UpdatePrivilegesRequest request,
                ISender sender) =>
            {
                var result = await sender.Send(new UpdateAdminPrivileges.Command(adminId, request));
                return result.Resolve();
            })
            .RequireAuthorization(PolicyConstants.ManagerPolicy);
    }
}