using System.ComponentModel.DataAnnotations;

namespace JuegardoApi.Dtos;

// Body of POST /api/auth/registro and POST /api/auth/login.
public class CredencialesDto
{
    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [StringLength(20, MinimumLength = 3, ErrorMessage = "El nombre de usuario debe tener entre 3 y 20 caracteres.")]
    [RegularExpression("^[a-zA-Z0-9_]+$", ErrorMessage = "El nombre de usuario solo puede tener letras, números y guiones bajos.")]
    public string NombreUsuario { get; set; } = "";

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(64, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 64 caracteres.")]
    public string Contrasena { get; set; } = "";
}

public record SesionDto(string Token, string NombreUsuario, DateTime Expira);

public record PerfilDto(string NombreUsuario, DateTime FechaRegistro, int PartidasGuardadas, int MejorPuntuacion, int VecesEnRanking);
