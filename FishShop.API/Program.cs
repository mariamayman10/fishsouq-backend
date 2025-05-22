using FishShop.API.Extensions;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = Directory.GetCurrentDirectory(),
    EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environments.Production
});

// ✅ Load configs: base, environment, secrets, environment variables
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

if (builder.Environment.IsDevelopment()) 
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.Configuration.AddEnvironmentVariables();
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
            .WithOrigins("https://thefishsouq.vercel.app") // Angular dev server
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
