using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using Shiori.Core.DTOs;
using Shiori.Core.Entities;
using Shiori.Core.Interfaces;

[ApiController]
[Route("api/user")]
[Authorize]
public class UserController : ControllerBase
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    private readonly IUserRepository _userRepository;
    private readonly IUserAnimeRepository _userAnimeRepository;
    private readonly IAnimeService _animeService;
    private readonly IAnimeRepository _animeRepository;

    public UserController(
        IUserRepository userRepository,
        IUserAnimeRepository userAnimeRepository,
        IAnimeService animeService,
        IAnimeRepository animeRepository)
    {
        _userRepository = userRepository;
        _userAnimeRepository = userAnimeRepository;
        _animeService = animeService;
        _animeRepository = animeRepository;
    }

    // 1. Obtener la lista de animes del usuario
    [HttpGet("animeList")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<UserAnime>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetUserAnimeList([FromQuery] UserAnimeFilterDto? filter)
    {
        var currentUser = await GetCurrentUser();
        if (currentUser == null) return Unauthorized(new { message = "Sesión no válida." });

        logger.Info("Recuperando lista de animes para: {0}", currentUser.UserName);

        var userAnimes = await _userAnimeRepository.GetUserAnimesAsync(currentUser, filter);

        // Si la lista está vacía, devolvemos 404 para que el front pinte el estado "Vacío"
        if (userAnimes == null || !userAnimes.Any())
        {
            return NotFound(new { message = "Tu lista de anime está vacía." });
        }

        return Ok(userAnimes);
    }

    // 2. Obtener estadísticas globales del usuario
    [HttpGet("stats")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserStatsDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserStatsDto>> GetStats()
    {
        var currentUser = await GetCurrentUser();
        if (currentUser == null) return Unauthorized();

        var stats = await _userAnimeRepository.GetUserStatsAsync(currentUser);

        if (stats == null)
        {
            return NotFound(new { message = "No hay estadísticas disponibles todavía." });
        }

        return Ok(stats);
    }

    // 3. Añadir anime a la lista personal
    [HttpPost("animeList/add/{animeId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> AddAnimeToList(int animeId)
    {
        var currentUser = await GetCurrentUser();
        if (currentUser == null) return Unauthorized();

        // Garantizamos que el anime exista localmente antes de vincularlo
        var anime = await _animeRepository.GetAnimeByJikanIdAsync(animeId);
        if (anime == null) return NotFound(new { message = "No se pudo importar el anime seleccionado." });

        await _userAnimeRepository.AddUserAnimeAsync(currentUser, anime.JikanId);

        logger.Info("Usuario {0} añadió el anime {1} a su lista.", currentUser.UserName, animeId);
        return Ok(new { message = "Anime añadido a tu lista correctamente." });
    }

    // 4. Actualizar progreso o puntuación
    [HttpPut("animeList/update/{animeId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateAnimeProgress(int animeId, [FromBody] UpdateUserAnimeDto updateDto)
    {
        var currentUser = await GetCurrentUser();
        if (currentUser == null) return Unauthorized();

        var actualizado = await _userAnimeRepository.UpdateUserAnimeAsync(
            currentUser, animeId, updateDto.Score, updateDto.Progress, updateDto.Status);

        if (!actualizado)
        {
            return NotFound(new { message = "El anime no se encuentra en tu lista personal." });
        }

        return Ok(new { message = "Progreso actualizado con éxito." });
    }

    private async Task<User?> GetCurrentUser() => await _userRepository.GetCurrentUser();
}