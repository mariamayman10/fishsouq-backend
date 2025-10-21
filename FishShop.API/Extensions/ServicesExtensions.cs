using System.Net;
using System.Net.Mail;
using Carter;
using FishShop.API.Database;
using FishShop.API.Entities;
using FishShop.API.Infrastructure.Email;
using FishShop.API.Infrastructure.Middleware;
using FishShop.API.Shared;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FishShop.API.Extensions;

public static class ServicesExtensions
{
    public static void AddRequiredServices(this IServiceCollection services, IConfiguration configuration,
        IWebHostEnvironment env)
    {
        var assembly = typeof(Program).Assembly;

        services.AddEndpointsApiExplorer()
            .AddSwagger()
            .AddTransient<IEmailSender<User>, EmailSender>()
            .AddScoped<ICustomEmailService, CustomEmailService>()
            .AddDbContext<AppDbContext>(o => o.UseNpgsql(configuration.GetConnectionString("Database")))
            .AddMediatR(config => config.RegisterServicesFromAssembly(assembly))
            .AddCarter()
            .AddValidatorsFromAssembly(assembly)
            .AddIdentityServices()
            .AddAppAuthentication(configuration)
            .AddAppAuthorization()
            .AddAppCors()
            .AddExceptionHandler<GlobalExceptionHandler>()
            .AddProblemDetails()
            .Configure<EmailSettings>(configuration.GetSection("EmailSettings"))
            .AddSingleton(new SmtpClient
            {
                Host = configuration["EmailSettings:SmtpHost"]!,
                Port = int.Parse(configuration["EmailSettings:SmtpPort"]!),
                EnableSsl = true,
                Credentials = new NetworkCredential(configuration["EmailSettings:SmtpUsername"]!,
                    configuration["EmailSettings:SmtpPassword"]!)
            })
            .AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin",
                    policy =>
                    {
                        policy.WithOrigins("http://localhost:4200").AllowAnyMethod().AllowAnyHeader()
                            .AllowCredentials();
                    });
            })
            .AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddSlidingWindowLimiter(RateLimitPolicyConstants.Sliding, limiterOptions =>
                {
                    limiterOptions.PermitLimit = 10; // Max 10 requests
                    limiterOptions.Window = TimeSpan.FromMinutes(1);
                    limiterOptions.SegmentsPerWindow = 6; // Refreshes every 10 seconds
                });

                // Concurrency Limiter: Only 2 requests can be processed at the same time
                options.AddConcurrencyLimiter(RateLimitPolicyConstants.Concurrent, limiterOptions =>
                {
                    limiterOptions.PermitLimit = 2; // Only 2 concurrent requests allowed
                });
            });
        
    }
}