using System.ComponentModel.DataAnnotations;
using JuegardoApi.Data;
using JuegardoApi.Dtos;
using JuegardoApi.Models;
using JuegardoApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JuegardoApi.Controllers;

// The ranking: anyone can read it, only logged-in players can add scores.
[ApiController]
[Route("api/puntuaciones")]
public class PuntuacionesController(JuegardoDbContext db) : ControllerBase
{
    // GET /api/puntuaciones?top=10
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<PuntuacionDto>>> Ranking(
        [FromQuery, Range(1, 50, ErrorMessage = "top debe estar entre 1 y 50.")] int top = 10)
    {
        // Join with Usuarios to get each player's name, best scores first.
        var mejores = await db.Puntuaciones
            .OrderByDescending(p => p.Puntos)
            .ThenBy(p => p.Fecha)
            .Take(top)
            .Select(p => new { p.Usuario!.NombreUsuario, p.Puntos, p.Nivel, p.Fecha })
            .ToListAsync();
        return mejores
            .Select((p, i) => new PuntuacionDto(i + 1, p.NombreUsuario, p.Puntos, p.Nivel, p.Fecha))
            .ToList();
    }

    // POST /api/puntuaciones
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<PuntuacionDto>> Registrar(PuntuacionEntradaDto datos)
    {
        var puntuacion = new Puntuacion
        {
            UsuarioId = User.UsuarioId(),
            Puntos = datos.Puntos,
            Nivel = datos.Nivel,
            Fecha = DateTime.UtcNow,
        };
        db.Puntuaciones.Add(puntuacion);
        await db.SaveChangesAsync();

        // Position = how many scores are strictly better, plus one.
        int posicion = await db.Puntuaciones.CountAsync(p => p.Puntos > puntuacion.Puntos) + 1;
        string nombre = User.Identity?.Name ?? "";
        return Created("/api/puntuaciones", new PuntuacionDto(posicion, nombre, puntuacion.Puntos, puntuacion.Nivel, puntuacion.Fecha));
    }
}
