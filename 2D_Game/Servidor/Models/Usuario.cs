namespace JuegardoApi.Models;

// A player account. The password is never stored, only its hash.
public class Usuario
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = "";
    public string ContrasenaHash { get; set; } = "";
    public DateTime FechaRegistro { get; set; }

    public List<Partida> Partidas { get; set; } = new();
    public List<Puntuacion> Puntuaciones { get; set; } = new();
}
