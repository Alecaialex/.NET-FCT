using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using Shiori.Core.DTOs;
using Shiori.Core.Entities;
using Shiori.Core.Interfaces;

namespace Shiori.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/user")]
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

        // Obtener la lista de animes del usuario
        [HttpGet("animeList")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<UserAnime>))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetUserAnimeList([FromQuery] UserAnimeFilterDto? filter)
        {
            var currentUser = await GetCurrentUser();
            var userAnimes = await _userAnimeRepository.GetUserAnimesAsync(currentUser!, filter);

            if (userAnimes == null || !userAnimes.Any())
                return NotFound(new { message = "No se han encontrado animes" });

            return Ok(userAnimes);
        }

        // Obtener estadísticas del usuario
        [HttpGet("stats")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserStatsDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserStatsDto>> GetStats()
        {
            var currentUser = await GetCurrentUser();
            var stats = await _userAnimeRepository.GetUserStatsAsync(currentUser!);

            if (stats == null)
                return NotFound(new { message = "No hay estadísticas disponibles" });

            return Ok(stats);
        }

        // Añadir anime a la lista del usuario
        [HttpPost("animeList/add/{animeId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> AddAnimeToList(int animeId)
        {
            var currentUser = await GetCurrentUser();
            var anime = await _animeRepository.GetAnimeByJikanIdAsync(animeId);
            if (anime == null)
                return NotFound(new { message = "No se han encontrado animes" });

            await _userAnimeRepository.AddUserAnimeAsync(currentUser!, anime.JikanId);

            return Ok(new { message = "Anime añadido a tu lista correctamente" });
        }

        // Actualizar estado, progreso o puntuación
        [HttpPut("animeList/update/{animeId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> UpdateAnimeProgress(int animeId, [FromBody] UpdateUserAnimeDto updateDto)
        {
            var currentUser = await GetCurrentUser();

            var actualizado = await _userAnimeRepository.UpdateUserAnimeAsync(
                currentUser!, animeId, updateDto.Score, updateDto.Progress, updateDto.Status);

            if (!actualizado)
                return NotFound(new { message = "El anime no se encuentra en tu lista personal" });

            return Ok(new { message = "Progreso actualizado con éxito" });
        }

        private async Task<User?> GetCurrentUser() => await _userRepository.GetCurrentUser();
    }
}