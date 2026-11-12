namespace JuegardoApi.Models;

// A saved game: the same data the game used to write to partida.json.
public class Partida
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public int Nivel { get; set; }
    public int Puntos { get; set; }
    public int Monedas { get; set; }
    public int Vidas { get; set; }
    public DateTime FechaGuardado { get; set; }
}
