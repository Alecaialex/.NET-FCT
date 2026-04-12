var builder = WebApplication.CreateBuilder(args);

var cadenaDeConexion = builder.Configuration.GetValue<string>("cadenaDeConexion");

var home = builder.Configuration.GetValue<string>("home");

var app = builder.Build();

app.MapGet("/", () => cadenaDeConexion);

app.MapGet("/home", () => home);

app.Run();
