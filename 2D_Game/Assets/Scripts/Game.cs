using UnityEngine;
using UnityEngine.InputSystem;

// Starts automatically when Play is pressed: builds the level from the text map,
// follows the player with the camera and draws the HUD.
public class Game : MonoBehaviour
{
    // # ground   B stone (breakable when big)   = one-way plank   o coin
    // ! bonus block (coin)   M bonus block (grow berry)   Q bonus block (fire pepper, or a berry if small)
    // H hidden block (star): invisible until hit from below
    // E slime   S spiky hedgehog   V bat   P player start   F goal gem
    public static readonly string[] Map =
    {
        "",
        "",
        "",
        "",
        "....................Q",
        ".......................................................................oooo",
        "......................................................................======...........................................##",
        ".........................................V.................ooooooo....................................V...............###",
        "..............M...B!B!B..........H........................=========.................V.....BBBMBBB.........BQB........####",
        ".......................................ooooo..##.....................ooooo.......................ooooo..............#####",
        "............................##................##......................................##...........................######",
        "...P..................E.....##.....E..........##..S....E......E.................E.....##....S..E..............E...#######.....F",
        "########################################...###########################...#########################...#############################",
        "########################################...###########################...#########################...#############################",
    };

    enum State { Menu, Playing, Dead, Won, GameOver }
    enum MenuPage { Main, Save }

    static readonly string[] MainOptions = { "JUGAR", "GUARDADO" };
    static readonly string[] SaveOptions = { "GUARDAR PARTIDA", "CARGAR PARTIDA", "VOLVER" };
    static readonly Color Gold = new(1f, 0.82f, 0.16f);
    static readonly Color Good = new(0.6f, 1f, 0.6f);
    static readonly Color Bad = new(1f, 0.6f, 0.6f);

    public static Game Instance { get; private set; }
    public static PhysicsMaterial2D Slippery { get; private set; }

