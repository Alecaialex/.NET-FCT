using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shiori.Core.DTOs;
using Shiori.Core.Entities;
using Shiori.Core.Interfaces;

namespace Shiori.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/anime")]
    public class AnimeController : ControllerBase
    {
        private readonly IJikanApiService _jikanApiService;
        private readonly IAnimeRepository _animeRepository;
        private readonly IUserAnimeRepository _userAnimeRepository;
        private readonly IUserRepository _userRepository;

        // Inyección de servicios y repositorios
        public AnimeController(IJikanApiService jikanApiService, IAnimeRepository animeRepository, IUserAnimeRepository userAnimeRepository, IUserRepository userRepository)
        {
            _jikanApiService = jikanApiService;
            _animeRepository = animeRepository;
            _userAnimeRepository = userAnimeRepository;
            _userRepository = userRepository;
        }

        // Obtención de anime por ID
        [HttpGet("{id}")]
        public async Task<ActionResult<AnimeExternalDto>> GetAnimeByJikanId(int id)
        {
            // Buscamos en BD
            var exists = await _animeRepository.GetAnimeByJikanIdAsync(id);

            if (exists != null)
            {
                return Ok(exists);
            }

            // En caso de no existir, solicitamos a Jikan
            var result = await _jikanApiService.GetAnimeByJikanIdAsync(id);

            if (result == null)
            {
                return NotFound();
            }

            // Guardamos el anime solicitado a Jikan en BD local
            await _animeRepository.AddAnimeToDbAsync(result);
            return Ok(result);
        }

        [HttpGet("search/{query}")]
        public async Task<ActionResult<IEnumerable<AnimeExternalDto>>> Search(string query)
        {
            // Preguntar siempre a Jikan para obtener resultados mas actualizados
            var externalResults = await _jikanApiService.SearchAnimesAsync(query);

            if (externalResults == null || !externalResults.Any())
            {
                // Si la API no devuelve nada, intentamos buscar en la BD
                var localResults = await _animeRepository.SearchAnimesAsync(query);
                if (localResults == null || !localResults.Any()) return NotFound();

                return Ok(localResults);
            }

            // Guardamos en la BD los resultados que no existían antes
            foreach (var anime in externalResults)
            {
                // Comprobamos si ya existe
                var existingAnime = await _animeRepository.GetAnimeByJikanIdAsync(anime.MalId);

                if (existingAnime == null)
                {
                    await _animeRepository.AddAnimeToDbAsync(anime);
                }
            }

            return Ok(externalResults);
        }

        // Listado de los animes más populares desde el servicio externo
        [HttpGet("top")]
        public async Task<ActionResult<IEnumerable<AnimeExternalDto>>> GetTop()
        {
            var result = await _jikanApiService.GetTopAnimesAsync();
            return Ok(result);
        }

        // Vinculación de un anime a la colección personal del usuario autenticado
        [HttpPost("{id}/add")]
        public async Task<ActionResult> AddAnimeToUserList(int id)
        {
            var currentUser = await GetCurrentUser();
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var existeAnime = await _animeRepository.GetAnimeByJikanIdAsync(id);
            if (existeAnime == null)
            {
                return NotFound();
            }

            await _userAnimeRepository.AddUserAnimeAsync(currentUser, id);
            return Ok();
        }

        // Métodos de administrador
        // Eliminar un anime
        [HttpDelete("{id}")]
        [Authorize(Policy = "admin")]
        public async Task<ActionResult> DeleteAnime(int id)
        {
            var result = await _animeRepository.DeleteAnimeAsync(id);
            if (!result)
            {
                return NotFound();
            }

            return Ok();
        }

        // Método auxiliar para recuperar la entidad del usuario actual
        private async Task<User?> GetCurrentUser()
        {
            return await _userRepository.GetCurrentUser();
        }
    }
}