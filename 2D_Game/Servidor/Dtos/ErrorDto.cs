namespace JuegardoApi.Dtos;

// Every error the API returns has this shape: { "mensaje": "..." }
public record ErrorDto(string Mensaje);
