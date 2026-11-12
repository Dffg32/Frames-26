namespace JuegardoApi.Models;

// A finished game's score, shown in the ranking.
public class Puntuacion
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public int Puntos { get; set; }
    public int Nivel { get; set; }
    public DateTime Fecha { get; set; }
}
