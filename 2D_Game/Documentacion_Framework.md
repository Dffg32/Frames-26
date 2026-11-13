# Documentación del proyecto: uso del framework (cliente)

**Proyecto:** Juegardo, un juego de plataformas 2D
**Fecha:** 13 de noviembre de 2026
**Framework del cliente:** Unity 6 (versión `6000.6.7f1`) con C#
**Servidor que consume:** API REST propia hecha con ASP.NET Core 8 (carpeta `Servidor/`)

Este documento explica cómo usa el proyecto el framework Unity en la parte cliente: el juego.
Recoge qué componentes tiene, cómo se crea uno nuevo, cómo cambia la interfaz según lo que
responde la API y cómo se gestionan la sesión y las llamadas a los servicios protegidos.

---

## 1. Componentes de la aplicación

### 1.1 Cómo organiza Unity una aplicación

En Unity todo lo que hay en el juego es un **GameObject**. Un GameObject no hace nada por sí
solo: su comportamiento lo dan los **componentes** que tiene añadidos. Hay dos tipos:

- **Componentes del framework:** `Transform` (posición), `SpriteRenderer` (dibujo), `Rigidbody2D`
  (física), `BoxCollider2D` / `CapsuleCollider2D` (colisiones), `Camera`...
- **Componentes propios:** clases de C# que heredan de `MonoBehaviour`. Unity llama
  automáticamente a sus métodos del ciclo de vida: `Awake`, `Start`, `Update`, `LateUpdate`,
  `OnGUI`, `OnTriggerEnter2D`, `OnCollisionEnter2D`...

En este proyecto **el nivel se construye por código**: la escena `Assets/Scenes/SampleScene.unity`
solo tiene la cámara y la luz 2D. Al pulsar Play, el componente `Game` crea todos los GameObjects
a partir de los mapas de texto de `Worlds.cs` y les añade sus componentes con `AddComponent<T>()`.
Así no hacen falta prefabs ni imágenes: los sprites también se generan por código (`PixelArt.cs`).

### 1.2 Componentes propios (`MonoBehaviour`)

Todos están en `Assets/Scripts/`.

| Componente | Qué hace |
|---|---|
| `Game.cs` | Componente principal. Arranca solo al pulsar Play, construye el mundo a partir de su mapa, mueve la cámara, dibuja el HUD (mundo, puntos, monedas, vidas y usuario conectado) y controla vidas, victoria, cambio de mundo y envío de la puntuación al ranking |
| `MainMenu.cs` | Menú principal con sus páginas JUGAR, GUARDADO (cuenta y partidas) y RANKING. Es la parte de la interfaz que más habla con la API |
| `PlayerController.cs` | Movimiento, salto, bolas de fuego, estados de poder (pequeño, grande, fuego, estrella) y detección de lo que toca el jugador |
| `Enemy.cs` | Enemigos: babosa, erizo, murciélago, rana, cactus y fantasma |
| `Boss.cs` | Jefe final con tres ataques |
| `BonusBlock.cs` | Bloque con premio (moneda, baya, pimiento o estrella), incluido el bloque oculto |
| `Brick.cs` | Bloque de piedra que se rompe si el jugador es grande |
| `Coin.cs` | Moneda que gira y se recoge |
| `PowerUp.cs` | Power-ups que salen de los bloques y se mueven por el nivel |
| `Fireball.cs` | Bola de fuego del jugador |
| `Projectile.cs` | Proyectiles de los enemigos y del jefe |
| `Ember.cs` | Chispa que salta de los pozos de lava |
| `Goal.cs` | Gema del final del nivel |
| `Hazard.cs` | Marca algo que hace daño al tocarlo y no se puede derrotar |
| `Lava.cs` | Superficie de lava (muerte instantánea) |

### 1.3 Clases de apoyo (no son componentes)

