using UnityEditor;
using UnityEngine;

// Shows a world's sprites in the Scene view while not playing, so the scene doesn't look empty.
// The preview is never saved into the scene and is removed when Play starts (the real level is built then).
// Pick which world to show from the Juegardo menu.
[InitializeOnLoad]
static class LevelPreview
{
    const string RootName = "Vista previa del nivel (solo editor)";
    const string WorldPref = "Juegardo.MundoVistaPrevia";

    static LevelPreview()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode) Clear();
            else if (state == PlayModeStateChange.EnteredEditMode) Build();
        };
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) Build();
        };
    }

    [MenuItem("Juegardo/Vista previa/Mundo 1 - Pradera")] static void World1() => Show(1);
    [MenuItem("Juegardo/Vista previa/Mundo 2 - Desierto")] static void World2() => Show(2);
    [MenuItem("Juegardo/Vista previa/Mundo 3 - Cueva")] static void World3() => Show(3);
    [MenuItem("Juegardo/Vista previa/Mundo 4 - Castillo")] static void World4() => Show(4);

    static void Show(int number)
    {
        EditorPrefs.SetInt(WorldPref, number);
        Build();
    }

    [MenuItem("Juegardo/Actualizar vista previa del nivel")]
    static void Build()
    {
        Clear();
        var world = Worlds.Get(EditorPrefs.GetInt(WorldPref, 1));
        var root = new GameObject(RootName) { hideFlags = HideFlags.DontSave | HideFlags.NotEditable };
        var map = world.Map;
        for (int y = 0; y < map.Length; y++)
            for (int x = 0; x < map[y].Length; x++)
            {
                Sprite sprite = map[y][x] switch
                {
                    '#' => Game.GroundSprite(world, x, y),
                    '~' or '^' => Game.IsLavaSurface(map, x, y) ? PixelArt.Lava : PixelArt.LavaDeep,
                    'R' => PixelArt.Frog,
                    'C' => PixelArt.Cactus,
                    'G' => PixelArt.Ghost,
                    'K' => PixelArt.Boss,
                    '|' => PixelArt.Stone,
                    'B' => PixelArt.Stone,
                    '!' or 'M' or 'Q' or 'H' => PixelArt.Bonus,
                    'S' => PixelArt.Spiky,
                    'V' => PixelArt.Bat,
                    '=' => PixelArt.Plank,
                    'o' => PixelArt.Coin,
                    'E' => PixelArt.Slime,
                    'F' => PixelArt.Gem,
                    'P' => PixelArt.Hero,
                    _ => null,
                };
                if (sprite == null) continue;
                var tile = new GameObject(map[y][x].ToString()) { hideFlags = HideFlags.DontSave | HideFlags.NotEditable };
                tile.transform.SetParent(root.transform);
                // The hero's and the boss's sprites are anchored at their feet, so they go half a tile lower.
                char c = map[y][x];
                tile.transform.position = new Vector2(x, map.Length - 1 - y - (c == 'P' || c == 'K' ? 0.5f : 0f));
                if (c == 'P') tile.transform.localScale = Vector3.one * 0.8f;
                if (c == 'K') tile.transform.localScale = Vector3.one * Boss.Scale;
                var sr = PixelArt.AddRenderer(tile, sprite);
                // Hidden blocks and the boss room's gate show faintly in the editor so you know where they are.
                if (c == 'H' || c == '|') sr.color = new Color(1f, 1f, 1f, 0.35f);
            }
    }

    static void Clear()
    {
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            if (go != null && go.name == RootName && go.transform.parent == null)
                Object.DestroyImmediate(go);
    }
}
