using BibliotecaAPI.Validaciones;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BibliotecaAPI.Entidades
{
    public class Autor
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "El campo {0} es requerido")]
        [StringLength(150, ErrorMessage = "El campo {0} debe tener {1} carácteres o menos")]
        [Capitalizar]
        public required string Nombres { get; set; }
        [Required(ErrorMessage = "El campo {0} es requerido")]
        [StringLength(150, ErrorMessage = "El campo {0} debe tener {1} carácteres o menos")]
        [Capitalizar]
        public required string Apellidos { get; set; }
        [StringLength(20, ErrorMessage = "El campo {0} debe tener {1} carácteres o menos")]

        public string? Identificacion { get; set; }
        [Unicode(false)]
        public string? Foto { get; set; }
        public List<AutorLibro> Libros { get; set; } = [];

        //[Range(18,120)]
        //public int Edad { get; set; }

        //[CreditCard]
        //public string? TarjetaDeCredito { get; set; }

        //[Url]
        //public string? URL { get; set; }

        //IValidatableObject
        //public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        //{
            //if (!string.IsNullOrEmpty(Nombre))
            //{
                //var primeraLetra = Nombre[0].ToString();

                //if (primeraLetra != primeraLetra.ToUpper())
                //{
                   //yield return new ValidationResult("La primera letra debe ser mayúscula - validacion por modelo",
                        //new string[] { nameof(Nombre) });
                //}
            //}
        //}
    }
}
