using AutoMapper;
using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Controllers
{
    [ApiController]
    [Route("api/libros")]
    [Authorize(Policy = "esadmin")]
    public class LibrosController: ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly ITimeLimitedDataProtector protectorLimitado;

        public LibrosController(ApplicationDbContext context, IMapper mapper, IDataProtectionProvider protectionProvider)
        {
            this.context = context;
            this.mapper = mapper;
            protectorLimitado = protectionProvider.CreateProtector("LibrosController").ToTimeLimitedDataProtector();
        }

        [HttpGet("listado/obtenertoken")]
        public ActionResult ObtenerTokenListado()
        {
            var textoPlano = Guid.NewGuid().ToString();
            var token = protectorLimitado.Protect(textoPlano, TimeSpan.FromSeconds(30));
            var url = Url.RouteUrl("ObtenerListadoConToken", new { token }, "https");
            return Ok(new { url });
        }

        [HttpGet("listado/{token}", Name = "ObtenerListadoConToken")]
        [AllowAnonymous]
        public async Task<ActionResult> ObtenerListadoConToken(string token)
        {
            try
            {
                protectorLimitado.Unprotect(token);
            }
            catch
            {
                ModelState.AddModelError(nameof(token), "Token expirado o inválido");
                return ValidationProblem();
            }

            var libros = await context.Libros.ToListAsync();
            var librosDTO = mapper.Map<IEnumerable<LibroDTO>>(libros);
            return Ok(librosDTO);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IEnumerable<LibroDTO>> Get()
        {
            var libros = await context.Libros.ToListAsync();
            var librosDTO = mapper.Map<IEnumerable<LibroDTO>>(libros);
            return librosDTO;
        }

        [HttpGet("{id:int}", Name ="ObtenerLibro")]
        [AllowAnonymous]
        public async Task<ActionResult<LibrosConAutoresDTO>> Get(int id)
        {
            var libro = await context.Libros
                .Include(x => x.Autores)
                .ThenInclude(x => x.Autor)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (libro == null)
            {
                return NotFound();
            }

            var libroDTO = mapper.Map<LibrosConAutoresDTO>(libro);

            return libroDTO;
        }

        [HttpPost]
        public async Task<ActionResult> Post(LibroCreacionDTO libroCreacionDTO)
        {
            if (!await ValidarAutores(libroCreacionDTO))
            {
                return ValidationProblem();
            }

            var libro = mapper.Map<Libro>(libroCreacionDTO);
            AsignarOrdenAutores(libro);

            context.Add(libro);
            await context.SaveChangesAsync();

            var libroDTO = mapper.Map<LibroDTO>(libro);
            return CreatedAtRoute("ObtenerLibro", new { id = libro.Id }, libroDTO);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Put(int id, LibroCreacionDTO libroCreacionDTO)
        {
            if (!await ValidarAutores(libroCreacionDTO))
            {
                return ValidationProblem();
            }

            var libroDB = await context.Libros
                .Include(x => x.Autores)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (libroDB is null)
            {
                return NotFound();
            }

            mapper.Map(libroCreacionDTO, libroDB);
            AsignarOrdenAutores(libroDB);

            await context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var registrosBorrados = await context.Libros.Where(x => x.Id == id).ExecuteDeleteAsync();

            if (registrosBorrados == 0)
            {
                return NotFound();
            }

            return NoContent();
        }

        private void AsignarOrdenAutores(Libro libro)
        {
            if (libro.Autores != null)
            {
                for (int i = 0; i < libro.Autores.Count; i++)
                {
                    libro.Autores[i].Orden = i + 1;
                }
            }
        }

        private async Task<bool> ValidarAutores(LibroCreacionDTO dto)
        {
            if (dto.AutoresIds == null || dto.AutoresIds.Count == 0)
            {
                ModelState.AddModelError(nameof(dto.AutoresIds), "No se puede crear un libro sin autores");
                return false;
            }

            var autoresIdsExisten = await context.Autores
                .Where(x => dto.AutoresIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync();

            if (autoresIdsExisten.Count != dto.AutoresIds.Count)
            {
                var autoresNoExisten = dto.AutoresIds.Except(autoresIdsExisten);
                var mensaje = $"No existen los autores con los siguientes ids: {string.Join(", ", autoresNoExisten)}";

                ModelState.AddModelError(nameof(dto.AutoresIds), mensaje);
                return false;
            }

            return true;
        }

    }
}
