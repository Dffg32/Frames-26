using JuegardoApi.Data;
using JuegardoApi.Dtos;
using JuegardoApi.Models;
using JuegardoApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace JuegardoApi.Controllers;

// Accounts: register, log in and see your own profile.
[ApiController]
[Route("api/auth")]
public class AuthController(JuegardoDbContext db, IPasswordHasher<Usuario> hasher, TokenService tokens) : ControllerBase
{
    // POST /api/auth/registro
    [HttpPost("registro")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<SesionDto>> Registrar(CredencialesDto datos)
    {
        if (await db.Usuarios.AnyAsync(u => u.NombreUsuario == datos.NombreUsuario))
            return Conflict(new ErrorDto("Ese nombre de usuario ya existe."));

        var usuario = new Usuario { NombreUsuario = datos.NombreUsuario, FechaRegistro = DateTime.UtcNow };
        usuario.ContrasenaHash = hasher.HashPassword(usuario, datos.Contrasena);
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Perfil), null, CrearSesion(usuario));
    }

    // POST /api/auth/login
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<SesionDto>> Login(CredencialesDto datos)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == datos.NombreUsuario);
        var resultado = usuario == null
            ? PasswordVerificationResult.Failed
            : hasher.VerifyHashedPassword(usuario, usuario.ContrasenaHash, datos.Contrasena);
        // Same message whether the user or the password is wrong, so accounts can't be discovered.
        if (usuario == null || resultado == PasswordVerificationResult.Failed)
            return Unauthorized(new ErrorDto("Usuario o contraseña incorrectos."));

        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
        {
            usuario.ContrasenaHash = hasher.HashPassword(usuario, datos.Contrasena);
            await db.SaveChangesAsync();
        }
        return Ok(CrearSesion(usuario));
    }

    // GET /api/auth/yo
    [Authorize]
    [HttpGet("yo")]
    public async Task<ActionResult<PerfilDto>> Perfil()
    {
        int id = User.UsuarioId();
        var perfil = await db.Usuarios
            .Where(u => u.Id == id)
            .Select(u => new PerfilDto(
                u.NombreUsuario,
                u.FechaRegistro,
                u.Partidas.Count,
                u.Puntuaciones.Max(p => (int?)p.Puntos) ?? 0,
                u.Puntuaciones.Count))
            .FirstOrDefaultAsync();
        return perfil == null ? NotFound(new ErrorDto("El usuario ya no existe.")) : Ok(perfil);
    }

    SesionDto CrearSesion(Usuario usuario)
    {
        var (token, expira) = tokens.Crear(usuario);
        return new SesionDto(token, usuario.NombreUsuario, expira);
    }
}
