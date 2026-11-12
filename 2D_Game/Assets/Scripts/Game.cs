using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Starts automatically when Play is pressed: builds the current world from its text map
// (see Worlds.cs), follows the player with the camera and draws the HUD.
public class Game : MonoBehaviour
{
    // WorldClear: a world was finished and the next one is waiting. Won: the last world was finished.
    enum State { Menu, Playing, Dead, WorldClear, Won, GameOver }

    const float WorldTitleTime = 2.5f;

    public static Game Instance { get; private set; }
    public static PhysicsMaterial2D Slippery { get; private set; }

    // The world being played (1 = the first). It's what gets saved as "nivel".
    int currentLevel = 1, score, coins, lives = 3;
    State state;
    float deathTimer, worldTitleUntil;
    World world;
    string[] Map => world.Map;
    // True while there's a game the player left with ESC that can still be saved.
    bool gameInProgress;
    // Shown under the end-of-game message: whether the score reached the online ranking.
    string rankingNotice;
    MainMenu menu;
    int levelWidth;
    Transform level;
    PlayerController player;
    Camera cam;

    // The boss room: its doorway is the '|' in the map. The camera starts showing the room this
    // far before the doorway, and the gate closes once the player is this far inside.
    // After reaching it, dying puts you back just outside the doorway instead of at the start.
    const float ApproachDistance = 10f;
    const float EnterDistance = 4f;
    Boss boss;
    float gateX, groundY, bossTitleUntil, shakeUntil, shakeStrength;
    bool arenaClosed, reachedBoss;

