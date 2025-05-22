using FishShop.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment()) 
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.ConfigureLogging();

builder.Services.AddRequiredServices(builder.Configuration, builder.Environment);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy => 
        policy
            .WithOrigins("http://localhost:4200") // Angular dev server
            .AllowAnyMethod()
            .AllowAnyHeader()
    );
});

var app = builder.Build();

// Enable CORS (place AFTER UseRouting but BEFORE UseAuthorization)
app.UseCors("AllowAngularDev");

// var app = builder.Build();

app.AddRequiredMiddlewares();

app.Run();