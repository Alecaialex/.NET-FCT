using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using Shiori.Core.DTOs;
using Shiori.Core.Entities;
using Shiori.Core.Interfaces;

namespace Shiori.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UserController : ControllerBase
    {
        // Definimos el logger de NLog
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IUserRepository _userRepository;
        private readonly IUserAnimeRepository _userAnimeRepository;

        public UserController(IUserRepository userRepository, IUserAnimeRepository userAnimeRepository)
        {
            _userRepository = userRepository;
            _userAnimeRepository = userAnimeRepository;
        }

        // Obtener la lista de animes del usuario actual
        [HttpGet("list")]
        public async Task<ActionResult> GetUserAnimeList([FromQuery] UserAnimeFilterDto? filter)
        {
            var currentUser = await GetCurrentUser();

            if (currentUser == null)
            {
                logger.Warn("Intento de acceder a la lista de animes sin sesión válida.");
                return Unauthorized(new { message = "Sesión no válida o expirada." });
            }

            logger.Info("Recuperando lista de animes filtrada para el usuario: {0}", currentUser.UserName);

            var userAnimes = await _userAnimeRepository.GetUserAnimesAsync(currentUser, filter);

            return Ok(userAnimes);
        }

        // Obtener estadísticas del usuario
        [HttpGet("stats")]
        public async Task<ActionResult<UserStatsDto>> GetStats()
        {
            var currentUser = await GetCurrentUser();

            if (currentUser == null)
            {
                logger.Warn("Intento de obtener estadísticas sin sesión válida.");
                return Unauthorized();
            }

            logger.Info("Generando estadísticas para el usuario: {0}", currentUser.UserName);

            var stats = await _userAnimeRepository.GetUserStatsAsync(currentUser);
            return Ok(stats);
        }

        private async Task<User?> GetCurrentUser()
        {
            return await _userRepository.GetCurrentUser();
        }
    }
}