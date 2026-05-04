using Microsoft.AspNetCore.Identity;
using Shiori.Core.Enums;

namespace Shiori.Core.Entities
{
    public class User : IdentityUser<Guid>
    {
        // Propiedad adicional para el rol del usuario (Admin/User)
        public UserRole Role { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
