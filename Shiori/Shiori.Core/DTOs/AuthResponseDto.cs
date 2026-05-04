using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Shiori.Core.DTOs
{
    // DTO para devolver en login/registro con token y expiración
    public class AuthResponseDto
    {
        public required string Token { get; set; }
        public DateTime Expiration { get; set; }
    }
}
