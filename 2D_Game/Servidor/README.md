# Servidor de Juegardo (API REST)

Servidor del juego hecho con **ASP.NET Core 8** (framework de servidor de .NET) y
**Entity Framework Core** con una base de datos **SQLite**. El juego de Unity lo usa para
las cuentas de usuario, guardar y cargar partidas y el ranking de puntuaciones.

## Cómo arrancarlo

Doble clic en `iniciar-servidor.bat`, o desde esta carpeta:

```bash
dotnet run --launch-profile http
```

El servidor queda escuchando en `http://localhost:5080`. Al arrancar crea la base de datos
`juegardo.db` (si no existe) y aplica las migraciones pendientes.

> Si no tienes el SDK de .NET instalado, `iniciar-servidor.bat` usa el que trae Unity
> (`C:\Program Files\Unity\Hub\Editor\<versión>\Editor\Data\DotNetSdk\dotnet.exe`).

## Estructura del proyecto

```
Servidor/
├── JuegardoApi.csproj        Proyecto y paquetes NuGet (JwtBearer, EF Core SQLite, EF Core Design)
├── Program.cs                Arranque: registro de servicios y pipeline de middleware
├── appsettings.json          Configuración: cadena de conexión, JWT, CORS, logs
├── appsettings.Development.json  Valores solo para desarrollo (clave JWT de pruebas)
├── Properties/
│   └── launchSettings.json   Perfiles de arranque (puertos 5080 http / 7080 https)
├── Controllers/              Controladores: definen las rutas de la API
│   ├── AuthController.cs         /api/auth          registro, login y perfil
│   ├── PartidasController.cs     /api/partidas      CRUD de partidas guardadas
│   └── PuntuacionesController.cs /api/puntuaciones  ranking
├── Models/                   Entidades de la base de datos (Usuario, Partida, Puntuacion)
├── Dtos/                     Datos que entran y salen de la API, con sus validaciones
├── Data/
│   └── JuegardoDbContext.cs  Contexto de EF Core: tablas, índices y relaciones
├── Migrations/               Migraciones generadas con dotnet-ef
├── Middleware/               Middleware propio (errores, logs, cabeceras de seguridad)
├── Services/                 Creación de tokens JWT y opciones
├── .config/dotnet-tools.json Herramienta local dotnet-ef (para las migraciones)
└── iniciar-servidor.bat      Arranca el servidor
```

## Rutas de la API

Todas las respuestas son JSON. Los errores siempre tienen la forma `{ "mensaje": "..." }`.
Las rutas marcadas con 🔒 necesitan la cabecera `Authorization: Bearer <token>`.

| Método | Ruta | Qué hace | Respuestas |
|---|---|---|---|
| POST | `/api/auth/registro` | Crea una cuenta y devuelve un token | 201, 400, 409, 429 |
| POST | `/api/auth/login` | Inicia sesión y devuelve un token | 200, 400, 401, 429 |
| GET 🔒 | `/api/auth/yo` | Perfil: partidas guardadas, mejor puntuación… | 200, 401 |
| GET 🔒 | `/api/partidas` | Lista tus partidas (la más reciente primero) | 200, 401 |
| GET 🔒 | `/api/partidas/{id}` | Una partida tuya | 200, 401, 404 |
| POST 🔒 | `/api/partidas` | Guarda una partida nueva (máximo 3) | 201, 400, 401, 409 |
| PUT 🔒 | `/api/partidas/{id}` | Sobrescribe una partida tuya | 204, 400, 401, 404 |
| DELETE 🔒 | `/api/partidas/{id}` | Borra una partida tuya | 204, 401, 404 |
| GET | `/api/puntuaciones?top=10` | Ranking de las mejores puntuaciones | 200, 400 |
| POST 🔒 | `/api/puntuaciones` | Añade una puntuación al ranking | 201, 400, 401 |

Ejemplo:

```bash
curl -X POST http://localhost:5080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"nombreUsuario":"daniel","contrasena":"secreta123"}'

curl http://localhost:5080/api/partidas -H "Authorization: Bearer <token>"
```

Cuerpo de una partida: `{ "nivel": 1, "puntos": 1250, "monedas": 12, "vidas": 2 }`

## Middleware

Se ejecutan en este orden para cada petición (`Program.cs`):

1. `ErrorHandlingMiddleware` (propio): captura cualquier excepción y devuelve un 500 genérico sin detalles internos.
2. `RequestLoggingMiddleware` (propio): escribe en el log método, ruta, código de respuesta y tiempo. No registra cuerpos, así que las contraseñas y los tokens no quedan en el log.
3. `SecurityHeadersMiddleware` (propio): añade cabeceras de seguridad (`X-Content-Type-Options`, `X-Frame-Options`, `Content-Security-Policy`, `Cache-Control: no-store`…).
4. HSTS y redirección a HTTPS (solo fuera de desarrollo).
5. CORS: solo los orígenes de `Cors:OrigenesPermitidos` pueden llamar a la API desde un navegador.
6. Rate limiting: máximo 5 intentos de login o registro por minuto e IP.
7. Autenticación JWT y autorización (`[Authorize]`).

## Base de datos y migraciones

Tablas: `Usuarios`, `Partidas` y `Puntuaciones`. Cada partida y puntuación pertenece a un usuario,
y se borran en cascada si se borra el usuario.

Migraciones (carpeta `Migrations/`):

1. `CrearUsuariosYPartidas`: crea las tablas `Usuarios` y `Partidas`.
2. `AnadirPuntuaciones`: añade la tabla `Puntuaciones` para el ranking.

Comandos (desde esta carpeta):

```bash
dotnet tool restore                          # instala dotnet-ef (herramienta local)
dotnet ef migrations add NombreDelCambio     # crea una migración tras cambiar los modelos
dotnet ef database update                    # aplica las migraciones (también se hace al arrancar)
```

## Seguridad

- **Contraseñas**: nunca se guardan en texto plano, solo un hash PBKDF2 con sal (`PasswordHasher`).
- **Autenticación**: tokens JWT firmados con HMAC-SHA256 que caducan a las 2 horas. Se comprueban emisor, audiencia, firma y caducidad.
- **Autorización**: cada usuario solo ve y modifica sus propias partidas. Pedir la partida de otro devuelve 404.
- **Validación**: todos los datos de entrada se validan (longitudes, caracteres permitidos y rangos de nivel, puntos, monedas y vidas).
- **Sin pistas a atacantes**: el login responde lo mismo si el usuario no existe o si la contraseña es incorrecta.
- **Fuerza bruta**: rate limiting en login y registro (5 intentos por minuto e IP).
- **Inyección SQL**: todas las consultas pasan por EF Core, que usa parámetros.
- **Errores**: los fallos internos se registran en el log pero al cliente solo le llega un mensaje genérico.
- **Clave JWT**: el servidor no arranca sin una clave de al menos 32 caracteres. La de `appsettings.Development.json` es solo para pruebas; en producción se define con la variable de entorno `Jwt__Clave`.
- **HTTPS**: fuera de desarrollo se activan HSTS y la redirección a HTTPS. En local el juego usa HTTP en `localhost`.
