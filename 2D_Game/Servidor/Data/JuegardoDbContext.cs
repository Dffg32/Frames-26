using JuegardoApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace JuegardoApi.Data;

// Entity Framework Core database (SQLite). Changes to the model are applied with migrations:
//   dotnet ef migrations add <Nombre>
// and the server applies pending migrations when it starts.
public class JuegardoDbContext(DbContextOptions<JuegardoDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Partida> Partidas => Set<Partida>();
    public DbSet<Puntuacion> Puntuaciones => Set<Puntuacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(usuario =>
        {
            // NOCASE: "Daniel" and "daniel" are the same user.
            usuario.Property(u => u.NombreUsuario).HasMaxLength(20).UseCollation("NOCASE");
            usuario.HasIndex(u => u.NombreUsuario).IsUnique();
            usuario.Property(u => u.ContrasenaHash).HasMaxLength(200);
        });

        modelBuilder.Entity<Partida>(partida =>
        {
            partida.HasOne(p => p.Usuario)
                .WithMany(u => u.Partidas)
                .HasForeignKey(p => p.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
            partida.HasIndex(p => new { p.UsuarioId, p.FechaGuardado });
        });

        modelBuilder.Entity<Puntuacion>(puntuacion =>
        {
            puntuacion.HasOne(p => p.Usuario)
                .WithMany(u => u.Puntuaciones)
                .HasForeignKey(p => p.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
            // The ranking sorts by points, so index them.
            puntuacion.HasIndex(p => p.Puntos);
        });

        // SQLite has no time zones: dates are saved in UTC, so mark them as UTC when read back.
        var utc = new ValueConverter<DateTime, DateTime>(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(DateTime)))
            property.SetValueConverter(utc);
    }
}
