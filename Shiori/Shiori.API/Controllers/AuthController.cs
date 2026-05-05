using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NLog;
using Shiori.Core.DTOs;
using Shiori.Core.Entities;
using Shiori.Core.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Shiori.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IUserRepository _userRepository;

        public AuthController(UserManager<User> userManager, SignInManager<User> signInManager, IConfiguration configuration, IUserRepository userRepository)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _userRepository = userRepository;
        }

        // Registro de un nuevo usuario
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] UserCredentialsDto userCredentials)
        {
            logger.Info("Intento de registro para el email: {0}", userCredentials.Email);

            var user = new User
            {
                UserName = userCredentials.Email,
                Email = userCredentials.Email
            };

            var result = await _userManager.CreateAsync(user, userCredentials.Password!);

            if (!result.Succeeded)
            {
                logger.Warn("Registro fallido para {0}. Errores: {1}",
                    userCredentials.Email,
                    string.Join(", ", result.Errors.Select(e => e.Description)));

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(error.Code, error.Description);
                }

                return ValidationProblem();
            }

            logger.Info("Usuario registrado con éxito: {0}", userCredentials.Email);
            return await BuildToken(userCredentials);
        }

        // Login de usuario
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] UserCredentialsDto userCredentials)
        {
            logger.Info("Intento de login para: {0}", userCredentials.Email);

            var user = await _userManager.FindByEmailAsync(userCredentials.Email);

            if (user is null)
            {
                logger.Warn("Login fallido: El email {0} no existe en BD.", userCredentials.Email);
                return ReturnIncorrectLogin();
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, userCredentials.Password!, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                logger.Info("Login exitoso para el usuario: {0} (Rol: {1})", userCredentials.Email, user.Role);
                return await BuildToken(userCredentials);
            }
            else
            {
                logger.Warn("Login fallido: Contraseña incorrecta para el usuario {0}.", userCredentials.Email);
                return ReturnIncorrectLogin();
            }
        }

        // Actualizar los datos de un usuario (Solo Admin)
        [HttpPut("update")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserDto updateUserDto)
        {
            logger.Info("ADMIN ACTION: Intento de actualizar usuario {0}", updateUserDto.Email);

            var result = await _userRepository.UpdateUserAsync(updateUserDto);

            if (result)
            {
                logger.Info("Usuario {0} actualizado exitosamente por el administrador.", updateUserDto.Email);
                return Ok(new { message = "Usuario actualizado exitosamente." });
            }
            else
            {
                logger.Warn("Fallo en la actualización del usuario {0}.", updateUserDto.Email);
                return BadRequest(new { message = "No se pudo actualizar el usuario. Verifica que el email sea correcto." });
            }
        }

        // Eliminar un usuario (Solo Admin)
        [HttpDelete("delete/{email}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(string email)
        {
            logger.Info("ADMIN ACTION: Intento de eliminar usuario con email: {0}", email);

            var result = await _userRepository.DeleteUserAsync(email);

            if (result)
            {
                logger.Info("ADMIN ACTION SUCCESS: Usuario {0} eliminado correctamente.", email);
                return Ok(new { message = $"El usuario {email} ha sido eliminado con éxito." });
            }
            else
            {
                logger.Warn("ADMIN ACTION FAIL: No se pudo eliminar al usuario {0}.", email);
                return NotFound(new { message = "No se pudo encontrar al usuario o ocurrió un error al eliminar." });
            }
        }

        // Construir el token
        private async Task<AuthResponseDto> BuildToken(UserCredentialsDto userCredentialsDto)
        {
            logger.Debug("Generando JWT para {0}", userCredentialsDto.Email);

            var user = await _userManager.FindByEmailAsync(userCredentialsDto.Email);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, userCredentialsDto.Email),
                new Claim(ClaimTypes.Role, user!.Role.ToString())
            };

            var claimsdb = await _userManager.GetClaimsAsync(user!);
            claims.AddRange(claimsdb);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["llavejwt"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiration = DateTime.UtcNow.AddHours(10);
            var securityToken = new JwtSecurityToken(issuer: null, audience: null, claims: claims, expires: expiration, signingCredentials: credentials);

            var token = new JwtSecurityTokenHandler().WriteToken(securityToken);

            logger.Debug("Token generado correctamente para {0}. Expira el: {1}", userCredentialsDto.Email, expiration);

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