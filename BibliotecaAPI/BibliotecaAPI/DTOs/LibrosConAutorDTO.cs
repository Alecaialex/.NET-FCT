namespace BibliotecaAPI.DTOs
{
    public class LibrosConAutoresDTO: LibroDTO
    {
        public List<AutorDTO> Autores { get; set; } = [];
    }
}
