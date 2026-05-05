using Microsoft.AspNetCore.Identity;
using Shiori.Infra.Data;
using Microsoft.EntityFrameworkCore;
using Shiori.Core.Entities;
using Shiori.Core.Interfaces;
using Shiori.Infra.Repositories;
using Shiori.Infra.Services;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Shiori.Infra.Workers;
using NSwag;
using NSwag.Generation.Processors.Security;
using Shiori.Core.Configurations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using NLog;
using NLog.Web;
using System.Security.Claims;

// Nlog
var logger = NLog.Web.NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();

try
{
    logger.Info("Iniciando aplicación");
    var builder = WebApplication.CreateBuilder(args);

    // Nlog
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();


    // AppConfig
    var appConfig = new AppConfig(builder.Configuration);
    builder.Configuration.Bind(appConfig);
    builder.Services.AddSingleton(appConfig);

    // Conexión a BD PostgreSQL
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseNpgsql(appConfig.ConnectionString);
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
    builder.Services.AddHttpClient("JikanClient", client =>
    {
        client.BaseAddress = new Uri("https://api.jikan.moe/v4/");
    });

    // Inyección de Dependencias
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IAnimeRepository, AnimeRepository>();
    builder.Services.AddScoped<IUserAnimeRepository, UserAnimeRepository>();
    builder.Services.AddScoped<IAnimeService, AnimeService>();
    builder.Services.AddSingleton<IJikanApiService, JikanApiService>();

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appConfig.JwtKey)),
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
        };

        options.Events = new JwtBearerEvents
        {
            OnForbidden = context =>
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                var result = System.Text.Json.JsonSerializer.Serialize(new { message = "Acceso denegado: No tienes permisos de administrador." });
                return context.Response.WriteAsync(result);
            },
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                var result = System.Text.Json.JsonSerializer.Serialize(new { message = "No autorizado: Debes iniciar sesión." });
                return context.Response.WriteAsync(result);
            }
        };
    });

    // Servicio de autorización y política para admin
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("admin", policy => policy.RequireClaim(ClaimTypes.Role, "Admin"));
    });

    // NSwag config
    builder.Services.AddOpenApiDocument(config =>
    {
        config.PostProcess = document =>
        {
            document.Info.Version = "v1";
            document.Info.Title = "Shiori API";
        };
        config.AddSecurity("Bearer", Enumerable.Empty<string>(), new OpenApiSecurityScheme
        {
            Type = OpenApiSecuritySchemeType.ApiKey,
            Name = "Authorization",
            In = OpenApiSecurityApiKeyLocation.Header,
            Description = "Escribe: Bearer {tu_token}"
        });
        config.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("Bearer"));
    });

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
}
catch (Exception ex)
{
    // Captura de errores fatales
    logger.Fatal(ex, "La aplicación terminó inesperadamente");
    throw;
}
finally
{
    // Cierre de NLog
    NLog.LogManager.Shutdown();
}