using Microsoft.AspNetCore.Identity;
using Shiori.Infra.Data;
using Microsoft.EntityFrameworkCore;
using Shiori.Core.Entities;
using Shiori.Core.Interfaces;
using Shiori.Infra.Repositories;
using Shiori.Infra.Services;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Shiori.Infra.BackgroundServices;
using NSwag;
using NSwag.Generation.Processors.Security;

var builder = WebApplication.CreateBuilder(args);

// String y conexión a BD postgre
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// Identity core
builder.Services.AddIdentityCore<User>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<UserManager<User>>();
builder.Services.AddScoped<SignInManager<User>>();
builder.Services.AddDataProtection();
builder.Services.AddHttpContextAccessor();

// Cliente HTTP para Jikan
builder.Services.AddHttpClient<IJikanApiService, JikanApiService>(client =>
{
    client.BaseAddress = new Uri("https://api.jikan.moe/v4/");
});

// Inyección de Dependencias
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAnimeRepository, AnimeRepository>();
builder.Services.AddScoped<IUserAnimeRepository, UserAnimeRepository>();

// Background Service
builder.Services.AddHostedService<TopAnimeUpdateService>();

// Autenticación JWT
builder.Services.AddAuthentication().AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["llavejwt"]!)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("admin", policy => policy.RequireClaim("role", "Admin"));
});

// NSwag config
builder.Services.AddOpenApiDocument(config =>
{
    config.PostProcess = document =>
    {
        document.Info.Version = "v1";
        document.Info.Title = "Shiori API";
        document.Info.Description = "API para la aplicación de seguimiento de anime";
    };

    // Configuración para poder usar el Token JWT en la interfaz de Swagger
    config.AddSecurity("Bearer", Enumerable.Empty<string>(), new OpenApiSecurityScheme
    {
        Type = OpenApiSecuritySchemeType.ApiKey,
        Name = "Authorization",
        In = OpenApiSecurityApiKeyLocation.Header,
        Description = "Escribe: Bearer {tu_token}"
    });

    config.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("Bearer"));
});

// Exception handler global
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();