| Clase | Qué hace |
|---|---|
| `ApiClient.cs` | **Cliente de la API REST.** Gestiona la sesión (token JWT) y hace todas las peticiones al servidor |
| `SaveSystem.cs` | Guarda una copia local de la partida en `partida.json` (`SaveData`) |
| `Worlds.cs` | Mapas de texto de los cuatro mundos y su tema (pradera, desierto, cueva, castillo) |
| `PixelArt.cs` | Crea los sprites a partir de texto |
| `Mover.cs` | Colisiones con el mapa para objetos que se mueven solos |
| `TextDrawing.cs` | Estilo común de los textos del HUD y del menú |

### 1.4 Scripts de editor (`Assets/Scripts/Editor/`)

Solo se ejecutan dentro del editor de Unity y no entran en el juego compilado.

| Script | Qué hace |
|---|---|
| `LevelPreview.cs` | Muestra el mundo en la vista Scene sin pulsar Play (menú **Juegardo**) |
| `NetworkSettings.cs` | Permite HTTP solo en el editor y en las builds de desarrollo (ver apartado 4) |

### 1.5 Recursos del framework que se usan

| Recurso de Unity | Para qué |
|---|---|
| `MonoBehaviour` y su ciclo de vida | Todos los componentes del juego |
| `[RuntimeInitializeOnLoadMethod]` | Arrancar `Game` automáticamente al cargar la escena |
| Física 2D (`Rigidbody2D`, colliders, triggers) | Movimiento, colisiones y recogida de objetos |
| Input System (`Keyboard.current`) | Teclado y mando |
| IMGUI (`OnGUI`, `GUI.TextField`, `GUI.PasswordField`) | HUD y menús |
| Corrutinas (`StartCoroutine`, `IEnumerator`) | Peticiones a la API sin congelar el juego |
| `UnityWebRequest` | Llamadas HTTP a la API |
| `JsonUtility` | Convertir objetos a JSON y al revés |
| `Application.persistentDataPath` | Ruta del guardado local |
| `PlayerSettings` (desde el editor) | Configuración de la red (HTTP) |

### 1.6 Relación entre los componentes

```
Game  (se crea solo al pulsar Play)
 ├── MainMenu ───────────────► ApiClient ──HTTP/JSON──► Servidor ASP.NET Core
 ├── Level (GameObject)                 ▲
 │    ├── Player: PlayerController      │ SubmitScore al terminar la partida
 │    ├── Enemy, Boss, Coin, Goal...    │
 │    └── BonusBlock ─► PowerUp         │
 └── HUD (OnGUI) ───────────────────────┘ muestra el usuario conectado
```

---

## 2. Proceso de creación de un nuevo componente

Como el nivel se construye por código, crear un componente nuevo tiene dos partes:
escribir el `MonoBehaviour` y decirle a `Game` cuándo crearlo. Como ejemplo se usa un
**muelle** que lanza al jugador hacia arriba. Este ejemplo no está en el proyecto: solo sirve
para explicar el proceso.

### Paso 1: crear el script

En Unity: **Project → Assets/Scripts → clic derecho → Create → MonoBehaviour Script**, y se le
da el nombre `Spring`. **El nombre del archivo tiene que ser igual que el de la clase**, porque
si no Unity no puede añadirlo como componente.

```csharp
using UnityEngine;

// A spring: the player bounces high when landing on it.
public class Spring : MonoBehaviour
{
    public float Force = 22f;

    // Called by PlayerController when the player touches the spring.
    public void Launch(Rigidbody2D body)
    {
        body.linearVelocity = new Vector2(body.linearVelocity.x, Force);
    }
}
```

Los campos `public` aparecen en el Inspector de Unity y se pueden ajustar sin tocar el código.

### Paso 2: darle un aspecto

Añadir el sprite en `PixelArt.cs`, siguiendo el mismo formato que `Coin` o `Gem`: un dibujo
hecho con letras, donde cada letra es un color de la paleta.

### Paso 3: ponerlo en el mapa

1. Elegir un carácter libre para el mapa, por ejemplo `J`, y añadirlo a la leyenda de `Worlds.cs`.
2. En `Game.BuildLevel()`, añadir el caso al `switch` que recorre el mapa:

```csharp
case 'J': SpawnSpring(pos); break;
```

