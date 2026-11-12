using System.Text;
using System.Threading.RateLimiting;
using JuegardoApi.Data;
using JuegardoApi.Dtos;
using JuegardoApi.Middleware;
using JuegardoApi.Models;
using JuegardoApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---------- Services ----------

// SQLite database managed with Entity Framework Core (see Data/ and Migrations/).
builder.Services.AddDbContext<JuegardoDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Juegardo")));

// JWT: refuse to start with a missing or weak signing key.
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (Encoding.UTF8.GetByteCount(jwt.Clave) < 32)
    throw new InvalidOperationException("Falta la clave JWT (Jwt:Clave) o tiene menos de 32 caracteres. Defínela con la variable de entorno Jwt__Clave.");
builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton<TokenService>();

// Passwords are stored as salted PBKDF2 hashes.
builder.Services.AddSingleton<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Emisor,
            ValidateAudience = true,
            ValidAudience = jwt.Audiencia,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Clave)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new ErrorDto("Tienes que iniciar sesión."));
            },
        };
    });
builder.Services.AddAuthorization();

// At most 5 login/registration attempts per minute from the same IP, against password guessing.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.OnRejected = (context, token) => new ValueTask(
        context.HttpContext.Response.WriteAsJsonAsync(new ErrorDto("Demasiados intentos. Espera un minuto."), token));
});

// Only the listed web origins may call the API from a browser (e.g. a WebGL build of the game).
var origenes = builder.Configuration.GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("juego", policy =>
    policy.WithOrigins(origenes).WithHeaders("Content-Type", "Authorization").WithMethods("GET", "POST", "PUT", "DELETE")));

builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    // Validation errors use the same { "mensaje": "..." } shape as every other error.
    options.InvalidModelStateResponseFactory = context =>
    {
        string mensaje = context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m)) ?? "Datos no válidos.";
        return new BadRequestObjectResult(new ErrorDto(mensaje));
    };
});

var app = builder.Build();

// Create the database or bring it up to date with any pending migrations.
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<JuegardoDbContext>().Database.Migrate();

// ---------- Middleware pipeline (order matters) ----------

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseCors("juego");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
