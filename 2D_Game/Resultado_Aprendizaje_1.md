# Resultado de Aprendizaje 1: requisitos cumplidos

Proyecto: **Juegardo**, un juego de plataformas 2D hecho en Unity con un servidor propio hecho
en **ASP.NET Core 8**, el framework de servidor de .NET, con base de datos **SQLite**
gestionada con **Entity Framework Core**. El juego usa el servidor para las cuentas de usuario,
para guardar y cargar partidas y para el ranking de puntuaciones.

- Juego: `Assets/`
- Servidor: `Servidor/`

## Tabla de evaluación

| Criterio de evaluación | Peso | Centro de estudios | Formación en empresa | ¿Se cumple? |
|---|:---:|---|---|:---:|
| **1.a)** Conoce la estructura base de un proyecto creado con un framework servidor | 25% | 15% Entrega de tarea en grupo | 10% Documentación | ✅ |
| **1.b)** Se han creado nuevas rutas y controladores en el proyecto | 20% | Entrega de tarea en grupo | - | ✅ |
| **1.c)** Se han utilizado distintos elementos middleware | 5% | Entrega de tarea en grupo | - | ✅ |
| **1.d)** Se han realizado migraciones de bases de datos utilizando el framework | 5% | Entrega de tarea en grupo | - | ✅ |
| **1.e)** Se han realizado consultas a bases de datos | 20% | Entrega de tarea en grupo | - | ✅ |
| **1.f)** Se ha implementado una API Restful | 5% | Entrega de tarea en grupo | - | ✅ |
| **1.g)** Se han tomado medidas de securización de la aplicación | 20% | Entrega de tarea en grupo | - | ✅ |
| **Total** | **100%** | | | **7 de 7** |

## Cómo se cumple cada criterio

### 1.a) Estructura base de un proyecto con framework servidor (25%)

El servidor es un proyecto ASP.NET Core con la estructura estándar del framework:

| Archivo / carpeta | Para qué sirve |
|---|---|
| [`JuegardoApi.csproj`](Servidor/JuegardoApi.csproj) | Proyecto y dependencias (paquetes NuGet) |
| [`Program.cs`](Servidor/Program.cs) | Punto de entrada: registra los servicios y monta el pipeline de middleware |
| [`appsettings.json`](Servidor/appsettings.json) | Configuración: base de datos, JWT, CORS y logs |
| [`appsettings.Development.json`](Servidor/appsettings.Development.json) | Configuración solo para desarrollo |
| [`Properties/launchSettings.json`](Servidor/Properties/launchSettings.json) | Perfiles de arranque y puertos |
| `Controllers/` | Controladores con las rutas de la API |
| `Models/` | Entidades de la base de datos |
| `Dtos/` | Datos de entrada y salida de la API, con validaciones |
| `Data/` | Contexto de la base de datos |
| `Migrations/` | Migraciones de la base de datos |
| `Middleware/` | Middleware propio |
| `Services/` | Servicios (creación de tokens JWT) |

**Documentación:** [`Servidor/README.md`](Servidor/README.md) explica la estructura carpeta por carpeta, cómo arrancar el servidor, las rutas, el middleware, las migraciones y la seguridad.

### 1.b) Rutas y controladores (20%)

Tres controladores en `Servidor/Controllers/`, con las rutas definidas mediante atributos (`[Route]`, `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]`):

| Controlador | Rutas |
|---|---|
| [`AuthController.cs`](Servidor/Controllers/AuthController.cs) | `POST /api/auth/registro`, `POST /api/auth/login`, `GET /api/auth/yo` |
| [`PartidasController.cs`](Servidor/Controllers/PartidasController.cs) | `GET /api/partidas`, `GET /api/partidas/{id}`, `POST /api/partidas`, `PUT /api/partidas/{id}`, `DELETE /api/partidas/{id}` |
| [`PuntuacionesController.cs`](Servidor/Controllers/PuntuacionesController.cs) | `GET /api/puntuaciones?top=10`, `POST /api/puntuaciones` |

### 1.c) Elementos middleware (5%)

**Middleware propio**, en `Servidor/Middleware/`:

- [`ErrorHandlingMiddleware.cs`](Servidor/Middleware/ErrorHandlingMiddleware.cs): captura cualquier error y devuelve un mensaje genérico sin detalles internos.
- [`RequestLoggingMiddleware.cs`](Servidor/Middleware/RequestLoggingMiddleware.cs): registra cada petición con su método, ruta, código de respuesta y tiempo.
- [`SecurityHeadersMiddleware.cs`](Servidor/Middleware/SecurityHeadersMiddleware.cs): añade cabeceras de seguridad a todas las respuestas.

**Middleware del framework**, configurado en [`Program.cs`](Servidor/Program.cs): CORS, rate limiting (límite de intentos), autenticación JWT, autorización, HSTS y redirección a HTTPS.

### 1.d) Migraciones de bases de datos (5%)

Migraciones generadas con la herramienta del framework (`dotnet ef migrations add`), en `Servidor/Migrations/`:

