using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Shiori.Core.DTOs
{
    // DTO para las credenciales del usuario al registrar o iniciar sesión
    public class UserCredentialsDto
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }
        [Required]
        public string? Password { get; set; }
    }
}
