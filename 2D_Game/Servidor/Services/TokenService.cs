using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JuegardoApi.Models;
using Microsoft.IdentityModel.Tokens;

namespace JuegardoApi.Services;

// Creates the signed JWT the game sends in the Authorization header after logging in.
public class TokenService(JwtOptions options)
{
    public (string Token, DateTime Expira) Crear(Usuario usuario)
    {
        var expira = DateTime.UtcNow.AddMinutes(options.MinutosValidez);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, usuario.NombreUsuario),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Clave)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(options.Emisor, options.Audiencia, claims, expires: expira, signingCredentials: credenciales);
        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }
}
