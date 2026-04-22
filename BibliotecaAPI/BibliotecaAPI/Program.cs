using BibliotecaAPI;
using BibliotecaAPI.Datos;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.Servicios;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using BibliotecaAPI.Swagger;
using BibliotecaAPI.Utilidades;
using Microsoft.AspNetCore.Diagnostics;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Area de servicios

/*
builder.Services.AddOutputCache(opciones =>
{
    opciones.DefaultExpirationTimeSpan = TimeSpan.FromSeconds(60);
});
*/

builder.Services.AddStackExchangeRedisOutputCache(opciones =>
{
    opciones.Configuration = builder.Configuration.GetConnectionString("redis");
});

builder.Services.AddRateLimiter(opciones =>
{
    /*
    opciones.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromSeconds(10),
            }));
    */

    opciones.AddPolicy("general", context =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromSeconds(10),
            });
    });

    opciones.AddPolicy("estricta", context =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 2,
                Window = TimeSpan.FromSeconds(5),
            });
    });

    opciones.AddPolicy("movil", context =>
    {
        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromSeconds(10),
                SegmentsPerWindow = 2,
                QueueLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
    });

    opciones.AddPolicy("cubeta", context =>
    {
        return RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
            factory: _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 5,
                TokensPerPeriod = 2,
                ReplenishmentPeriod = TimeSpan.FromSeconds(10)
            });
    });

    opciones.AddPolicy("concurrencia", context =>
    {
        return RateLimitPartition.GetConcurrencyLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
            factory: _ => new ConcurrencyLimiterOptions
            {
                PermitLimit = 1
            });
    });

    opciones.AddPolicy("prueba-usuario", context =>
    {
        var emailClaim = context.User.Claims.Where(claim => claim.Type == "email").FirstOrDefault()!;
        var email = emailClaim.Value;

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: email,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 2,
                Window = TimeSpan.FromSeconds(10)
            });
    });

    opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    opciones.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers["Retry-After"] = retryAfter.TotalSeconds.ToString(); 
        }

        await context.HttpContext.Response.WriteAsync("Limite excedido, intenta nuevamente más tarde.", cancellationToken);
    };
});

builder.Services.AddDataProtection();

var origenesPermitidos = builder.Configuration.GetSection("OrigenesPermitidos").Get<string[]>()!;

builder.Services.AddCors(opciones =>
{
    opciones.AddDefaultPolicy(opcionesCORS =>
    {
        opcionesCORS
        .WithOrigins(origenesPermitidos)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .WithExposedHeaders("cantidad-total-registros");
    });
});

builder.Services.AddAutoMapper(typeof(Program));

//builder.Services.AddTransient<IRepositorioValores, RepositorioValores>();

builder.Services.AddControllers(opciones =>
{
    opciones.Filters.Add<FiltroTiempoEjecucion>();
    opciones.Conventions.Add(new AgruparPorVersion());
}).AddNewtonsoftJson();

builder.Services.AddDbContext<ApplicationDbContext>(opciones => 
                                                    opciones.UseSqlServer("name=DefaultConnection"));

builder.Services.AddIdentityCore<Usuario>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<UserManager<Usuario>>();
builder.Services.AddScoped<SignInManager<Usuario>>();
builder.Services.AddTransient<IServicioUsuarios, ServicioUsuarios>();
builder.Services.AddTransient<IServicioHash, ServicioHash>();
builder.Services.AddTransient<IAlmacenadorArchivos, AlmacenadorArchivosLocal>();
builder.Services.AddScoped<MiFiltroDeAccion>();
builder.Services.AddScoped<FiltroValidacionLibro>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication().AddJwtBearer(opciones =>
{
    opciones.MapInboundClaims = false;

    opciones.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime= false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["llavejwt"]!)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(opciones =>
{
    opciones.AddPolicy("esadmin", politica => politica.RequireClaim("esadmin"));
});

builder.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc("v1", 
        new OpenApiInfo 
        { 
            Title = "Biblioteca API",
            Description = "API para gestionar una biblioteca online",
            Contact = new OpenApiContact
            {
                Name = "Alex",
                Email = "alex@example.com",
                Url = new Uri("https://google.com")
            },
            License = new OpenApiLicense
            {
                Name = "MIT",
                Url = new Uri("https://opensource.org/licenses/MIT")
            }
        });

    opciones.SwaggerDoc("v2",
        new OpenApiInfo
        {
            Version = "v2",
            Title = "Biblioteca API",
            Description = "API para gestionar una biblioteca online",
            Contact = new OpenApiContact
            {
                Name = "Alex",
                Email = "alex@example.com",
                Url = new Uri("https://google.com")
            },
            License = new OpenApiLicense
            {
                Name = "MIT",
                Url = new Uri("https://opensource.org/licenses/MIT")
            }
        });

    opciones.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    opciones.OperationFilter<FiltroAutorizacion>();
});

var app = builder.Build();


// Area de middlewares

app.UseExceptionHandler(exceptionHandlerApp => exceptionHandlerApp.Run(async context =>
{
    var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
    var excepcion = exceptionHandlerFeature?.Error!;

    var error = new Error()
    {
        MensajeError = excepcion.Message,
        StackTrace = excepcion.StackTrace,
        Fecha = DateTime.UtcNow
    };

    var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
    dbContext.Add(error);
    await dbContext.SaveChangesAsync();
    await Results.InternalServerError(new { tipo = "Error", mensaje = "Ha ocurrido un error inesperado", estatus = 500 }).ExecuteAsync(context);
}));

app.UseSwagger();
app.UseSwaggerUI(opciones =>
{
    opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "Biblioteca API v1");
    opciones.SwaggerEndpoint("/swagger/v2/swagger.json", "Biblioteca API v2");
});

app.UseStaticFiles();

app.UseRateLimiter();

app.UseCors();

app.UseOutputCache();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
