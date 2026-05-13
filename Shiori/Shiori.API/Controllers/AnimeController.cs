using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using Shiori.Core.DTOs;
using Shiori.Core.Interfaces;

namespace Shiori.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/anime")]
    public class AnimeController : ControllerBase
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly IAnimeService _animeService;
        private readonly IAnimeRepository _animeRepository;
        private readonly IJikanApiService _jikanApiService;

        public AnimeController(
            IAnimeService animeService,
            IAnimeRepository animeRepository,
            IJikanApiService jikanApiService)
        {
            _animeService = animeService;
            _animeRepository = animeRepository;
            _jikanApiService = jikanApiService;
        }

        // Obtener detalles del anime
        [HttpGet("getAnime/{animeId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Anime))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetAnimeDetail(int animeId)
        {
            var anime = await _animeService.GetOrImportAnimeAsync(animeId);

            if (anime == null)
                return NotFound(new { message = $"No se encontró ningún anime con el ID {animeId}" });

            return Ok(anime);
        }

        // Búsqueda de anime en BD local
        [HttpGet("searchAnime/local/{animeName}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Anime>))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> SearchLocal(string animeName)
        {
            var results = await _animeRepository.SearchAnimesLocalAsync(animeName);

            if (results == null || !results.Any())
                return NotFound(new { message = $"No hay resultados en base de datos para: {animeName}" });

            return Ok(results);
        }

        // Búsqueda e anime en Jikan
        [HttpGet("searchAnime/external/{animeName}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Anime>))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> SearchExternal(string animeName)
        {
            var results = await _animeService.SearchExternalAsync(animeName);

            if (results == null || !results.Any())
                return NotFound(new { message = "No se encontraron resultados en Jikan" });

            return Ok(results);
        }

        // Obtener el top de animes
        [HttpGet("topAnimes")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<AnimeExternalDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetTop([FromQuery] int limit = 25)
        {
            if (limit > 25)
                return BadRequest(new { message = "El límite máximo permitido es 25" });
            if (limit <= 0)
                return BadRequest(new { message = "No es posible solicitar un top con valor negativo" });

            var result = await _jikanApiService.GetTopAnimesAsync(limit);

            if (result == null || !result.Any())
                return NotFound(new { message = "No se pudo recuperar el Top Anime" });

            return Ok(result);
        }

        // Borrar un anime (Admin)
        [HttpDelete("deleteAnime/{animeId}")]
        [Authorize(Policy = "admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteAnime(int animeId)
        {
            var result = await _animeRepository.DeleteAnimeAsync(animeId);
            if (!result)
                return NotFound(new { message = "El anime no existe o ya fue eliminado" });

            return Ok(new { message = "Eliminado correctamente" });
        }
    }
}