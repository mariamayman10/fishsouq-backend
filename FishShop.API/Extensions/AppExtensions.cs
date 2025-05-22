using System.Security.Claims;
using Carter;
using FishShop.API.Entities;
using FishShop.API.Features.Auth;
using FishShop.API.Shared;
using Serilog;

namespace FishShop.API.Extensions;

public static class AppExtensions
{
    public static void AddRequiredMiddlewares(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();

            app.ApplyMigrations();
        }

        app.UseExceptionHandler();

        app.UseSerilogRequestLogging(options =>
        {
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? string.Empty);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"]);

                if (httpContext.User.Identity?.IsAuthenticated == true)
                {
                    diagnosticContext.Set("UserId",
                        httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
                    diagnosticContext.Set("UserName",
                        httpContext.User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty);
                }
            };
        });

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseCors("AllowSpecificOrigin");
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapCustomizedIdentityApi<User>().RequireRateLimiting(RateLimitPolicyConstants.Sliding);
        app.MapCarter();
    }
}