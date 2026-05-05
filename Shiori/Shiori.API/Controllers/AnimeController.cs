using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shiori.Core.DTOs;
using Shiori.Core.Entities;
using Shiori.Core.Interfaces;
using NLog;

namespace Shiori.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/anime")]
    public class AnimeController : ControllerBase
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IJikanApiService _jikanApiService;
        private readonly IAnimeRepository _animeRepository;
        private readonly IUserAnimeRepository _userAnimeRepository;
        private readonly IUserRepository _userRepository;

        public AnimeController(IJikanApiService jikanApiService, IAnimeRepository animeRepository, IUserAnimeRepository userAnimeRepository, IUserRepository userRepository)
        {
            _jikanApiService = jikanApiService;
            _animeRepository = animeRepository;
            _userAnimeRepository = userAnimeRepository;
            _userRepository = userRepository;
        }

        // Obtener anime por el ID de JIkan
        [HttpGet("{id}")]
        public async Task<ActionResult> GetAnimeByJikanId(int id)
        {
            logger.Info("Solicitando detalles del anime con ID Jikan: {0}", id);

            var exists = await _animeRepository.GetAnimeByJikanIdAsync(id);
            if (exists != null)
            {
                logger.Debug("Anime {0} encontrado en base de datos local.", id);
                return Ok(exists);
            }

            logger.Info("Anime {0} no encontrado localmente. Consultando API externa Jikan...", id);
            var result = await _jikanApiService.GetAnimeByJikanIdAsync(id);

            if (result == null)
            {
                logger.Warn("No se encontró el anime con ID {0}.", id);
                return NotFound(new { message = $"No se encontró el anime con ID {id}." });
            }

            await _animeRepository.AddAnimeToDbAsync(result);
            logger.Info("Anime {0} ({1}) recuperado de Jikan y guardado en BD local.", id, result.Title);

            return Ok(result);
        }

        // Buscar animes por una query
        [HttpGet("search/{query}")]
        public async Task<ActionResult> Search(string query)
        {
            logger.Info("Iniciando búsqueda de anime con el término: '{0}'", query);
            var externalResults = await _jikanApiService.SearchAnimesAsync(query);

            if (externalResults == null || !externalResults.Any())
            {
                logger.Warn("La búsqueda para '{0}' no devolvió resultados. Intentando búsqueda local...", query);
                var localResults = await _animeRepository.SearchAnimesAsync(query);

                if (localResults == null || !localResults.Any())
                {
                    logger.Warn("Sin resultados locales ni externos para: '{0}'", query);
                    return NotFound(new { message = $"No se encontraron resultados para: {query}" });
                }

                return Ok(localResults);
            }

            logger.Info("Búsqueda exitosa. Procesando {0} resultados para guardar en local.", externalResults.Count());
            foreach (var anime in externalResults)
            {
                var existingAnime = await _animeRepository.GetAnimeByJikanIdAsync(anime.MalId);
                if (existingAnime == null) await _animeRepository.AddAnimeToDbAsync(anime);
            }

            return Ok(externalResults);
        }

        // Obtener los animes top
        [HttpGet("top")]
        public async Task<ActionResult> GetTop([FromQuery] int limit = 25)
        {
            logger.Info("Solicitando Top Animes (Límite: {0})", limit);

            if (limit <= 0) limit = 1;
            if (limit > 25) limit = 25;

            var result = await _jikanApiService.GetTopAnimesAsync(limit);
            if (result == null || !result.Any())
            {
                logger.Error("Fallo de la API o respuesta vacía al intentar obtener el Top Animes de Jikan.");
                return BadRequest(new { message = "No se pudo obtener el top de animes en este momento." });
            }

            return Ok(result);
        }

        // Añadir anime a la lista del usuario
        [HttpPost("{id}/add")]
        public async Task<ActionResult> AddAnimeToUserList(int id)
        {
            var currentUser = await GetCurrentUser();
            if (currentUser == null)
            {
                logger.Warn("Intento de añadir anime {0} sin sesión válida.", id);
                return Unauthorized(new { message = "Sesión no válida o expirada." });
            }

            logger.Info("Usuario {0} intenta añadir anime {1} a su lista.", currentUser.UserName, id);

            var existeAnime = await _animeRepository.GetAnimeByJikanIdAsync(id);
            if (existeAnime == null)
            {
                logger.Error("Error: Usuario {0} intentó añadir anime {1} que no existe en la BD.", currentUser.UserName, id);
                return NotFound(new { message = "El anime que intentas añadir no existe en la base de datos." });
            }

            await _userAnimeRepository.AddUserAnimeAsync(currentUser, id);
            logger.Info("Anime {0} añadido con éxito a la lista de {1}.", id, currentUser.UserName);

            return Ok(new { message = "Anime añadido a tu lista correctamente." });
        }

        // Actualizar info de anime en la lista del usuario (episodios, estado, etc...)
        [HttpPut("{id}/update")]
        public async Task<ActionResult> UpdateAnimeProgress(int id, [FromBody] UpdateUserAnimeDto updateDto)
        {
            var currentUser = await GetCurrentUser();
            if (currentUser == null)
            {
                logger.Warn("Intento de actualizar progreso del anime {0} por usuario no autenticado.", id);
                return Unauthorized(new { message = "Debes estar autenticado para actualizar tu progreso." });
            }

            logger.Info("Actualizando progreso de {0} para el anime {1}. Status: {2}", currentUser.UserName, id, updateDto.Status);

            var actualizado = await _userAnimeRepository.UpdateUserAnimeAsync(
                currentUser,
                id,
                updateDto.Score,
                updateDto.Progress,
                updateDto.Status
            );

            if (!actualizado)
            {
                logger.Warn("Fallo al actualizar: El anime {0} no está en la lista del usuario {1}.", id, currentUser.UserName);
                return NotFound(new { message = "No puedes actualizar un anime que no tienes en tu lista." });
            }

            logger.Info("Progreso actualizado correctamente para usuario {0} en anime {1}.", currentUser.UserName, id);
            return Ok(new { message = "Tu progreso ha sido actualizado con éxito." });
        }

        // Borrar anime (Admin)
        [HttpDelete("{id}")]
        [Authorize(Policy = "admin")]
        public async Task<ActionResult> DeleteAnime(int id)
        {
            logger.Info("ADMIN ACTION: Intento de eliminación del anime con ID {0}.", id);

            var result = await _animeRepository.DeleteAnimeAsync(id);
            if (!result)
            {
                logger.Warn("ADMIN ACTION FAIL: El anime {0} no se pudo eliminar (no existe).", id);
                return NotFound(new { message = $"Error: El anime con ID {id} no existe o ya fue eliminado." });
            }

            logger.Info("ADMIN ACTION SUCCESS: Anime {0} eliminado de la base de datos.", id);
            return Ok(new { message = $"Anime con ID {id} eliminado correctamente." });
        }

        private async Task<User?> GetCurrentUser()
        {
            return await _userRepository.GetCurrentUser();
        }
    }
}