1. **`CrearUsuariosYPartidas`**: crea las tablas `Usuarios` y `Partidas`.
2. **`AnadirPuntuaciones`**: añade la tabla `Puntuaciones` para el ranking.

- **Modelo:** [`Data/JuegardoDbContext.cs`](Servidor/Data/JuegardoDbContext.cs) define las tablas, relaciones e índices, y `Models/` las entidades.
- **Aplicación:** el servidor aplica las migraciones pendientes al arrancar (`Database.Migrate()` en `Program.cs`).
- **Herramienta:** `dotnet-ef` está instalada como herramienta local en `Servidor/.config/dotnet-tools.json`.

### 1.e) Consultas a bases de datos (20%)

Consultas hechas con Entity Framework Core (LINQ) desde los controladores:

| Tipo de consulta | Dónde |
|---|---|
| Filtrar y ordenar (partidas del usuario, de la más reciente a la más antigua) | `PartidasController.Listar` |
| Buscar un registro (partida por id y usuario, usuario por nombre) | `PartidasController.BuscarPropia`, `AuthController.Login` |
| Comprobar si existe (nombre de usuario repetido) | `AuthController.Registrar` |
| Contar (límite de 3 partidas por usuario, posición en el ranking) | `PartidasController.Crear`, `PuntuacionesController.Registrar` |
| Agregados `Count` y `Max` (perfil: partidas guardadas y mejor puntuación) | `AuthController.Perfil` |
| Join con otra tabla, ordenar y limitar (ranking con el nombre de cada jugador) | `PuntuacionesController.Ranking` |
| Insertar, actualizar y borrar | `AuthController`, `PartidasController`, `PuntuacionesController` |

### 1.f) API Restful (5%)

- **Recursos con los verbos HTTP correspondientes:** `GET` para leer, `POST` para crear, `PUT` para modificar y `DELETE` para borrar.
- **Peticiones y respuestas en JSON.** Los errores siempre tienen la forma `{ "mensaje": "..." }`.
- **Códigos de estado correctos:**

  | Código | Cuándo |
  |---|---|
  | `200 OK` | Lectura correcta |
  | `201 Created` | Recurso creado, con la cabecera `Location` |
  | `204 No Content` | Actualización o borrado correcto |
  | `400 Bad Request` | Datos no válidos |
  | `401 Unauthorized` | Sin sesión o token no válido |
  | `404 Not Found` | El recurso no existe o no es tuyo |
  | `409 Conflict` | Usuario repetido o límite de partidas |
  | `429 Too Many Requests` | Demasiados intentos |

- **Cliente:** el juego consume la API desde [`Assets/Scripts/ApiClient.cs`](Assets/Scripts/ApiClient.cs).

### 1.g) Securización de la aplicación (20%)

| Medida | Dónde |
|---|---|
| Contraseñas guardadas como hash PBKDF2 con sal, nunca en texto plano | `AuthController.cs` (`PasswordHasher`) |
| Autenticación con tokens JWT firmados (HMAC-SHA256) que caducan a las 2 horas | [`Services/TokenService.cs`](Servidor/Services/TokenService.cs), `Program.cs` |
| Rutas protegidas: hay que iniciar sesión para guardar o puntuar | `[Authorize]` en los controladores |
| Cada usuario solo puede ver y modificar sus propias partidas | `PartidasController.cs` (filtra por el usuario del token) |
| Validación de todos los datos de entrada (longitudes, caracteres, rangos) | `Servidor/Dtos/` |
| Límite de 5 intentos de login o registro por minuto (contra fuerza bruta) | Rate limiting en `Program.cs` |
| El login da el mismo mensaje si falla el usuario o la contraseña | `AuthController.Login` |
| Los errores internos no se muestran al cliente | `ErrorHandlingMiddleware.cs` |
| Cabeceras de seguridad (`nosniff`, `X-Frame-Options`, CSP, `no-store`) | `SecurityHeadersMiddleware.cs` |
| CORS restringido a los orígenes configurados | `Program.cs`, `appsettings.json` |
| El servidor no arranca sin una clave JWT de al menos 32 caracteres | `Program.cs` |
| HTTPS y HSTS fuera del entorno de desarrollo | `Program.cs` |
| Consultas parametrizadas (protección contra inyección SQL) | Entity Framework Core |
| No se registran contraseñas ni tokens en los logs | `RequestLoggingMiddleware.cs` |

## Comprobación

El servidor se arrancó y se probó con peticiones reales:

- **Lo que funciona:** registro, login y perfil; crear, leer, actualizar y borrar partidas; y el ranking.
- **Los errores que responde correctamente:**
  - `400` con datos no válidos;
  - `401` sin sesión o con token falso;
  - `404` al pedir la partida de otro usuario;
  - `409` con un usuario repetido o al pasar de 3 partidas;
  - `429` al superar el límite de intentos.
- **También se comprobó:** las cabeceras de seguridad y el log de peticiones.

Para probarlo: arrancar `Servidor/iniciar-servidor.bat`, abrir el juego y entrar en **GUARDADO**
para registrarse, guardar y cargar, o en **RANKING** para ver las puntuaciones.