3. Crear el método que monta el GameObject con sus componentes, igual que `SpawnCoin` o `SpawnGoal`:

```csharp
void SpawnSpring(Vector2 pos)
{
    var go = Tile(pos, PixelArt.Spring, "Spring", 3);   // GameObject + SpriteRenderer
    var box = go.AddComponent<BoxCollider2D>();         // collider del framework
    box.isTrigger = true;
    go.AddComponent<Spring>();                          // nuestro componente
}
```

### Paso 4: conectarlo con los demás componentes

Los componentes se encuentran entre sí con `TryGetComponent`. En `PlayerController.OnTriggerEnter2D`
ya se hace así con las monedas, los power-ups y la gema, así que solo hay que añadir una línea:

```csharp
else if (other.TryGetComponent(out Spring spring)) spring.Launch(body);
```

### Paso 5: probarlo

Pulsar **Play**. Para ver el componente sin jugar, el menú **Juegardo** del editor muestra el mundo en la
vista Scene, gracias a `LevelPreview.cs`.

### Alternativa: añadirlo desde el editor

También se puede arrastrar el script a un GameObject de la escena o usar **Add Component** en el
Inspector. En este proyecto no se hace así porque los niveles se generan por código, pero el
script sería el mismo.

### Si el componente usa la API

Si el componente nuevo necesita datos del servidor, no hace la petición él mismo:

1. Se añade un método a `ApiClient` que llama a `Send(...)` con el verbo y la ruta, y devuelve
   el resultado por un callback.
2. El componente lo llama con `StartCoroutine(ApiClient.MiMetodo(..., (resultado, error) => { ... }))`
   y actualiza la interfaz en el callback (ver apartado 3).

---

## 3. Ejemplos de adaptación de la interfaz tras una petición a la API

### 3.1 Cómo funciona

Las peticiones las hace `ApiClient` con `UnityWebRequest` dentro de una **corrutina**, así que el
juego sigue dibujándose mientras espera la respuesta. Cuando llega la respuesta, `ApiClient`
llama a un **callback** con dos posibles resultados:

- `error == null` → la petición fue bien y se reciben los datos ya convertidos desde JSON.
- `error != null` → se recibe el mensaje de error. Si el servidor responde `{ "mensaje": "..." }`,
  se usa ese texto. Si no se puede conectar, el mensaje es "No se puede conectar con el servidor...".

La interfaz se dibuja con `OnGUI`, que Unity llama en cada frame. Por eso, para cambiar lo que se
ve, **basta con cambiar las variables de estado** (`info`, `notice`, `rankingLines`, `page`...):
en el siguiente frame la interfaz ya aparece actualizada.

### 3.2 Mientras se espera la respuesta (todas las peticiones)

`MainMenu.Run()` cuenta las peticiones en curso (`pendingRequests`). Mientras hay alguna:

- se muestra **"Conectando con el servidor..."** debajo de las opciones;
- se ignoran el teclado y los clics (`Busy`), para que no se envíe la misma petición dos veces.

### 3.3 Iniciar sesión o registrarse: `POST /api/auth/login` y `POST /api/auth/registro`

`MainMenu.OnLoggedIn`:

| Respuesta | Cómo cambia la interfaz |
|---|---|
| Correcta | Se borra el campo de contraseña, se pasa a la página **GUARDADO** y aparece en verde "Sesión iniciada como *usuario*". El HUD de la partida muestra el nombre arriba a la derecha |
| `401` | Aparece en rojo "Usuario o contraseña incorrectos." y se sigue en la página de cuenta |
| `409` | Aparece en rojo "Ese nombre de usuario ya existe." |
| `400` | Aparece en rojo el error de validación del servidor (por ejemplo, la longitud de la contraseña) |
| `429` | Aparece en rojo "Demasiados intentos. Espera un minuto." |

### 3.4 Abrir la página GUARDADO: `GET /api/partidas`

Al entrar se muestra "Buscando tu partida en el servidor...". Después, en `MainMenu.OnLatestSave`:

