using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace JuegardoApi.Services;

public static class ClaimsPrincipalExtensions
{
    // Id of the logged-in user, read from the validated JWT.
    public static int UsuarioId(this ClaimsPrincipal user)
    {
        string? id = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(id, out int value) ? value : throw new InvalidOperationException("El token no contiene el id del usuario.");
    }
}
