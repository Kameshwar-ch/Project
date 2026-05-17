using System;
using Skillforge.Domain;

namespace Skillforge.Service;

public interface IJWTProviderService
{
    string GenerateJwtToken(User user);
}
