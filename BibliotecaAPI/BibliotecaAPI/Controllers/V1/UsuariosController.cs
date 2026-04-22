using AutoMapper;
using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BibliotecaAPI.Controllers.V1
{
    [ApiController]
    [Route("api/v1/usuarios")]
    [EnableRateLimiting("estricta")]
    public class UsuariosController : ControllerBase
    {
        private readonly UserManager<Usuario> userManager;
        private readonly IConfiguration configuration;
        private readonly SignInManager<Usuario> signInManager;
        private readonly IServicioUsuarios servicioUsuarios;
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;

        public UsuariosController(UserManager<Usuario> userManager, IConfiguration configuration, SignInManager<Usuario> signInManager, IServicioUsuarios servicioUsuarios, ApplicationDbContext context, IMapper mapper)
        {
            this.userManager = userManager;
            this.configuration = configuration;
            this.signInManager = signInManager;
            this.servicioUsuarios = servicioUsuarios;
            this.context = context;
            this.mapper = mapper;
        }

        [HttpGet]
        [Authorize(Policy = "esadmin")]
        [EnableRateLimiting("prueba-usuario")]
        public async Task<IEnumerable<UsuarioDTO>> Get()
        {
            var usuarios = await context.Users.ToListAsync();
            return mapper.Map<IEnumerable<UsuarioDTO>>(usuarios);
        }

        [HttpPost("registro")]
        public async Task<ActionResult<RespuestaAutenticacionDTO>> Registrar(CredencialesUsuarioDTO credencialesUsuarioDTO)
        {
            var usuario = new Usuario
            {
                UserName = credencialesUsuarioDTO.Email,
                Email = credencialesUsuarioDTO.Email
            };

            var resultado = await userManager.CreateAsync(usuario, credencialesUsuarioDTO.Password!);

            if (!resultado.Succeeded)
            {
                foreach (var error in resultado.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return ValidationProblem();
            }

            return await ConstruirToken(credencialesUsuarioDTO);
        }

        [HttpPost("login")]
        public async Task<ActionResult<RespuestaAutenticacionDTO>> Login(CredencialesUsuarioDTO credencialesUsuarioDTO)
        {
            var usuario = await userManager.FindByEmailAsync(credencialesUsuarioDTO.Email);

            if (usuario is null)
            {
                return DevolverLoginIncorrecto();
            }

            var resultado = await signInManager.CheckPasswordSignInAsync(usuario, credencialesUsuarioDTO.Password!, lockoutOnFailure: false);

            if (resultado.Succeeded)
            {
                return await ConstruirToken(credencialesUsuarioDTO);
            }
            else
            {
                return DevolverLoginIncorrecto();
            }
        }

        [HttpPut]
        [Authorize]
        public async Task<ActionResult> Put(ActualizarUsuarioDTO actualizarUsuarioDTO)
        {
            var usuario = await servicioUsuarios.ObtenerUsuario();

            if (usuario is null)
            {
                return NotFound();
            }

            usuario.FechaNacimiento = actualizarUsuarioDTO.FechaNacimiento;

            var resultado = await userManager.UpdateAsync(usuario);

            if (!resultado.Succeeded)
            {
                return BadRequest(resultado.Errors);
            }

            return NoContent();
        }

        [HttpGet("renovar-token")]
        [Authorize]
        [DisableRateLimiting]
        public async Task<ActionResult<RespuestaAutenticacionDTO>> RenovarToken()
        {
            var usuario = await servicioUsuarios.ObtenerUsuario();

            if (usuario is null)
            {
                return NotFound();
            }

            var credencialesUsuarioDTO = new CredencialesUsuarioDTO { Email = usuario.Email! };

            var respuestaAutenticacion = await ConstruirToken(credencialesUsuarioDTO);

            return respuestaAutenticacion;
        }

        [HttpPost("hacer-admin")]
        //[Authorize(Policy = "esadmin")]
        public async Task<ActionResult> HacerAdmin(EditarClaimDTO editarClaimDTO)
        {
            var usuario = await userManager.FindByEmailAsync(editarClaimDTO.Email);

            if (usuario is null)
            {
                return NotFound();
            }

            await userManager.AddClaimAsync(usuario, new Claim("esadmin", "true"));

            return NoContent();
        }

        [HttpPost("quitar-admin")]
        [Authorize(Policy = "esadmin")]
        public async Task<ActionResult> QuitarAdmin(EditarClaimDTO editarClaimDTO)
        {
            var usuario = await userManager.FindByEmailAsync(editarClaimDTO.Email);

            if (usuario is null)
            {
                return NotFound();
            }

            await userManager.RemoveClaimAsync(usuario, new Claim("esadmin", "true"));

            return NoContent();
        }

        private ActionResult DevolverLoginIncorrecto()
        {
            ModelState.AddModelError(string.Empty, "Login incorrecto");

            return ValidationProblem();
        }

        private async Task<RespuestaAutenticacionDTO> ConstruirToken(CredencialesUsuarioDTO credencialesUsuarioDTO)
        {
            var claims = new List<Claim>
            {
                new Claim("email", credencialesUsuarioDTO.Email),
                new Claim("lo que yo quiera", "cualquier valor")
            };

            var usuario = await userManager.FindByEmailAsync(credencialesUsuarioDTO.Email);
            var claimsDB = await userManager.GetClaimsAsync(usuario!);

            claims.AddRange(claimsDB);

            var llave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["llavejwt"]!));
            var credenciales = new SigningCredentials(llave, SecurityAlgorithms.HmacSha256);

            var expiracion = DateTime.UtcNow.AddYears(1);

            var tokenDeSeguridad = new JwtSecurityToken(issuer: null, audience: null, claims: claims, expires: expiracion, signingCredentials: credenciales);

            var token = new JwtSecurityTokenHandler().WriteToken(tokenDeSeguridad);

            return new RespuestaAutenticacionDTO
            {
                Token = token,
                Expiracion = expiracion
            };
        }
    }
}