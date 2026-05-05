using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shiori.Core.DTOs;
using Shiori.Core.Entities;
using Shiori.Core.Interfaces;
using Shiori.Infra.Data;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Shiori.Infra.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(AppDbContext context, UserManager<User> userManager, IHttpContextAccessor contextAccessor, ILogger<UserRepository> logger)
        {
            _context = context;
            _userManager = userManager;
            _contextAccessor = contextAccessor;
            _logger = logger;
        }

        // Crear un nuevo usuario
        public async Task<bool> CreateUserAsync(User user)
        {
            try
            {
                _logger.LogInformation("Intentando crear nuevo usuario: {Email}", user.Email);
                var result = await _userManager.CreateAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Usuario {Email} creado exitosamente.", user.Email);
                }
                else
                {
                    _logger.LogWarning("Fallo al crear usuario {Email}. Errores: {Errors}",
                        user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                return result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DATABASE ERROR: Error crítico al crear usuario {Email}", user.Email);
                return false;
            }
        }

        // Actualizar los datos de un usuario (Rol)
        public async Task<bool> UpdateUserAsync(UpdateUserDto updateUserDto)
        {
            try
            {
                _logger.LogInformation("Iniciando cambio de rol para el usuario: {Email}", updateUserDto.Email);

                var existingUser = await _userManager.FindByEmailAsync(updateUserDto.Email);

                if (existingUser == null)
                {
                    _logger.LogWarning("No se pudo actualizar: Usuario con email {Email} no encontrado.", updateUserDto.Email);
                    return false;
                }

                _logger.LogInformation("Cambiando rol de {OldRole} a {NewRole} para {Email}.",
                    existingUser.Role, updateUserDto.Role, updateUserDto.Email);

                // Actualizar el rol
                existingUser.Role = updateUserDto.Role;

                var result = await _userManager.UpdateAsync(existingUser);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Rol de {Email} actualizado correctamente en la base de datos.", updateUserDto.Email);
                    return true;
                }
                else
                {

                    _logger.LogWarning("Fallo al actualizar la entidad User para {Email}. Errores: {Errors}",
                        updateUserDto.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DATABASE ERROR: Error crítico al intentar actualizar el rol de {Email}", updateUserDto.Email);
                return false;
            }
        }

        // Obtener un usuario por su email
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            try
            {
                _logger.LogDebug("Buscando usuario en BD por email: {Email}", email);
                return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DATABASE ERROR: Error al buscar usuario por email {Email}", email);
                return null;
            }
        }

        // Eliminar un usuario por su email (Admin)
        public async Task<bool> DeleteUserAsync(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("Intento de eliminar usuario inexistente: {Email}", email);
                    return false;
                }

                _logger.LogInformation("Eliminando usuario: {Email}", email);
                var result = await _userManager.DeleteAsync(user);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el usuario {Email}", email);
                return false;
            }
        }

        // Obtener el usuario actual
        public async Task<User?> GetCurrentUser()
        {
            try
            {
                _logger.LogDebug("Extrayendo usuario actual desde el HttpContext.");

                var emailClaim = _contextAccessor.HttpContext?.User.Claims
                    .FirstOrDefault(c => c.Type == "email" || c.Type == ClaimTypes.Email);

                if (emailClaim == null)
                {
                    _logger.LogWarning("No se encontró el claim de email en el token.");
                    return null;
                }

                _logger.LogDebug("Buscando datos completos del usuario para el email: {Email}", emailClaim.Value);
                var user = await _userManager.FindByEmailAsync(emailClaim.Value);

                if (user == null)
                    _logger.LogWarning("El email del token ({Email}) no corresponde a ningún usuario en la BD.", emailClaim.Value);

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SECURITY ERROR: Error al intentar recuperar el usuario.");
                return null;
            }
        }
    }
}