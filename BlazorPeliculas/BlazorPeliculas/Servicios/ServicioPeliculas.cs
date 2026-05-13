using BlazorPeliculas.Entidades;

namespace BlazorPeliculas.Servicios
{
    public class ServicioPeliculas : IServicioPeliculas
    {
        public List<Pelicula> ObtenerPeliculas()
        {
            return new List<Pelicula>
            {
                new Pelicula
                {
                    Id = 1,
                    Titulo = "El señor de los anillos",
                    FechaLanzamiento = new DateTime(2005, 2, 14),
                    PosterUrl = "https://upload.wikimedia.org/wikipedia/commons/c/c5/LotR21.png"
                },
                new Pelicula
                {
                    Id = 2,
                    Titulo = "Harry Potter",
                    FechaLanzamiento = new DateTime(2011, 4, 2),
                    PosterUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/6/6b/Harry-potter-and-the-sorcerers-stone-logo_%28cropped%29.svg/1920px-Harry-potter-and-the-sorcerers-stone-logo_%28cropped%29.svg.png"
                }
            };
        }
    }
}
