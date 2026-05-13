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
            _config = config;

            JwtKey = _config["LlaveJwt"] ?? throw new Exception("Falta LlaveJwt");
            ConnectionString = _config["ConnectionString"] ?? throw new Exception("Falta ConnectionString");

            UpdateIntervalHours = int.Parse(_config["UpdateIntervalHours"] ?? "1");
        }
    }
}