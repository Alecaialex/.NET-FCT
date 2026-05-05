using Shiori.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Shiori.Core.DTOs
{
    // DTO para actualización de usuario
    public class UpdateUserDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required]
        [Range(0,1, ErrorMessage = "El rol debe ser 0 (User) o 1 (Admin)")]
        public UserRole Role { get; set; }
    }
}
