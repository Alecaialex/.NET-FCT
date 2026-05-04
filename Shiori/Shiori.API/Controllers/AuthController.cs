using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Shiori.Core.DTOs;
using Shiori.Core.Entities;
using Shiori.Core.Interfaces;
using Shiori.Infra.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Shiori.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConfiguration _configuration;

        public AuthController(UserManager<User> userManager, SignInManager<User> signInManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] UserCredentialsDto userCredentials)
        {
            var user = new User
            {
                UserName = userCredentials.Email,
                Email = userCredentials.Email
             
            };

            var result = await _userManager.CreateAsync(user, userCredentials.Password!);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(error.Code, error.Description);
                }

                return ValidationProblem();
            }

            return await BuildToken(userCredentials);
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] UserCredentialsDto userCredentials)
        {
            var user = await _userManager.FindByEmailAsync(userCredentials.Email);

            if (user is null)
            {
                return ReturnIncorrectLogin();
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, userCredentials.Password!, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return await BuildToken(userCredentials);
            }
            else
            {
                return ReturnIncorrectLogin();
            }
        }

        private async Task<AuthResponseDto> BuildToken(UserCredentialsDto userCredentialsDto)
        {
            var user = await _userManager.FindByEmailAsync(userCredentialsDto.Email);

            var claims = new List<Claim>
            {
                new Claim("email", userCredentialsDto.Email),
                new Claim("role", user!.Role.ToString())
            };

            var claimsdb = await _userManager.GetClaimsAsync(user!);
            claims.AddRange(claimsdb);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["llavejwt"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiration = DateTime.UtcNow.AddHours(10);
            var securityToken = new JwtSecurityToken(issuer: null, audience: null, claims: claims, expires: expiration, signingCredentials: credentials);
            
            var token = new JwtSecurityTokenHandler().WriteToken(securityToken);

            return new AuthResponseDto
            {
                Token = token,
                Expiration = expiration
            };
        }

        private ActionResult ReturnIncorrectLogin()
        {
            ModelState.AddModelError(string.Empty, "Login incorrecto");

            return ValidationProblem();
        }
    }
}
