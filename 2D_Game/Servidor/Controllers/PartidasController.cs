using JuegardoApi.Data;
using JuegardoApi.Dtos;
using JuegardoApi.Models;
using JuegardoApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JuegardoApi.Controllers;

// Saved games of the logged-in user (full CRUD).
// Every query filters by the user in the token, so nobody can read or change someone else's games:
// another user's id simply answers 404.
[ApiController]
[Route("api/partidas")]
[Authorize]
public class PartidasController(JuegardoDbContext db) : ControllerBase
{
    const int MaxPartidasPorUsuario = 3;

    // GET /api/partidas  (most recent first)
    [HttpGet]
    public async Task<ActionResult<List<PartidaDto>>> Listar()
    {
        int usuarioId = User.UsuarioId();
        return await db.Partidas
            .Where(p => p.UsuarioId == usuarioId)
            .OrderByDescending(p => p.FechaGuardado)
            .Select(p => new PartidaDto(p.Id, p.Nivel, p.Puntos, p.Monedas, p.Vidas, p.FechaGuardado))
            .ToListAsync();
    }

    // GET /api/partidas/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PartidaDto>> Obtener(int id)
    {
        var partida = await BuscarPropia(id);
        return partida == null ? NotFound(new ErrorDto("No existe esa partida.")) : Ok(ADto(partida));
    }

    // POST /api/partidas
    [HttpPost]
    public async Task<ActionResult<PartidaDto>> Crear(PartidaEntradaDto datos)
    {
        int usuarioId = User.UsuarioId();
        if (await db.Partidas.CountAsync(p => p.UsuarioId == usuarioId) >= MaxPartidasPorUsuario)
            return Conflict(new ErrorDto($"Solo puedes tener {MaxPartidasPorUsuario} partidas guardadas."));

        var partida = new Partida { UsuarioId = usuarioId };
        Copiar(datos, partida);
        db.Partidas.Add(partida);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Obtener), new { id = partida.Id }, ADto(partida));
    }

    // PUT /api/partidas/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, PartidaEntradaDto datos)
    {
        var partida = await BuscarPropia(id);
        if (partida == null) return NotFound(new ErrorDto("No existe esa partida."));
        Copiar(datos, partida);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/partidas/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Borrar(int id)
    {
        var partida = await BuscarPropia(id);
        if (partida == null) return NotFound(new ErrorDto("No existe esa partida."));
        db.Partidas.Remove(partida);
        await db.SaveChangesAsync();
        return NoContent();
    }

    Task<Partida?> BuscarPropia(int id)
    {
        int usuarioId = User.UsuarioId();
        return db.Partidas.FirstOrDefaultAsync(p => p.Id == id && p.UsuarioId == usuarioId);
    }

    static void Copiar(PartidaEntradaDto datos, Partida partida)
    {
        partida.Nivel = datos.Nivel;
        partida.Puntos = datos.Puntos;
        partida.Monedas = datos.Monedas;
        partida.Vidas = datos.Vidas;
        partida.FechaGuardado = DateTime.UtcNow;
    }

    static PartidaDto ADto(Partida p) => new(p.Id, p.Nivel, p.Puntos, p.Monedas, p.Vidas, p.FechaGuardado);
}
