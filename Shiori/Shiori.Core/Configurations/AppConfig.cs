namespace Shiori.Core.Configurations
{
    public class AppConfig
    {
        public string LlaveJwt { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
        public int UpdateIntervalHours { get; set; }
    }
}