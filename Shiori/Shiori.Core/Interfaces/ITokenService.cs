using Shiori.Core.DTOs;
using Shiori.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shiori.Core.Interfaces
{
    public interface ITokenService
    {
        AuthResponseDto CreateToken(User user);
    }
}