    public PlayerController Player => player;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindAnyObjectByType<Game>() == null) new GameObject("Game").AddComponent<Game>();
    }

    void Awake()
    {
        Instance = this;
        Slippery = new PhysicsMaterial2D("Slippery") { friction = 0f, bounciness = 0f };

        cam = Camera.main;
        if (cam == null)
        {
            cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
        }
        cam.orthographic = true;
        // All maps have the same number of rows, so the camera height never changes.
        cam.orthographicSize = Worlds.All[0].Map.Length / 2f;
        cam.clearFlags = CameraClearFlags.SolidColor;

        menu = gameObject.AddComponent<MainMenu>();
        ShowMenu();
    }

    // The main menu shows over a frozen copy of the level.
    void ShowMenu()
    {
        BuildLevel();
        state = State.Menu;
        player.Freeze();
        menu.Open();
    }

    public void StartNewGame() => ContinueFrom(new SaveData());

    // Starts the given world from the beginning with the given points, coins and lives.
    public void ContinueFrom(SaveData data)
    {
        menu.Close();
        currentLevel = Mathf.Clamp(data.nivel, 1, Worlds.All.Length);
        reachedBoss = false;
        score = data.puntos;
        coins = data.monedas;
        lives = Mathf.Max(1, data.vidas);
        gameInProgress = true;
        BuildLevel();
    }

    // What would be saved right now, or null if there's no game to save.
    public SaveData CurrentProgress => gameInProgress
        ? new SaveData { nivel = currentLevel, puntos = score, monedas = coins, vidas = lives }
        : null;

    // When a game ends, logged-in players send their score to the online ranking.
    void SubmitScore()
    {
        if (!ApiClient.LoggedIn)
        {
            rankingNotice = "Inicia sesión en GUARDADO para salir en el ranking";
            return;
        }
        rankingNotice = "Enviando puntuación al ranking...";
        StartCoroutine(ApiClient.SubmitScore(score, currentLevel, (position, error) =>
            rankingNotice = error ?? $"¡Puesto {position} en el ranking!"));
    }

    public void AddCoin()
    {
        coins++;
        score += 50;
    }

    public void AddScore(int points) => score += points;

    public void PlayerDied()
    {
        if (state != State.Playing) return;
        state = State.Dead;
        lives--;
        deathTimer = 2f;
    }

    // Touching the gem. The next world starts when ENTER is pressed; after the last one the game is won
    // and the score goes to the ranking.
    public void Win()
    {
        if (state != State.Playing) return;
        score += 1000;
        player.Freeze();
        reachedBoss = false;
        if (currentLevel < Worlds.All.Length)
        {
            state = State.WorldClear;
            // Moved on right away, so saving from the menu now continues from the next world.
            currentLevel++;
            return;
        }
        state = State.Won;
        gameInProgress = false;
        SubmitScore();
    }

    void Update()
    {
        // The menu handles its own input.
        if (state == State.Menu) return;

        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            Time.timeScale = 1f;
            ShowMenu();
            return;
        }

        if (state == State.Playing && boss != null && !arenaClosed && player.transform.position.x > gateX + EnterDistance)
            CloseArena();

        if (state == State.Dead)
        {
            deathTimer -= Time.deltaTime;
            if (deathTimer <= 0f)
            {
                if (lives > 0) BuildLevel();
                else
                {
                    state = State.GameOver;
                    gameInProgress = false;
                    SubmitScore();
                }
            }
        }
        else if (state == State.WorldClear || state == State.Won || state == State.GameOver)
        {
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
            {
                if (state == State.WorldClear) BuildLevel();
                else StartNewGame();
            }
        }
    }

    void LateUpdate()
    {
        if (player == null || state == State.Dead) return;
        float halfWidth = cam.orthographicSize * cam.aspect;
        float px = player.transform.position.x;
        float maxX = Mathf.Max(halfWidth - 0.5f, levelWidth - 0.5f - halfWidth);
        float x = Mathf.Clamp(px, halfWidth - 0.5f, maxX);
        if (gateX >= 0f)
        {
            // Following the player but showing only the boss room (its doorway at the left edge).
            float roomX = Mathf.Clamp(px, Mathf.Min(gateX - 0.5f + halfWidth, maxX), maxX);
            if (arenaClosed) x = roomX;
            else
            {
                // Walking up to the doorway, the camera drifts ahead to show the room and the boss
                // waiting in it, while keeping the player on screen. By the time the gate closes it's
                // already framing the room, so nothing jumps.
                float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(gateX - ApproachDistance, gateX, px));
                x = Mathf.Lerp(x, Mathf.Max(x, Mathf.Min(roomX, px + halfWidth - 2f)), t);
            }
        }
        var shake = Time.time < shakeUntil ? (Vector3)(Random.insideUnitCircle * shakeStrength) : Vector3.zero;
        cam.transform.position = new Vector3(x, (Map.Length - 1) / 2f, -10f) + shake;
    }

    public void Shake(float time, float strength)
    {
        shakeUntil = Time.time + time;
        shakeStrength = strength;
    }

    // Shuts the player in with the boss: a stone gate slides down out of the wall over the doorway.
    void CloseArena()
    {
        arenaClosed = true;
        reachedBoss = true;
        bossTitleUntil = Time.time + 2.5f;

        // The doorway goes from the floor up to the wall above it.
        int column = Mathf.RoundToInt(gateX), rows = Map.Length;
        int topRow = rows - 1 - Mathf.RoundToInt(groundY);
        while (topRow > 0 && !IsSolid(Map, column, topRow - 1)) topRow--;
        float top = rows - 1 - topRow;

        // The collider is in place at once, so there's no slipping back out while the stones fall.
        var gate = new GameObject("Gate");
        gate.transform.SetParent(level);
        var box = gate.AddComponent<BoxCollider2D>();
        box.offset = new Vector2(gateX, (groundY + top) / 2f);
        box.size = new Vector2(1f, top - groundY + 1f);
        box.sharedMaterial = Slippery;

        // Drawn behind the wall, so they come out from inside it.
        var stones = new GameObject("GateStones").transform;
        stones.SetParent(level);
        for (float y = groundY; y <= top; y++) Tile(new Vector2(gateX, y), PixelArt.Stone, "GateBlock", -1).transform.SetParent(stones);
        StartCoroutine(DropGate(stones, top - groundY + 1f));

        boss.Begin(gateX + 0.5f, levelWidth - 0.5f);
    }

    IEnumerator DropGate(Transform stones, float height)
    {
        const float Duration = 0.5f;
        for (float t = 0f; t < Duration && stones != null; t += Time.deltaTime)
        {
            float k = t / Duration;
            stones.position = Vector3.up * height * (1f - k * k);   // speeds up as it falls
            yield return null;
        }
        if (stones == null) yield break;
        stones.position = Vector3.zero;
        Shake(0.4f, 0.3f);
    }

    // The gem appears in the middle of the room once the boss falls.
    public void BossDefeated(Vector2 floorCenter)
    {
        score += 5000;
        SpawnGoal(new Vector2(Mathf.Round(floorCenter.x), floorCenter.y + 0.5f));
    }

    void DrawBossBar(GUIStyle style)
    {
        float width = Mathf.Min(500f, Screen.width * 0.5f), height = style.fontSize * 0.6f;
        float x = (Screen.width - width) / 2f, y = style.fontSize * 2.8f;
        var label = new GUIStyle(style) { alignment = TextAnchor.MiddleCenter, fontSize = style.fontSize * 3 / 4 };
        TextDrawing.Shadowed(new Rect(0, y - style.fontSize * 1.2f, Screen.width, style.fontSize), Boss.Name, label, TextDrawing.Gold);
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(x - 3, y - 3, width + 6, height + 6), Texture2D.whiteTexture);
        GUI.color = new Color(0.85f, 0.2f, 0.15f);
        GUI.DrawTexture(new Rect(x, y, width * boss.Health / Boss.MaxHealth, height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void OnGUI()
    {
        // The menu draws itself (MainMenu).
        if (state == State.Menu) return;

        var style = TextDrawing.BaseStyle();
        TextDrawing.Shadowed(new Rect(20, 10, 900, 60), $"MUNDO {currentLevel}    PUNTOS {score:000000}    MONEDAS {coins:00}    VIDAS {lives}", style);
        if (ApiClient.LoggedIn)
        {
            var right = new GUIStyle(style) { alignment = TextAnchor.UpperRight, fontSize = style.fontSize * 3 / 4 };
            TextDrawing.Shadowed(new Rect(0, 14, Screen.width - 20, 60), ApiClient.UserName, right);
        }

        var center = new GUIStyle(style) { alignment = TextAnchor.MiddleCenter, fontSize = style.fontSize * 3 / 2 };
        // The world's name shows for a moment when it starts, and the boss's when the gate closes.
        if (state == State.Playing && Time.time < worldTitleUntil)
            TextDrawing.Shadowed(new Rect(0, 0, Screen.width, Screen.height * 0.6f), $"MUNDO {currentLevel}\n{world.Name}", center, TextDrawing.Gold);
        if (arenaClosed && boss != null && !boss.Defeated)
        {
            DrawBossBar(style);
            if (state == State.Playing && Time.time < bossTitleUntil)
                TextDrawing.Shadowed(new Rect(0, 0, Screen.width, Screen.height * 0.6f), $"¡{Boss.Name}!", center, TextDrawing.Bad);
        }

        string message = state switch
        {
            State.WorldClear => $"¡MUNDO {currentLevel - 1} SUPERADO!\nSiguiente: {Worlds.Get(currentLevel).Name}\nENTER: continuar    ESC: menú",
            State.Won => "¡HAS SUPERADO TODOS LOS MUNDOS!\nENTER: jugar otra vez    ESC: menú",
            State.GameOver => "GAME OVER\nENTER: reintentar    ESC: menú",
            _ => null,
        };
        if (message == null) return;
        TextDrawing.Shadowed(new Rect(0, 0, Screen.width, Screen.height), message, center);
        if (rankingNotice != null)
        {
            var below = new GUIStyle(style) { alignment = TextAnchor.MiddleCenter };
            TextDrawing.Shadowed(new Rect(0, Screen.height * 0.68f, Screen.width, 60), rankingNotice, below, TextDrawing.Gold);
        }
    }

    void BuildLevel()
    {
        if (level != null) Destroy(level.gameObject);
        level = new GameObject("Level").transform;
        state = State.Playing;
        rankingNotice = null;
        world = Worlds.Get(currentLevel);
        cam.backgroundColor = world.Sky;
        worldTitleUntil = Time.time + WorldTitleTime;
        boss = null;
        gateX = -1f;
        arenaClosed = false;
        shakeUntil = 0f;

        int rows = Map.Length;
        levelWidth = 0;
        foreach (var row in Map) levelWidth = Mathf.Max(levelWidth, row.Length);

        Vector2 spawn = new Vector2(2, 2);
        for (int y = 0; y < rows; y++)
        {
            string row = Map[y];
            for (int x = 0; x < row.Length; x++)
            {
                var pos = new Vector2(x, rows - 1 - y);
                switch (row[x])
                {
                    case '#': Tile(pos, GroundSprite(world, x, y), "Ground"); break;
                    case '~': SpawnLava(pos, IsLavaSurface(Map, x, y)); break;
                    case '^':
                        SpawnLava(pos, IsLavaSurface(Map, x, y));
                        SpawnEmber(pos);
                        break;
                    case 'B': SpawnBrick(pos); break;
                    case '=': Tile(pos, PixelArt.Plank, "Plank"); break;
                    case '!': SpawnBonusBlock(pos, BlockContents.Coin); break;
                    case 'M': SpawnBonusBlock(pos, BlockContents.Berry); break;
                    case 'Q': SpawnBonusBlock(pos, BlockContents.Pepper); break;
                    case 'H': SpawnBonusBlock(pos, BlockContents.Star, hidden: true); break;
                    case 'o': SpawnCoin(pos); break;
                    case 'E': SpawnEnemy(pos, EnemyKind.Slime); break;
                    case 'S': SpawnEnemy(pos, EnemyKind.Spiky); break;
                    case 'V': SpawnEnemy(pos, EnemyKind.Bat); break;
                    case 'R': SpawnEnemy(pos, EnemyKind.Frog); break;
                    case 'C': SpawnEnemy(pos, EnemyKind.Cactus); break;
                    case 'G': SpawnEnemy(pos, EnemyKind.Ghost); break;
                    case 'K': SpawnBoss(pos); break;
                    case '|':
                        gateX = pos.x;
                        groundY = pos.y;
                        break;
                    case 'F': SpawnGoal(pos); break;
                    case 'P': spawn = pos; break;
                }
            }
            AddRowColliders(row, rows - 1 - y);
        }
        // After reaching the boss once, you come back just outside its gate.
        if (reachedBoss && gateX >= 0f) spawn = new Vector2(gateX - 3f, groundY);

        // Invisible walls at both ends of the level, tall enough that nothing can jump over them.
        foreach (float wallX in new[] { -1f, levelWidth })
        {
            var wall = level.gameObject.AddComponent<BoxCollider2D>();
            wall.offset = new Vector2(wallX, rows / 2f + 10f);
            wall.size = new Vector2(1f, rows + 40f);
            wall.sharedMaterial = Slippery;
        }

        for (int x = 4; world.Clouds && x < levelWidth; x += 11)
        {
            var cloud = new GameObject("Cloud");
            cloud.transform.SetParent(level);
            cloud.transform.position = new Vector3(x, rows - 2.5f - (x * 7 % 3), 0);
            cloud.transform.localScale = Vector3.one * 2.5f;
            PixelArt.AddRenderer(cloud, PixelArt.Cloud, -10).color = new Color(1, 1, 1, 0.9f);
        }

        SpawnPlayer(spawn);
    }

    // The top tile of the ground (grass, sand...) unless something solid sits on it.
    // The top row counts as covered, so a cave ceiling shows rock, not grass.
    // Also used by the editor's level preview, so the ground looks the same there as in play.
    public static Sprite GroundSprite(World world, int x, int y) =>
        PixelArt.Ground(world.Theme, top: y > 0 && !IsSolid(world.Map, x, y - 1));

    static bool IsSolid(string[] map, int x, int y) =>
        y >= 0 && y < map.Length && x >= 0 && x < map[y].Length && "#B!MQ".IndexOf(map[y][x]) >= 0;

    // '~' is lava and '^' is lava with a spark in it. Only the top tile of a pit is the bubbling surface.
    public static bool IsLavaSurface(string[] map, int x, int y) =>
        y == 0 || x >= map[y - 1].Length || "~^".IndexOf(map[y - 1][x]) < 0;

    // One collider per horizontal run of ground or planks, so the player doesn't snag on seams.
    // Stone and bonus blocks get their own collider because they can be hit one by one.
    void AddRowColliders(string row, float worldY)
    {
        for (int x = 0; x < row.Length;)
        {
            char c = row[x];
            if (c != '#' && c != '=') { x++; continue; }
            int start = x;
            while (x < row.Length && row[x] == c) x++;
            int length = x - start;
            float centerX = start + (length - 1) / 2f;

            if (c == '=')
            {
                var go = new GameObject("PlankCollider");
                go.transform.SetParent(level);
                go.transform.position = new Vector3(centerX, worldY + 0.35f);
                var box = go.AddComponent<BoxCollider2D>();
                box.size = new Vector2(length, 0.3f);
                box.sharedMaterial = Slippery;
                box.usedByEffector = true;
                go.AddComponent<PlatformEffector2D>().useOneWay = true;
            }
            else
            {
                var box = level.gameObject.AddComponent<BoxCollider2D>();
                box.offset = new Vector2(centerX, worldY);
                box.size = new Vector2(length, 1f);
                box.sharedMaterial = Slippery;
            }
        }
    }

    GameObject Tile(Vector2 pos, Sprite sprite, string name, int order = 0)
    {
        var go = new GameObject(name);
        go.transform.SetParent(level);
        go.transform.position = pos;
        PixelArt.AddRenderer(go, sprite, order);
        return go;
    }

    void SpawnBrick(Vector2 pos)
    {
        var go = Tile(pos, PixelArt.Stone, "Stone");
        go.AddComponent<BoxCollider2D>().sharedMaterial = Slippery;
        go.AddComponent<Brick>();
    }

    void SpawnBonusBlock(Vector2 pos, BlockContents contents, bool hidden = false)
    {
        var go = Tile(pos, PixelArt.Bonus, hidden ? "HiddenBlock" : "BonusBlock");
        var box = go.AddComponent<BoxCollider2D>();
        box.sharedMaterial = Slippery;
        // A hidden block is an invisible trigger until the player bumps it from below.
        box.isTrigger = hidden;
        go.GetComponent<SpriteRenderer>().enabled = !hidden;
        var block = go.AddComponent<BonusBlock>();
        block.Contents = contents;
        block.Hidden = hidden;
    }

    void SpawnCoin(Vector2 pos)
    {
        var go = Tile(pos, PixelArt.Coin, "Coin", 3);
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.5f, 0.7f);
        box.isTrigger = true;
        go.AddComponent<Coin>();
    }

    void SpawnEnemy(Vector2 pos, EnemyKind kind)
    {
        var (sprite, size, offset) = kind switch
        {
            EnemyKind.Spiky => (PixelArt.Spiky, new Vector2(0.85f, 0.7f), new Vector2(0f, -0.15f)),
            EnemyKind.Bat => (PixelArt.Bat, new Vector2(0.8f, 0.5f), new Vector2(0f, 0.06f)),
            EnemyKind.Frog => (PixelArt.Frog, new Vector2(0.85f, 0.6f), new Vector2(0f, -0.2f)),
            EnemyKind.Cactus => (PixelArt.Cactus, new Vector2(0.75f, 0.9f), new Vector2(0f, -0.05f)),
            EnemyKind.Ghost => (PixelArt.Ghost, new Vector2(0.75f, 0.7f), new Vector2(0f, 0f)),
            _ => (PixelArt.Slime, new Vector2(0.85f, 0.65f), new Vector2(0f, -0.17f)),
        };
        bool flying = kind == EnemyKind.Bat || kind == EnemyKind.Ghost;
        var go = Tile(pos, sprite, kind.ToString(), 5);
        var body = go.AddComponent<Rigidbody2D>();
        body.gravityScale = flying ? 0f : 4f;
        body.freezeRotation = true;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = size;
        box.offset = offset;
        box.sharedMaterial = Slippery;
        // The ghost floats through walls, so it's a trigger: the player can't stand on it either.
        box.isTrigger = kind == EnemyKind.Ghost;
        go.AddComponent<Enemy>().Kind = kind;
    }

    // Only the surface tile gets a trigger; the tiles under it are just for looks.
    void SpawnLava(Vector2 pos, bool surface)
    {
        var go = Tile(pos, surface ? PixelArt.Lava : PixelArt.LavaDeep, "Lava");
        if (!surface) return;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1f, 0.5f);
        box.offset = new Vector2(0f, -0.2f);
        box.isTrigger = true;
        go.AddComponent<Lava>();
    }

    // Waits under the lava surface (drawn behind it), so it's hidden until it jumps.
    void SpawnEmber(Vector2 pos)
    {
        var go = Tile(pos + Vector2.down * 0.6f, PixelArt.Ember, "Ember", -1);
        var body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.6f, 0.7f);
        box.isTrigger = true;
        go.AddComponent<Hazard>();
        go.AddComponent<Ember>();
    }

    void SpawnBoss(Vector2 pos)
    {
        var go = Tile(pos + Vector2.down * 0.5f, PixelArt.Boss, "Boss", 6);
        go.transform.localScale = Vector3.one * Boss.Scale;
        // Dynamic (with no gravity) rather than kinematic so the player's fireballs register on it.
        var body = go.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.sleepMode = RigidbodySleepMode2D.NeverSleep;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1.6f, 1.75f);
        box.offset = new Vector2(0f, 0.875f);
        box.isTrigger = true;
        boss = go.AddComponent<Boss>();
    }

    void SpawnGoal(Vector2 pos)
    {
        var go = Tile(pos + Vector2.up * 0.5f, PixelArt.Gem, "Goal", 4);
        go.transform.localScale = Vector3.one * 1.5f;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.8f, 1.5f);
        box.isTrigger = true;
        go.AddComponent<Goal>();
    }

    void SpawnPlayer(Vector2 pos)
    {
        // The player's position is at its feet (bottom of the tile).
        var go = Tile(pos + Vector2.down * 0.5f, PixelArt.Hero, "Player", 10);
        var body = go.AddComponent<Rigidbody2D>();
        body.gravityScale = 4f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        go.AddComponent<CapsuleCollider2D>().sharedMaterial = Slippery;
        player = go.AddComponent<PlayerController>();
    }
}
