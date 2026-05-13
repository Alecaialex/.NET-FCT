using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NLog;
using Shiori.Core.DTOs;
using Shiori.Core.Entities;
using Shiori.Core.Interfaces;
using Shiori.Infra.Data;
using System.Security.Claims;

namespace Shiori.Infra.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IHttpContextAccessor _contextAccessor;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public UserRepository(AppDbContext context, UserManager<User> userManager, IHttpContextAccessor contextAccessor)
        {
            _context = context;
            _userManager = userManager;
            _contextAccessor = contextAccessor;
        }

        // Crear un nuevo usuario
        public async Task<bool> CreateUserAsync(User user)
        {
            try
            {
                var result = await _userManager.CreateAsync(user);

                if (result.Succeeded)
                {
                    logger.Info("Usuario {0} creado exitosamente.", user.Email);
                }
                else
                {
                    logger.Warn("Fallo al crear usuario {0}. Errores: {1}",
                        user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                return result.Succeeded;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error en BD al crear usuario {0}", user.Email);
                return false;
            }
        }

        // Actualizar los datos de un usuario
        public async Task<bool> UpdateUserAsync(UpdateUserDto updateUserDto)
        {
            try
            {
                var existingUser = await _userManager.FindByEmailAsync(updateUserDto.Email);

                if (existingUser == null)
                {
                    logger.Warn("No se pudo actualizar: Usuario con email {0} no encontrado", updateUserDto.Email);
                    return false;
                }

                existingUser.Role = updateUserDto.Role;

                var result = await _userManager.UpdateAsync(existingUser);

                if (result.Succeeded)
                {
                    logger.Info("Rol de {0} actualizado correctamente a {1} en BD", updateUserDto.Email, updateUserDto.Role);
                    return true;
                }
                else
                {

                    logger.Warn("Fallo al actualizar el usuario {0}. Errores: {1}",
                        updateUserDto.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error en BD al intentar actualizar el rol de {0}", updateUserDto.Email);
                return false;
            }
        }

        // Obtener un usuario por su email
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            try
            {
                return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error en BD al buscar usuario por email {0}", email);
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
                    logger.Warn("Intento de eliminar usuario inexistente: {0}", email);
                    return false;
                }

                logger.Info("Eliminando usuario: {Email}", email);
                var result = await _userManager.DeleteAsync(user);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error al eliminar el usuario {0}", email);
                return false;
            }
        }

        // Obtener el usuario actual
        public async Task<User?> GetCurrentUser()
        {
            try
            {
                var emailClaim = _contextAccessor.HttpContext?.User.Claims
                    .FirstOrDefault(c => c.Type == "email" || c.Type == ClaimTypes.Email);

                if (emailClaim == null)
                    return null;

                var user = await _userManager.FindByEmailAsync(emailClaim.Value);

                if (user == null)
                    logger.Warn("El email del token ({0}) no corresponde a ningún usuario en la BD", emailClaim.Value);

                return user;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error al intentar recuperar el usuario");
                return null;
            }
        }
    }
}