- **Hay partida:** se muestra el resumen, `daniel:  MUNDO 2 · 3150 PUNTOS · 24 MONEDAS · 3 VIDAS`,
  y se guarda su `id` para que la siguiente vez se sobrescriba con `PUT`.
- **Lista vacía:** se muestra "Todavía no tienes ninguna partida guardada".
- **Error:** se muestra en rojo. Si era `401`, se vuelve a la página de inicio de sesión (apartado 4.3).

### 3.5 Guardar partida: `PUT /api/partidas/{id}` o `POST /api/partidas`

`MainMenu.SaveGame`. Si ya hay partida, la sobrescribe. Si no hay o el servidor responde `404`, crea una nueva.

- **Correcto:** el resumen se actualiza con la partida guardada, aparece "Partida guardada en el
  servidor" y se guarda también una copia local en `partida.json`.
- **`409`:** aparece "Solo puedes tener 3 partidas guardadas.".

### 3.6 Cargar partida: `GET /api/partidas`

`MainMenu.OnLoadRequested`: si hay partida, el menú se cierra y el juego empieza en ese mundo con
esos puntos, monedas y vidas (`Game.ContinueFrom`). Si no hay, aparece "No tienes ninguna partida guardada".

### 3.7 Ranking: `GET /api/puntuaciones?top=10`

Al entrar se muestra "Cargando el ranking...". Después, en `MainMenu.OnRanking`:

- Se crea una línea por jugador: `1. daniel   12400 PUNTOS   (MUNDO 4)`. El primero aparece en **dorado**.
- Si no hay puntuaciones: "Todavía no hay puntuaciones".
- Si el servidor está apagado: el mensaje de conexión en rojo.

### 3.8 Fin de partida: `POST /api/puntuaciones`

`Game.SubmitScore`, al perder todas las vidas o al ganar el último mundo. El texto aparece en la
pantalla final, debajo del mensaje principal:

| Situación | Texto |
|---|---|
| Sin sesión (no se hace la petición) | "Inicia sesión en GUARDADO para salir en el ranking" |
| Mientras se envía | "Enviando puntuación al ranking..." |
| Correcto | "¡Puesto *N* en el ranking!", con la posición que devuelve el servidor |
| Error | El mensaje de error |

### 3.9 Cerrar sesión (no hace petición)

`ApiClient.Logout()` borra el token, se vuelve a la página de cuenta y aparece "Sesión cerrada".
El nombre de usuario desaparece del HUD.

---

## 4. Seguridad: gestión de la sesión y servicios protegidos

### 4.1 Cómo funciona la sesión

La sesión usa **JWT (JSON Web Token)**:

1. El jugador escribe usuario y contraseña en la página **CUENTA**.
2. `ApiClient.Login` / `Register` envía `POST /api/auth/login` o `/api/auth/registro` con
   `{ "nombreUsuario", "contrasena" }`.
3. El servidor comprueba la contraseña contra su hash PBKDF2 y devuelve
   `{ "token", "nombreUsuario", "expira" }`. El token está firmado con HMAC-SHA256 y caduca a las 2 horas.
4. `ApiClient` guarda `Token` y `UserName`. `ApiClient.LoggedIn` indica si hay sesión.
5. Todas las peticiones siguientes llevan la cabecera `Authorization: Bearer <token>`.
6. La sesión termina al cerrar sesión, al cerrar el juego o cuando el servidor rechaza el token.

### 4.2 Medidas en el cliente (Unity)

