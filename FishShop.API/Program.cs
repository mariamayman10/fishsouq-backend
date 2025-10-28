using Carter;
using FishShop.API.Extensions;
using FishShop.API.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment()) 
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.ConfigureLogging();

builder.Services.AddRequiredServices(builder.Configuration, builder.Environment);
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PolicyConstants.ManagerOrAdminPolicy, policy =>
        policy.RequireRole(RolesConstants.ManagerRole, RolesConstants.AdminRole));
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy => 
        policy
            .WithOrigins("https://fishsouq.vercel.app", "http://localhost:4200", "https://thefishsouq-dashboard.vercel.app") // Angular dev server
            .AllowAnyMethod()
            .AllowAnyHeader()
    );
});

Console.WriteLine($"🔍 DEBUG Connection String: {builder.Configuration.GetConnectionString("Database")}");

var app = builder.Build();
app.UseStaticFiles();


// Enable CORS (place AFTER UseRouting but BEFORE UseAuthorization)
app.UseCors("AllowAngularDev");

// var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.AddRequiredMiddlewares();
app.Run();