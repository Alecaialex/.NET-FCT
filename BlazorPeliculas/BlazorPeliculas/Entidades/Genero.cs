using BlazorPeliculas.Validaciones;
using System.ComponentModel.DataAnnotations;

namespace BlazorPeliculas.Entidades
{
    public class Genero
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "El campo {0} es requerido")]
        [PrimeraLetraMayusc]
        public string? Nombre { get; set; }
    }
}