| Medida | Dónde | Por qué |
|---|---|---|
| El token solo se guarda **en memoria**, nunca en `PlayerPrefs` ni en archivos | `ApiClient.Token` (propiedad con `private set`) | Nadie puede leerlo del disco. Al cerrar el juego, la sesión se cierra |
| Solo `ApiClient` puede cambiar el token | `private set` en `Token` y `UserName` | El resto del juego solo puede leerlo o cerrar sesión con `Logout()` |
| La cabecera `Authorization: Bearer` se añade en un único sitio | `ApiClient.Send` | Todas las llamadas protegidas se autentican igual |
| Al recibir `401` se cierra la sesión sola | `ApiClient.Send` (`if (responseCode == 401 && LoggedIn) Logout()`) | Si el token caduca o no es válido, no se sigue usando |
| La interfaz vuelve a la página de inicio de sesión cuando la sesión caduca | `MainMenu.ShowServerError` | El jugador ve que tiene que volver a entrar |
| Campo de contraseña oculto | `GUI.PasswordField(..., '*', 64, ...)` en `MainMenu.DrawLoginFields` | La contraseña no se ve en pantalla |
| La contraseña se borra de memoria después de iniciar sesión | `passwordField = ""` en `MainMenu.OnLoggedIn` | No se queda guardada en el menú |
| Límites de longitud iguales a los del servidor (20 y 64 caracteres) | `GUI.TextField(..., 20)` y `GUI.PasswordField(..., 64)` | No se pueden escribir datos que el servidor rechazaría |
| La puntuación solo se envía con sesión iniciada | `Game.SubmitScore` (`if (!ApiClient.LoggedIn)`) | No se hacen peticiones protegidas sin token |
| GUARDADO pide iniciar sesión antes de entrar | `MainMenu.Choose` (`ApiClient.LoggedIn ? Page.Save : Page.Account`) | Las partidas son privadas de cada usuario |
| Tiempo máximo de espera de 5 segundos | `UnityWebRequest { timeout = 5 }` | El juego no se queda esperando para siempre |
| Errores del servidor mostrados sin detalles técnicos | `ApiClient.ErrorMessage` | Solo se muestra el texto de `{ "mensaje" }` o el código de estado |
| No se envían peticiones mientras hay otra en curso | `MainMenu.Busy` | Evita repetir peticiones por pulsar varias veces |
| HTTP sin cifrar solo en el editor y en las builds de desarrollo | `Editor/NetworkSettings.cs` (`InsecureHttpOption.DevelopmentOnly`) | Una build final no puede usar HTTP: tendría que usar HTTPS |

### 4.3 Servicios protegidos (lado del servidor)

El cliente solo puede llamar con éxito a estos servicios si envía un token válido:

| Servicio | Protegido | Qué pasa sin token |
|---|:---:|---|
| `POST /api/auth/registro`, `POST /api/auth/login` | No (límite de 5 intentos por minuto e IP) | `429` si se superan los intentos |
| `GET /api/auth/yo` | 🔒 | `401` "Tienes que iniciar sesión." |
| `GET`, `POST`, `PUT`, `DELETE /api/partidas` | 🔒 | `401` |
| `GET /api/puntuaciones` | No (el ranking es público) | — |
| `POST /api/puntuaciones` | 🔒 | `401` |

En el servidor esto se configura con los recursos de ASP.NET Core:

- `AddAuthentication().AddJwtBearer(...)` en `Servidor/Program.cs` comprueba la firma, el emisor,
  la audiencia y la caducidad del token.
- `[Authorize]` en `PartidasController` y en las rutas protegidas de `AuthController` y
  `PuntuacionesController`; `[AllowAnonymous]` en el ranking.
- El usuario se saca **siempre del token** (`User.UsuarioId()`), nunca de los datos que envía el
  cliente. Así nadie puede leer ni cambiar las partidas de otro: si lo intenta, recibe `404`.

### 4.4 Ejemplo completo: sesión caducada

1. El jugador inició sesión hace más de 2 horas y entra en **GUARDADO**.
2. `GET /api/partidas` lleva un token caducado → el servidor responde `401` con
   `{ "mensaje": "Tienes que iniciar sesión." }`.
3. `ApiClient.Send` ve el `401` y llama a `Logout()`.
4. `MainMenu.ShowServerError` ve que ya no hay sesión, abre la página **CUENTA** y muestra el mensaje en rojo.
5. El jugador vuelve a iniciar sesión y recibe un token nuevo.

---

## 5. Cómo crear y ejecutar el .exe

### 5.1 Qué se genera

La versión para jugar sin Unity se crea en la carpeta `Build/Juegardo/`:

