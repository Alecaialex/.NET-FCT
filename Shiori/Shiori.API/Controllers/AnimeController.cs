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
        private readonly IUserAnimeRepository _userAnimeService;
        private readonly IUserRepository _userRepository;

        public AnimeController(IJikanApiService jikanApiService, IAnimeRepository animeRepository, IUserAnimeRepository userAnimeService, IUserRepository userRepository)
        {
            _jikanApiService = jikanApiService;
            _animeRepository = animeRepository;
            _userAnimeService = userAnimeService;
            _userRepository = userRepository;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AnimeExternalDto>> GetAnimeByJikanId(int id)
        {
            var exists = await _animeRepository.GetAnimeByJikanIdAsync(id);

            if (exists != null)
            {
                return Ok(exists);
            }
            var result = await _jikanApiService.GetAnimeByJikanIdAsync(id);

            if (result == null)
            {
                return NotFound();
            }

            await _animeRepository.AddAnimeToDbAsync(result);
            return Ok(result);
        }

        [HttpGet("search/{query}")]
        public async Task<ActionResult<IEnumerable<AnimeExternalDto>>> Search(string query)
        {
            var result = await _animeRepository.SearchAnimesAsync(query);

            if (result == null || !result.Any())
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpGet("top")]
        public async Task<ActionResult<IEnumerable<AnimeExternalDto>>> GetTop()
        {
            var result = await _jikanApiService.GetTopAnimesAsync();
            return Ok(result);
        }

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

            await _userAnimeService.AddUserAnimeAsync(currentUser, id);
            return Ok();
        }

        private async Task<User?> GetCurrentUser()
        {
            return await _userRepository.GetCurrentUser();
        }
    }
}
