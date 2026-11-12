using System.ComponentModel.DataAnnotations;

namespace JuegardoApi.Dtos;

// Body of POST /api/partidas and PUT /api/partidas/{id}.
// The ranges stop obviously impossible values from being stored.
public class PartidaEntradaDto
{
    [Range(1, 99, ErrorMessage = "El nivel debe estar entre 1 y 99.")]
    public int Nivel { get; set; }

    [Range(0, 9_999_999, ErrorMessage = "Los puntos deben estar entre 0 y 9.999.999.")]
    public int Puntos { get; set; }

    [Range(0, 9_999, ErrorMessage = "Las monedas deben estar entre 0 y 9.999.")]
    public int Monedas { get; set; }

    [Range(0, 99, ErrorMessage = "Las vidas deben estar entre 0 y 99.")]
    public int Vidas { get; set; }
}

public record PartidaDto(int Id, int Nivel, int Puntos, int Monedas, int Vidas, DateTime FechaGuardado);
