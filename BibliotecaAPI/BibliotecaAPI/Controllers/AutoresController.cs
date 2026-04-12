using AutoMapper;
using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Controllers
{
    [ApiController]
    [Route("api/autores")]
    [Authorize(Policy = "esadmin")]
    public class AutoresController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly ILogger<AutoresController> logger;
        private readonly IMapper mapper;

        public AutoresController(ApplicationDbContext context, ILogger<AutoresController> logger, IMapper mapper)
        {
            this.context = context;
            this.logger = logger;
            this.mapper = mapper;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IEnumerable<AutorDTO>> Get()
        {
            logger.LogTrace("Obteniendo el listado de autores");
            logger.LogDebug("Obteniendo el listado de autores");
            logger.LogInformation("Obteniendo el listado de autores");
            logger.LogWarning("Obteniendo el listado de autores");
            logger.LogError("Obteniendo el listado de autores");
            logger.LogCritical("Obteniendo el listado de autores");
            var autores = await context.Autores.ToListAsync();
            // var autoresDTO = autores.Select(autor => new AutorDTO { Id = autor.Id, NombreCompleto = $"{autor.Nombres} {autor.Apellidos}" });
            var autoresDTO = mapper.Map<IEnumerable<AutorDTO>>(autores);

            return autoresDTO;
        }

        [HttpGet("{id:int}", Name = "ObtenerAutor")]
        public async Task<ActionResult<AutorConLibrosDTO>> Get(int id)
        {
            var autor = await context.Autores
                .Include(x => x.Libros)
                .ThenInclude(x => x.Libro)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (autor == null)
            {
                return NotFound();
            }

            var autorDTO = mapper.Map<AutorConLibrosDTO>(autor);

            return autorDTO;
        }


        [HttpPost]
        public async Task<ActionResult> Post(AutorCreacionDTO autorCreacionDTO)
        {
            var autor = mapper.Map<Autor>(autorCreacionDTO);
            context.Add(autor);
            await context.SaveChangesAsync();
            var autorDTO = mapper.Map<AutorDTO>(autor);
            return CreatedAtRoute("ObtenerAutor", new { id = autor.Id }, autorDTO);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Put(int id, AutorCreacionDTO autorCreacionDTO)
        {
            var autorDB = await context.Autores.FirstOrDefaultAsync(x => x.Id == id);

            if (autorDB == null)
            {
                return NotFound();
            }

            mapper.Map(autorCreacionDTO, autorDB);
            
            await context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPatch("{id:int}")]
        public async Task<ActionResult> Patch(int id, JsonPatchDocument<AutorPatchDTO> patchDoc)
        {
            if (patchDoc is null)
            {
                return BadRequest();
            }

            var autorDB = await context.Autores.FirstOrDefaultAsync(x => x.Id == id);

            if (autorDB is null)
            {
                return NotFound();
            }

            var autorPatchDTO = mapper.Map<AutorPatchDTO>(autorDB);

            patchDoc.ApplyTo(autorPatchDTO, ModelState);

            var esValido = TryValidateModel(autorPatchDTO);

            if (!esValido)
            {
                return ValidationProblem();
            }

            mapper.Map(autorPatchDTO, autorDB);

            await context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var registrosBorrados = await context.Autores.Where(x => x.Id == id).ExecuteDeleteAsync();

            if (registrosBorrados == 0)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
