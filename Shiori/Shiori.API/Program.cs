using Microsoft.AspNetCore.Identity;
using Shiori.Infra.Data;
using Microsoft.EntityFrameworkCore;
using Shiori.Core.Entities;
using Shiori.Core.Interfaces;
using Shiori.Infra.Repositories;
using Shiori.Infra.Services;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi;
using Shiori.Infra.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);

// String de conexión a BD PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Add services to the container.

// Añadir el contexto de la base de datos
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// Añadir identity
builder.Services.AddIdentityCore<User>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Servicios para manejar usuarios y inicio de sesión
builder.Services.AddScoped<UserManager<User>>();
builder.Services.AddScoped<SignInManager<User>>();

builder.Services.AddDataProtection();

// Contexto HTTP de la solicitud
builder.Services.AddHttpContextAccessor();

// Singleton API Jikan
builder.Services.AddHttpClient<IJikanApiService, JikanApiService>(client =>
{
    client.BaseAddress = new Uri("https://api.jikan.moe/v4/");
});

// Registrar interfaces y repositorios
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAnimeRepository, AnimeRepository>();
builder.Services.AddScoped<IUserAnimeRepository, UserAnimeRepository>();

// Background service
builder.Services.AddHostedService<TopAnimeUpdateService>();

// JWT
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

builder.Services.AddAuthorization();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1",
        new Microsoft.OpenApi.OpenApiInfo
        {
            Version = "v1",
            Title = "Shiori API",
            Description = "API para la aplicación de seguimiento de anime"
        }
    );

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
