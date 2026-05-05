using Shiori.Core.DTOs;
using Shiori.Core.Entities;
using System;
using System.Threading.Tasks;

namespace Shiori.Core.Interfaces
{
    public interface IUserRepository
    {

        // Obtener usuario por email
        Task<User?> GetUserByEmailAsync(string email);

        // Crear usuario
        Task<bool> CreateUserAsync(User user);

        // Actualizar usuario
        Task<bool> UpdateUserAsync(UpdateUserDto updateUserDto);

        // Obtener el usuario actual mediante el email en el token
        Task<User?> GetCurrentUser();

        //Eliminar un usuario (Admin)
        Task<bool> DeleteUserAsync(string email);
    }
}