```
Build/Juegardo/
├── Jugar.bat          Arranca el servidor y después el juego (es el que hay que abrir)
├── Juegardo.exe       El juego
├── Juegardo_Data/, MonoBleedingEdge/, UnityPlayer.dll...   Archivos que necesita el juego
└── Servidor/
    └── JuegardoApi.exe   El servidor, con .NET incluido
```

No hace falta tener instalado **ni Unity ni .NET**. Para llevarlo a otro ordenador basta con
copiar la carpeta `Build/Juegardo` entera (o comprimirla en un ZIP).

La carpeta `Build/` está en el `.gitignore`, así que no se sube al repositorio. Para compartir el
juego hay que pasar el ZIP o subirlo como *Release* en GitHub.

### 5.2 Compilar el juego (Unity)

Se usa el sistema de compilación del framework, `BuildPipeline.BuildPlayer`, desde el script de
editor `Assets/Scripts/Editor/BuildGame.cs`. Hay dos formas de lanzarlo:

- **Desde el editor:** menú **Juegardo → Crear .exe para Windows**. Al terminar se abre la carpeta.
- **Desde la línea de comandos**, con el proyecto cerrado en el editor:

  ```bat
  "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe" -batchmode -quit ^
    -projectPath C:\ruta\al\proyecto -executeMethod BuildGame.Windows -logFile build.log
  ```

El script compila las escenas activas en **File → Build Profiles** para Windows de 64 bits y deja
el resultado en `Build/Juegardo/Juegardo.exe`.

Es una **Development Build** a propósito: el juego habla con el servidor por
`http://localhost:5080`, y el proyecto solo permite HTTP sin cifrar en las builds de desarrollo
(`NetworkSettings.cs`, apartado 4.2). Por eso aparece el texto "Development Build" en una esquina.
Para una versión final habría que servir la API por HTTPS y quitar esa opción.

### 5.3 Compilar el servidor (ASP.NET Core)

Desde la carpeta `Servidor/`:

```bash
dotnet publish JuegardoApi.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ../Build/Juegardo/Servidor
```

- `--self-contained true` incluye .NET dentro del `.exe`, así que el ordenador no necesita tenerlo instalado.
- `-p:PublishSingleFile=true` junta el servidor en un solo `JuegardoApi.exe`.
- Si no tienes .NET instalado, puedes usar el que trae Unity:
  `C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Data\DotNetSdk\dotnet.exe`.

### 5.4 Ejecutarlo

Doble clic en **`Jugar.bat`**. Lo que hace es:

1. Configurar el servidor para uso local (`ASPNETCORE_ENVIRONMENT=Development`, que usa la clave
   JWT de `appsettings.Development.json`, y `ASPNETCORE_URLS=http://localhost:5080`).
2. Arrancar `Servidor/JuegardoApi.exe` en una ventana minimizada. La primera vez crea la base de
   datos `juegardo.db`, vacía, y aplica las migraciones.
3. Esperar 3 segundos y abrir `Juegardo.exe`.

Al terminar de jugar hay que cerrar la ventana del servidor. Si se abre `Juegardo.exe` directamente,
el juego funciona, pero GUARDADO y RANKING dirán que no se puede conectar con el servidor.

**Datos que no van en la carpeta:**

- **Cuentas, partidas y ranking:** están en `Servidor/juegardo.db`, en el ordenador donde se juega.
  En otro ordenador empiezan vacíos.
- **Copia local de la partida:** está en `partida.json`, dentro de
  `C:\Users\<usuario>\AppData\LocalLow\DefaultCompany\Juegardo\`.

---

## Cómo probarlo

**Con el .exe:** doble clic en `Build/Juegardo/Jugar.bat` (apartado 5).

**Desde Unity:**

1. Arrancar el servidor con `Servidor/iniciar-servidor.bat` (escucha en `http://localhost:5080`).
2. Abrir el proyecto en Unity Hub y pulsar **Play**.

**En el menú del juego:**

- **GUARDADO**: registrarse o iniciar sesión, guardar y cargar partida.
- **RANKING**: ver las puntuaciones.
- Para ver los mensajes de error, probar con una contraseña incorrecta, con un usuario repetido
  o con el servidor apagado.
