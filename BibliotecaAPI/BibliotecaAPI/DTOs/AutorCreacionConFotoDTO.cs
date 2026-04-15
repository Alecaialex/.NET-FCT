namespace BibliotecaAPI.DTOs
{
    public class AutorCreacionConFotoDTO: AutorCreacionDTO
    {
        public IFormFile? Foto { get; set; }
    }
}
