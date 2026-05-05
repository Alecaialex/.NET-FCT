using Microsoft.Extensions.Configuration;

namespace Shiori.Core.Configurations
{
    public class AppConfig
    {
        private readonly IConfiguration _config;

        public string JwtKey { get; }
        public string ConnectionString { get; }
        public int UpdateIntervalHours { get; }

        public AppConfig(IConfiguration config)
        {
            // 1. Primero guardamos la configuración
            _config = config ?? throw new ArgumentNullException(nameof(config));

            // 2. Ahora ya podemos usar _config porque ya no es null
            JwtKey = _config["LlaveJwt"] ?? throw new Exception("Falta LlaveJwt");
            ConnectionString = _config["ConnectionString"] ?? throw new Exception("Falta ConnectionString");

            // 3. Parseo directo del entero
            UpdateIntervalHours = int.Parse(_config["UpdateIntervalHours"]!);
        }
    }
}