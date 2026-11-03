using UnityEngine;
using UnityEngine.InputSystem;

// Starts automatically when Play is pressed: builds the level from the text map,
// follows the player with the camera and draws the HUD.
public class Game : MonoBehaviour
{
    // # ground   B stone (breakable when big)   = one-way plank   o coin
    // ! bonus block (coin)   M bonus block (grow berry)   Q bonus block (fire pepper, or a berry if small)
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
        "..............M...B!B!B...................................=========.................V.....BBBMBBB.........BQB........####",
        ".......................................ooooo..##.....................ooooo.......................ooooo..............#####",
        "............................##................##......................................##...........................######",
        "...P..................E.....##.....E..........##..S....E......E.................E.....##....S..E..............E...#######.....F",
        "########################################...###########################...#########################...#############################",
        "########################################...###########################...#########################...#############################",
    };

    enum State { Playing, Dead, Won, GameOver }

    public static Game Instance { get; private set; }
    public static PhysicsMaterial2D Slippery { get; private set; }

    int score, coins, lives = 3;
    State state;
    float deathTimer;
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

        BuildLevel();
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
        if (state == State.Dead)
        {
            deathTimer -= Time.deltaTime;
            if (deathTimer <= 0f)
            {
                if (lives > 0) BuildLevel();
                else state = State.GameOver;
            }
        }
        else if (state == State.Won || state == State.GameOver)
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
            {
                score = coins = 0;
                lives = 3;
                BuildLevel();
            }
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
        Shadowed(new Rect(20, 10, 600, 60), $"PUNTOS {score:000000}    MONEDAS {coins:00}    VIDAS {lives}", style);

        string message = state switch
        {
            State.Won => "¡NIVEL COMPLETADO!\nPulsa ENTER para jugar otra vez",
            State.GameOver => "GAME OVER\nPulsa ENTER para reintentar",
            _ => null,
        };
        if (message == null) return;
        var center = new GUIStyle(style) { alignment = TextAnchor.MiddleCenter, fontSize = style.fontSize * 3 / 2 };
        Shadowed(new Rect(0, 0, Screen.width, Screen.height), message, center);
    }

    static void Shadowed(Rect rect, string text, GUIStyle style)
    {
        var color = style.normal.textColor;
        style.normal.textColor = Color.black;
        GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), text, style);
        style.normal.textColor = Color.white;
        GUI.Label(rect, text, style);
        style.normal.textColor = color;
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

    void SpawnBonusBlock(Vector2 pos, BlockContents contents)
    {
        var go = Tile(pos, PixelArt.Bonus, "BonusBlock");
        go.AddComponent<BoxCollider2D>().sharedMaterial = Slippery;
        go.AddComponent<BonusBlock>().Contents = contents;
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
