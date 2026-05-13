using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NLog;
using Shiori.Core.DTOs;
using Shiori.Core.Entities;
using Shiori.Core.Interfaces;

namespace Shiori.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;

        public AuthController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IUserRepository userRepository,
            ITokenService tokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _userRepository = userRepository;
            _tokenService = tokenService;
        }

        // Registro de nuevo usuario
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthResponseDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] UserCredentialsDto userCredentials)
        {
            var user = new User
            {
                UserName = userCredentials.Email,
                Email = userCredentials.Email
            };

            var result = await _userManager.CreateAsync(user, userCredentials.Password!);

            if (!result.Succeeded)
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

            return Ok(_tokenService.CreateToken(user));
        }

        // Login de usuario
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthResponseDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] UserCredentialsDto userCredentials)
        {
            var user = await _userManager.FindByEmailAsync(userCredentials.Email!);

            if (user is null)
            {
                logger.Warn("Login fallido: El email {0} no existe", userCredentials.Email);
                return Unauthorized(new { message = "Credenciales incorrectas" });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, userCredentials.Password!, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                logger.Info("Login exitoso para: {0}", userCredentials.Email);
                return Ok(_tokenService.CreateToken(user));
            }

            logger.Warn("Login fallido: Contraseña incorrecta para {0}", userCredentials.Email);
            return Unauthorized(new { message = "Credenciales incorrectas" });
        }

        // Actualizar rol de un usuario (Admin)
        [HttpPut("admin/updateUserRole")]
        [Authorize(Policy = "admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUserRole([FromBody] UpdateUserDto updateUserDto)
        {
            var result = await _userRepository.UpdateUserAsync(updateUserDto);

            if (!result)
            {
                logger.Warn("No se ha encontrado el usuario para actualizar: {0}", updateUserDto.Email);
                return NotFound(new { message = "No se encontró el usuario para actualizar" });
            }

            logger.Info("Actualizado el rol del usuario {0} a {1}", updateUserDto.Email, updateUserDto.Role);
            return Ok(new { message = "Usuario actualizado exitosamente." });
        }

        // Eliminar un usuario (Admin)
        [HttpDelete("admin/deleteUser/{email}")]
        [Authorize(Policy = "admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(string email)
        {
            var result = await _userRepository.DeleteUserAsync(email);

            if (!result)
            {
                logger.Warn("No se ha encontrado el usuario para eliminar: {0}", email);
                return NotFound(new { message = "No se pudo encontrar al usuario para eliminar" });
            }

            logger.Warn("Usuario eliminado: {0}", email);
            return Ok(new { message = $"El usuario {email} ha sido eliminado." });
        }
    }
}