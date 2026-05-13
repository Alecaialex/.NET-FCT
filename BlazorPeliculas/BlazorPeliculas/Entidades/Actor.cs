using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;

namespace BlazorPeliculas.Entidades
{
    public class Actor
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "El campo {0} es requerido")]
        public string? Nombre { get; set; }
        public string? FotoUrl { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public IBrowserFile? FotoArchivo { get; set; }
    }
}
