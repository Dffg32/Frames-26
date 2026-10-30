using UnityEditor;
using UnityEngine;

// Shows the level's sprites in the Scene view while not playing, so the scene doesn't look empty.
// The preview is never saved into the scene and is removed when Play starts (the real level is built then).
[InitializeOnLoad]
static class LevelPreview
{
    const string RootName = "Vista previa del nivel (solo editor)";

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

    [MenuItem("Juegardo/Actualizar vista previa del nivel")]
    static void Build()
    {
        Clear();
        var root = new GameObject(RootName) { hideFlags = HideFlags.DontSave | HideFlags.NotEditable };
        var map = Game.Map;
        for (int y = 0; y < map.Length; y++)
            for (int x = 0; x < map[y].Length; x++)
            {
                Sprite sprite = map[y][x] switch
                {
                    '#' => y > 0 && x < map[y - 1].Length && map[y - 1][x] == '#' ? PixelArt.Dirt : PixelArt.Grass,
                    'B' => PixelArt.Stone,
                    '!' => PixelArt.Bonus,
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
                tile.transform.position = new Vector2(x, map.Length - 1 - y);
                PixelArt.AddRenderer(tile, sprite);
            }
    }

    static void Clear()
    {
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            if (go != null && go.name == RootName && go.transform.parent == null)
                Object.DestroyImmediate(go);
    }
}
