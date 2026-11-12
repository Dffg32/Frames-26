namespace JuegardoApi.Services;

// The "Jwt" section of appsettings.json.
public class JwtOptions
{
    // Secret used to sign tokens. At least 32 characters. In production it must come from the
    // environment variable Jwt__Clave, never from a file in the repository.
    public string Clave { get; set; } = "";
    public string Emisor { get; set; } = "";
    public string Audiencia { get; set; } = "";
    public int MinutosValidez { get; set; } = 120;
}
