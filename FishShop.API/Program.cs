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
            .WithOrigins("https://thefishsouq-hth4bdcjg0eteha3.uaenorth-01.azurewebsites.net") // Angular dev server
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