    // Only one level exists for now; currentLevel is kept (and saved) so more can be added later.
    int currentLevel = 1, score, coins, lives = 3;
    State state;
    float deathTimer;
    // True while there's a game the player left with ESC that can still be saved.
    bool gameInProgress;
    MenuPage menuPage;
    int menuIndex;
    string menuNotice, savedGameInfo;
    Color menuNoticeColor;
    float menuNoticeUntil;
    int levelWidth;
    Transform level;
    PlayerController player;
    Camera cam;

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
        cam.orthographicSize = Map.Length / 2f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.42f, 0.68f, 1f);

        ShowMenu();
    }

    // The main menu shows over a frozen copy of the level.
    void ShowMenu()
    {
        BuildLevel();
        state = State.Menu;
        player.Freeze();
        OpenMenuPage(MenuPage.Main, 0);
    }

    void StartGame()
    {
        currentLevel = 1;
        score = coins = 0;
        lives = 3;
        gameInProgress = true;
        BuildLevel();
    }

    string[] MenuOptions => menuPage == MenuPage.Main ? MainOptions : SaveOptions;

    void OpenMenuPage(MenuPage page, int selected)
    {
        menuPage = page;
        menuIndex = selected;
        menuNotice = null;
        if (page == MenuPage.Save) RefreshSavedGameInfo();
    }

    void RefreshSavedGameInfo()
    {
        var data = SaveSystem.Load();
        savedGameInfo = data == null
            ? "No hay ninguna partida guardada"
            : $"Partida guardada:  NIVEL {data.nivel} · {data.puntos} PUNTOS · {data.monedas} MONEDAS · {data.vidas} VIDAS";
    }

    void ChooseMenuOption(int index)
    {
        menuIndex = index;
        if (menuPage == MenuPage.Main)
        {
            if (index == 0) StartGame();
            else OpenMenuPage(MenuPage.Save, 0);
            return;
        }

        switch (index)
        {
            case 0: SaveGame(); break;
            case 1: LoadGame(); break;
            default: OpenMenuPage(MenuPage.Main, 1); break;
        }
    }

    void SaveGame()
    {
        if (!gameInProgress)
        {
            ShowNotice("No hay ninguna partida en curso. Pulsa ESC durante la partida para venir aquí.", Bad);
            return;
        }
        var data = new SaveData { nivel = currentLevel, puntos = score, monedas = coins, vidas = lives };
        if (SaveSystem.Save(data)) ShowNotice("Partida guardada", Good);
        else ShowNotice("No se pudo guardar la partida", Bad);
        RefreshSavedGameInfo();
    }

    // Starts the saved level from the beginning with the saved points, coins and lives.
    void LoadGame()
    {
        var data = SaveSystem.Load();
        if (data == null)
        {
            ShowNotice("No hay ninguna partida guardada", Bad);
            return;
        }
        currentLevel = data.nivel;
        score = data.puntos;
        coins = data.monedas;
        lives = Mathf.Max(1, data.vidas);
        gameInProgress = true;
        BuildLevel();
    }

    void ShowNotice(string text, Color color)
    {
        menuNotice = text;
        menuNoticeColor = color;
        menuNoticeUntil = Time.unscaledTime + 3f;
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

    public void Win()
    {
        if (state != State.Playing) return;
        state = State.Won;
        score += 1000;
        player.Freeze();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (state == State.Menu)
        {
            if (kb == null) return;
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)
                menuIndex = (menuIndex + MenuOptions.Length - 1) % MenuOptions.Length;
            if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)
                menuIndex = (menuIndex + 1) % MenuOptions.Length;
            if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) ChooseMenuOption(menuIndex);
            else if (kb.escapeKey.wasPressedThisFrame && menuPage == MenuPage.Save) OpenMenuPage(MenuPage.Main, 1);
            return;
        }

        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            Time.timeScale = 1f;
            ShowMenu();
            return;
        }

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
                }
            }
        }
        else if (state == State.Won || state == State.GameOver)
        {
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)) StartGame();
        }
    }

    void LateUpdate()
    {
        if (player == null || state == State.Dead) return;
        float halfWidth = cam.orthographicSize * cam.aspect;
        float x = Mathf.Clamp(player.transform.position.x, halfWidth - 0.5f, Mathf.Max(halfWidth - 0.5f, levelWidth - 0.5f - halfWidth));
        cam.transform.position = new Vector3(x, (Map.Length - 1) / 2f, -10f);
    }

    void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(18, Screen.height / 25), fontStyle = FontStyle.Bold };
        if (state == State.Menu)
        {
            DrawMenu(style);
            return;
        }
        Shadowed(new Rect(20, 10, 900, 60), $"NIVEL {currentLevel}    PUNTOS {score:000000}    MONEDAS {coins:00}    VIDAS {lives}", style);

        string message = state switch
        {
            State.Won => "¡NIVEL COMPLETADO!\nENTER: jugar otra vez    ESC: menú",
            State.GameOver => "GAME OVER\nENTER: reintentar    ESC: menú",
            _ => null,
        };
        if (message == null) return;
        var center = new GUIStyle(style) { alignment = TextAnchor.MiddleCenter, fontSize = style.fontSize * 3 / 2 };
        Shadowed(new Rect(0, 0, Screen.width, Screen.height), message, center);
    }

    // Arrows / W-S to choose, ENTER or SPACE to confirm. Options can also be clicked.
    void DrawMenu(GUIStyle baseStyle)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        var title = new GUIStyle(baseStyle) { alignment = TextAnchor.MiddleCenter, fontSize = baseStyle.fontSize * 3 };
        string heading = menuPage == MenuPage.Main ? "JUEGARDO" : "GUARDADO";
        Shadowed(new Rect(0, Screen.height * 0.12f, Screen.width, Screen.height * 0.2f), heading, title, Gold);

        var small = new GUIStyle(baseStyle) { alignment = TextAnchor.MiddleCenter, fontSize = baseStyle.fontSize * 3 / 4 };
        if (menuPage == MenuPage.Save)
            Shadowed(new Rect(0, Screen.height * 0.32f, Screen.width, baseStyle.fontSize * 2f), savedGameInfo, small);

        var option = new GUIStyle(baseStyle) { alignment = TextAnchor.MiddleCenter, fontSize = baseStyle.fontSize * 3 / 2 };
        var options = MenuOptions;
        float height = option.fontSize * 2f;
        float top = Screen.height * 0.42f;
        for (int i = 0; i < options.Length; i++)
        {
            var rect = new Rect(0, top + i * height * 1.2f, Screen.width, height);
            bool selected = i == menuIndex;
            if (rect.Contains(Event.current.mousePosition)) menuIndex = i;
            string label = selected ? $"> {options[i]} <" : options[i];
            Shadowed(rect, label, option, selected ? Gold : Color.white);
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) ChooseMenuOption(i);
        }

        if (menuNotice != null && Time.unscaledTime < menuNoticeUntil)
        {
            var notice = new GUIStyle(baseStyle) { alignment = TextAnchor.MiddleCenter };
            Shadowed(new Rect(0, top + options.Length * height * 1.2f + 10, Screen.width, height), menuNotice, notice, menuNoticeColor);
        }

        var hint = new GUIStyle(baseStyle) { alignment = TextAnchor.LowerCenter, fontSize = baseStyle.fontSize * 2 / 3 };
        string help = menuPage == MenuPage.Main ? "Flechas para elegir · ENTER para aceptar" : "Flechas para elegir · ENTER para aceptar · ESC para volver";
        Shadowed(new Rect(0, 0, Screen.width, Screen.height - 20), help, hint);
    }

    static void Shadowed(Rect rect, string text, GUIStyle style) => Shadowed(rect, text, style, Color.white);

    static void Shadowed(Rect rect, string text, GUIStyle style, Color color)
    {
        var original = style.normal.textColor;
        style.normal.textColor = Color.black;
        GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), text, style);
        style.normal.textColor = color;
        GUI.Label(rect, text, style);
        style.normal.textColor = original;
    }

    void BuildLevel()
    {
        if (level != null) Destroy(level.gameObject);
        level = new GameObject("Level").transform;
        state = State.Playing;

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
                    case '#': Tile(pos, IsSolid(x, y - 1) ? PixelArt.Dirt : PixelArt.Grass, "Ground"); break;
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
                    case 'F': SpawnGoal(pos); break;
                    case 'P': spawn = pos; break;
                }
            }
            AddRowColliders(row, rows - 1 - y);
        }

        // Invisible walls at both ends of the level, tall enough that nothing can jump over them.
        foreach (float wallX in new[] { -1f, levelWidth })
        {
            var wall = level.gameObject.AddComponent<BoxCollider2D>();
            wall.offset = new Vector2(wallX, rows / 2f + 10f);
            wall.size = new Vector2(1f, rows + 40f);
            wall.sharedMaterial = Slippery;
        }

        for (int x = 4; x < levelWidth; x += 11)
        {
            var cloud = new GameObject("Cloud");
            cloud.transform.SetParent(level);
            cloud.transform.position = new Vector3(x, rows - 2.5f - (x * 7 % 3), 0);
            cloud.transform.localScale = Vector3.one * 2.5f;
            PixelArt.AddRenderer(cloud, PixelArt.Cloud, -10).color = new Color(1, 1, 1, 0.9f);
        }

        SpawnPlayer(spawn);
    }

    static bool IsSolid(int x, int y) =>
        y >= 0 && y < Map.Length && x >= 0 && x < Map[y].Length && "#B!MQ".IndexOf(Map[y][x]) >= 0;

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
            _ => (PixelArt.Slime, new Vector2(0.85f, 0.65f), new Vector2(0f, -0.17f)),
        };
        var go = Tile(pos, sprite, kind.ToString(), 5);
        var body = go.AddComponent<Rigidbody2D>();
        body.gravityScale = kind == EnemyKind.Bat ? 0f : 4f;
        body.freezeRotation = true;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = size;
        box.offset = offset;
        box.sharedMaterial = Slippery;
        go.AddComponent<Enemy>().Kind = kind;
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
