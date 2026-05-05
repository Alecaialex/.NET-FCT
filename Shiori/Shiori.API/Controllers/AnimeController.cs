using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using Shiori.Core.DTOs;
using Shiori.Core.Interfaces;

[ApiController]
[Authorize]
[Route("api/anime")]
public class AnimeController : ControllerBase
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    private readonly IAnimeService _animeService;
    private readonly IAnimeRepository _animeRepository;

    public AnimeController(IAnimeService animeService, IAnimeRepository animeRepository)
    {
        _animeService = animeService;
        _animeRepository = animeRepository;
    }

    // 1. Detalle de Anime (Manejo de null en vez de excepción)
    [HttpGet("getAnime/{animeId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Anime))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetAnimeDetail(int animeId)
    {
        var anime = await _animeService.GetOrImportAnimeAsync(animeId);

        if (anime == null)
        {
            logger.Warn("Detalle no encontrado para ID: {0}", animeId);
            return NotFound(new { message = $"No se encontró ningún anime con el ID {animeId}." });
        }

        return Ok(anime);
    }

    // 2. Búsqueda Local
    [HttpGet("searchAnime/local")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Anime>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SearchLocal([FromQuery] string query)
    {
        logger.Info("Búsqueda local: {0}", query);
        var results = await _animeService.SearchLocalAsync(query);

        if (results == null || !results.Any())
        {
            return NotFound(new { message = "No hay resultados en la base de datos local." });
        }

        return Ok(results);
    }

    // 3. Búsqueda Externa (404 si Jikan no devuelve nada)
    [HttpGet("searchAnime/external")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Anime>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SearchExternal([FromQuery] string query)
    {
        logger.Info("Búsqueda externa: {0}", query);
        var results = await _animeService.SearchExternalAsync(query);

        if (results == null || !results.Any())
        {
            logger.Warn("Jikan no devolvió nada para: {0}", query);
            return NotFound(new { message = "No se encontraron resultados en la fuente externa." });
        }

        return Ok(results);
    }

    // 4. Top Animes
    [HttpGet("topAnimes")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<AnimeExternalDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetTop([FromQuery] int limit = 25)
    {
        if (limit > 25) return BadRequest(new { message = "El límite máximo permitido es 25." });
        if (limit <= 0) return BadRequest(new { message = "No es posible solicitar un top con valor negativo"});

        var result = await _animeService.GetTopAnimesAsync(limit);

        if (result == null || !result.Any())
        {
            return NotFound(new { message = "No se pudo recuperar el Top Anime." });
        }

        return Ok(result);
    }

    // 5. Borrado (Admin)
    [HttpDelete("deleteAnime/{animeId}")]
    [Authorize(Policy = "admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAnime(int animeId)
    {
        var result = await _animeRepository.DeleteAnimeAsync(animeId);
        if (!result) return NotFound(new { message = "El anime no existe o ya fue eliminado." });

        return Ok(new { message = "Eliminado correctamente." });
    }
}