using Microsoft.AspNetCore.Identity;
using Shiori.Core.Enums;

namespace Shiori.Core.Entities
{
    public class User : IdentityUser
    {
        public UserRole Role { get; set; }
    }
}
