using AutoMapper;
using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Controllers
{
    [ApiController]
    [Route("api/libros/{libroId:int}/comentarios")]
    [Authorize]
    public class ComentariosController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly IServicioUsuarios servicioUsuarios;

        public ComentariosController(ApplicationDbContext context, IMapper mapper, IServicioUsuarios servicioUsuarios)
        {
            this.context = context;
            this.mapper = mapper;
            this.servicioUsuarios = servicioUsuarios;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<ComentarioDTO>>> Get(int libroId)
        {
            var existeLibro = await context.Libros.AnyAsync(x => x.Id == libroId);

            if (!existeLibro)
            {
                return NotFound();
            }

            var comentarios = await context.Comentarios
                .Include(x => x.Usuario)
                .Where(x => x.LibroId == libroId)
                .OrderByDescending(x => x.FechaPublicacion)
                .ToListAsync();

            return mapper.Map<List<ComentarioDTO>>(comentarios);
        }

        [HttpGet("{id}", Name = "ObtenerComentario")]
        [AllowAnonymous]
        public async Task<ActionResult<ComentarioDTO>> Get(Guid id)
        {
            var comentario = await context.Comentarios
                .Include(x => x.Usuario)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (comentario == null)
            {
                return NotFound();
            }

            return mapper.Map<ComentarioDTO>(comentario);
        }

        [HttpPost]
        public async Task<ActionResult> Post(int libroId, ComentarioCreacionDTO comentarioCreacionDTO)
        {
            if (!await ExisteLibro(libroId))
            {
                return NotFound();
            }

            var usuario = await ObtenerUsuarioActual();
            if (usuario == null)
            {
                return Unauthorized();
            }

            var comentario = mapper.Map<Comentario>(comentarioCreacionDTO);
            comentario.LibroId = libroId;
            comentario.FechaPublicacion = DateTime.UtcNow;
            comentario.UsuarioId = usuario.Id;

            context.Add(comentario);
            await context.SaveChangesAsync();

            var comentarioDTO = mapper.Map<ComentarioDTO>(comentario);

            return CreatedAtRoute("ObtenerComentario", new { id = comentario.Id, libroId }, comentarioDTO);
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult> Patch(Guid id, int libroId, JsonPatchDocument<ComentarioPatchDTO> patchDoc)
        {
            if (patchDoc is null)
            {
                return BadRequest();
            }

            if (!await ExisteLibro(libroId))
            {
                return NotFound();
            }

            var usuario = await ObtenerUsuarioActual();
            if (usuario == null)
            {
                return Unauthorized();
            }

            var comentarioDB = await context.Comentarios.FirstOrDefaultAsync(x => x.Id == id);

            if (comentarioDB is null)
            {
                return NotFound();
            }

            if (comentarioDB.UsuarioId != usuario.Id)
            {
                return Forbid();
            }

            var comentarioPatchDTO = mapper.Map<ComentarioPatchDTO>(comentarioDB);

            patchDoc.ApplyTo(comentarioPatchDTO, ModelState);

            var esValido = TryValidateModel(comentarioPatchDTO);

            if (!esValido)
            {
                return ValidationProblem();
            }

            mapper.Map(comentarioPatchDTO, comentarioDB);

            await context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id, int libroId)
        {
            if (!await ExisteLibro(libroId))
            {
                return NotFound();
            }

            var usuario = await ObtenerUsuarioActual();
            if (usuario == null)
            {
                return Unauthorized();
            }

            var comentarioDB = await context.Comentarios.FirstOrDefaultAsync(x => x.Id == id);

            if (comentarioDB == null)
            {
                return NotFound();
            }

            if (comentarioDB.UsuarioId != usuario.Id)
            {
                return Forbid();
            }

            context.Remove(comentarioDB);
            context.SaveChanges();

            return NoContent();
        }

        private async Task<bool> ExisteLibro(int libroId)
        {
            return await context.Libros.AnyAsync(x => x.Id == libroId);
        }

        private async Task<Usuario?> ObtenerUsuarioActual()
        {
            return await servicioUsuarios.ObtenerUsuario();
        }
    }
}
