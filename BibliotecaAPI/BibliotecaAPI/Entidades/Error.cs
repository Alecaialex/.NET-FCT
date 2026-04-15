using System.Security.Cryptography.X509Certificates;

namespace BibliotecaAPI.Entidades
{
    public class Error
    {
        public Guid Id { get; set; }
        public required string MensajeError { get; set; }
        public string? StackTrace { get; set; }
        public DateTime Fecha { get; set; }
    }
}
