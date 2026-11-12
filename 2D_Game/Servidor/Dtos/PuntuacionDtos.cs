using System.ComponentModel.DataAnnotations;

namespace JuegardoApi.Dtos;

// Body of POST /api/puntuaciones.
public class PuntuacionEntradaDto
{
    [Range(0, 9_999_999, ErrorMessage = "Los puntos deben estar entre 0 y 9.999.999.")]
    public int Puntos { get; set; }

    [Range(1, 99, ErrorMessage = "El nivel debe estar entre 1 y 99.")]
    public int Nivel { get; set; }
}

public record PuntuacionDto(int Posicion, string NombreUsuario, int Puntos, int Nivel, DateTime Fecha);
