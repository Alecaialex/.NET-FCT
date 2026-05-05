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

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        logger.Info("Usuario registrado con éxito: {0}", userCredentials.Email);
        return await BuildToken(userCredentials);
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthResponseDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] UserCredentialsDto userCredentials)
    {
        logger.Info("Intento de login para: {0}", userCredentials.Email);

        var user = await _userManager.FindByEmailAsync(userCredentials.Email);

        if (user is null)
        {
            logger.Warn("Login fallido: El email {0} no existe.", userCredentials.Email);
            return Unauthorized(new { message = "Credenciales incorrectas." });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, userCredentials.Password!, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            logger.Info("Login exitoso para: {0}", userCredentials.Email);
            return await BuildToken(userCredentials);
        }

        logger.Warn("Login fallido: Contraseña incorrecta para {0}.", userCredentials.Email);
        return Unauthorized(new { message = "Credenciales incorrectas." });
    }

    // Nombre más descriptivo para la acción de Admin
    [HttpPut("admin/updateUserRole")]
    [Authorize(Policy = "admin")] // Usando la política que definimos antes
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserRole([FromBody] UpdateUserDto updateUserDto)
    {
        logger.Info("ADMIN ACTION: Actualizando rol de {0}", updateUserDto.Email);

        var result = await _userRepository.UpdateUserAsync(updateUserDto);

        if (!result)
        {
            return NotFound(new { message = "No se encontró el usuario para actualizar." });
        }

        return Ok(new { message = "Usuario actualizado exitosamente." });
    }

    [HttpDelete("admin/deleteUser/{email}")]
    [Authorize(Policy = "admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(string email)
    {
        logger.Info("ADMIN ACTION: Eliminando usuario {0}", email);

        var result = await _userRepository.DeleteUserAsync(email);

        if (!result)
        {
            return NotFound(new { message = "No se pudo encontrar al usuario para eliminar." });
        }

        return Ok(new { message = $"El usuario {email} ha sido eliminado." });
    }

    private async Task<AuthResponseDto> BuildToken(UserCredentialsDto userCredentialsDto)
    {
        var user = await _userManager.FindByEmailAsync(userCredentialsDto.Email);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, userCredentialsDto.Email),
            new Claim(ClaimTypes.Role, user!.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["llavejwt"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiration = DateTime.UtcNow.AddHours(10);

        var securityToken = new JwtSecurityToken(
            issuer: null,
            audience: null,
            claims: claims,
            expires: expiration,
            signingCredentials: credentials);

        return new AuthResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(securityToken),
            Expiration = expiration
        };
    }
}