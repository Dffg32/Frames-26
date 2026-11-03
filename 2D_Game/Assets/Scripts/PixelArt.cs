using System.Collections.Generic;
using UnityEngine;

// Builds the game's pixel-art sprites from text, so no image assets are needed.
// Each letter is a color from the palette; '.' (or a missing char) is transparent.
public static class PixelArt
{
    static readonly Dictionary<char, Color32> Palette = new()
    {
        ['R'] = new Color32(220, 40, 50, 255),   // bandana
        ['S'] = new Color32(255, 200, 150, 255), // skin
        ['K'] = new Color32(25, 20, 35, 255),    // outline / dark
        ['M'] = new Color32(150, 40, 40, 255),   // mouth
        ['B'] = new Color32(50, 90, 220, 255),   // blue clothes
        ['Y'] = new Color32(255, 210, 40, 255),  // yellow
        ['O'] = new Color32(230, 130, 20, 255),  // orange
        ['W'] = new Color32(255, 255, 255, 255), // white
        ['G'] = new Color32(90, 205, 75, 255),   // grass
        ['g'] = new Color32(40, 140, 50, 255),   // dark grass
        ['D'] = new Color32(165, 105, 55, 255),  // dirt / wood
        ['d'] = new Color32(110, 65, 30, 255),   // dark dirt
        ['A'] = new Color32(150, 150, 165, 255), // stone
        ['a'] = new Color32(80, 80, 95, 255),    // dark stone
        ['L'] = new Color32(205, 205, 215, 255), // light stone / cloud shade
        ['P'] = new Color32(165, 70, 205, 255),  // slime
        ['p'] = new Color32(100, 40, 140, 255),  // dark slime
        ['C'] = new Color32(90, 230, 240, 255),  // gem
        ['c'] = new Color32(30, 130, 170, 255),  // dark gem
    };

    static readonly string[] HeroTop =
    {
        "................",
        ".....RRRRRR.....",
        "....RRRRRRRRRR..",
        "....RRRRRRR.RR..",
        "....SSSSSSS.....",
        "...SSKSSSKSS....",
        "...SSKSSSKSS....",
        "...SSSSSSSSS....",
        "....SSSMMSSS....",
        ".....SSSSSS.....",
        "...BBBBBBBBBB...",
        "..SBBBYBBYBBBS..",
        "..SBBBBBBBBBBS..",
        "....BBBBBBBB....",
    };

    static readonly string[] BonusArt =
    {
        "KKKKKKKKKKKKKKKK",
        "KYYYYYYYYYYYYYYK",
        "KYOYYYYYYYYYYOYK",
        "KYYYYYYWWYYYYYYK",
        "KYYYYYYWWYYYYYYK",
        "KYYYYYYWWYYYYYYK",
        "KYYYYYYWWYYYYYYK",
        "KYYYYYYWWYYYYYYK",
        "KYYYYYYWWYYYYYYK",
        "KYYYYYYYYYYYYYYK",
        "KYYYYYYWWYYYYYYK",
        "KYYYYYYWWYYYYYYK",
        "KYYYYYYYYYYYYYYK",
        "KYOYYYYYYYYYYOYK",
        "KYYYYYYYYYYYYYYK",
        "KKKKKKKKKKKKKKKK",
    };

    static readonly string[] DirtRows =
    {
        "DDDDDDDDDDDDDDDD",
        "DDDdDDDDDDDDdDDD",
        "DDDDDDDdDDDDDDDD",
        "DdDDDDDDDDDDDDdD",
    };

    // Big hero: same head, taller body and legs (16x32).
    static readonly string[] HeroBigTop = Concat(HeroTop[..10],
        "...BBBBBBBBBB...",
        "..BBBBBBBBBBBB..",
        "..BBBBYBBYBBBB..",
        ".SBBBBBBBBBBBBS.",
        ".SBBBBBBBBBBBBS.",
        ".SSBBBBBBBBBBSS.",
        ".SS.BBBBBBBB.SS.",
        "....BBBBBBBB....",
        "....BBBBBBBB....",
        "....KKKKKKKK....",
        "....BBBBBBBB....",
        "....BBBBBBBB....");

    static Sprite hero, heroWalk, heroBig, heroBigWalk, grass, dirt, stone, bonus, usedBonus, plank, coin, slime, slimeFlat, gem, cloud, berry, brickBit;
    static Material material;

    // Hero sprites have their pivot at the feet, so growing or shrinking keeps them on the ground.
    static readonly Vector2 Feet = new(0.5f, 0f);

    public static Sprite Hero => hero ? hero : hero = Make(Concat(HeroTop, "....KKK..KKK....", "...KKKK..KKKK..."), Feet);
    public static Sprite HeroWalk => heroWalk ? heroWalk : heroWalk = Make(Concat(HeroTop, "...KKK....KKK...", "..KKK......KKK.."), Feet);

    public static Sprite HeroBig => heroBig ? heroBig : heroBig = Make(Concat(Concat(HeroBigTop, Repeat(new[] { "....BBB..BBB...." }, 8)),
        "...KKKK..KKKK...",
        "..KKKKK..KKKKK.."), Feet);

    public static Sprite HeroBigWalk => heroBigWalk ? heroBigWalk : heroBigWalk = Make(Concat(Concat(HeroBigTop,
        Repeat(new[] { "...BBB....BBB..." }, 4)), Concat(Repeat(new[] { "..BBB......BBB.." }, 4),
        "..KKK......KKK..",
        ".KKKK......KKKK.")), Feet);

    // The grow power-up: a red berry with a leaf.
    public static Sprite Berry => berry ? berry : berry = Make(new[]
    {
        "",
        "........gG......",
        ".......gG.......",
        ".....RRgRRR.....",
        "...RRRRRRRRRR...",
        "..RRWWRRRRRRRR..",
        "..RWWRRRRRRRRR..",
        ".RRWRRRRRRRRRRR.",
        ".RRRRRRRRRRRRRR.",
        ".RRRRRRRRRRRRRR.",
        ".RRRRRRRRRRRRMR.",
        "..RRRRRRRRRRMR..",
        "..RRRRRRRRRMMR..",
        "...RRRRRRRRRR...",
        "....MMRRRRMM....",
        "",
    });

    // A piece of a broken stone block.
    public static Sprite BrickBit => brickBit ? brickBit : brickBit = Make(new[] { "aAAa", "ALAa", "AAAa", "aaaa" });

    public static Sprite Grass => grass ? grass : grass = Make(Concat(new[]
    {
        "GGGGGGGGGGGGGGGG",
        "GGgGGGGGGgGGGGGG",
        "gGggGGgGggGGgGgg",
        "DgDDgDDDDgDDDgDD",
    }, Repeat(DirtRows, 3)));

    public static Sprite Dirt => dirt ? dirt : dirt = Make(Repeat(DirtRows, 4));

    public static Sprite Stone => stone ? stone : stone = Make(new[]
    {
        "aaaaaaaaaaaaaaaa",
        "aLLLLLLaLLLLLLLa",
        "aLAAAAAaLAAAAAAa",
        "aLAAAAAaLAAAAAAa",
        "aLAAAAAaLAAAAAAa",
        "aLAAAAAaLAAAAAAa",
        "aLAAAAAaLAAAAAAa",
        "aaaaaaaaaaaaaaaa",
        "aLLLaLLLLLLLaLLa",
        "aLAAaLAAAAAAaLAa",
        "aLAAaLAAAAAAaLAa",
        "aLAAaLAAAAAAaLAa",
        "aLAAaLAAAAAAaLAa",
        "aLAAaLAAAAAAaLAa",
        "aLAAaLAAAAAAaLAa",
        "aaaaaaaaaaaaaaaa",
    });

    public static Sprite Bonus => bonus ? bonus : bonus = Make(BonusArt);

    public static Sprite UsedBonus => usedBonus ? usedBonus : usedBonus = Make(System.Array.ConvertAll(
        BonusArt, row => row.Replace('Y', 'D').Replace('W', 'D').Replace('O', 'd')));

    public static Sprite Plank => plank ? plank : plank = Make(Concat(new[]
    {
        "OOOOOOOOOOOOOOOO",
        "ODDDDDDDDDDDDDDO",
        "DDdDDDDDDDdDDDDD",
        "dddddddddddddddd",
        "..d..........d..",
        "..d..........d..",
    }, Repeat(new[] { "" }, 10)));

    public static Sprite Coin => coin ? coin : coin = Make(new[]
    {
        "", "",
        "......KKKK......",
        ".....KYYYYK.....",
        "....KYWYYYOK....",
        "....KYWYYYOK....",
        "....KYWYYYOK....",
        "....KYWYYYOK....",
        "....KYWYYYOK....",
        "....KYWYYYOK....",
        "....KYYYYYOK....",
        ".....KYYYOK.....",
        "......KKKK......",
        "", "", "",
    });

    public static Sprite Slime => slime ? slime : slime = Make(new[]
    {
        "", "", "", "", "",
        "......PPPP......",
        "....PPPPPPPP....",
        "...PPPPPPPPPP...",
        "..PPPWWPPPWWPP..",
        "..PPPWKPPPWKPP..",
        ".PPPPWKPPPWKPPP.",
        ".PPPPPPPPPPPPPP.",
        ".PPPPPPPPPPPPPP.",
        ".PPPpppppppPPPP.",
        "pPPPPPPPPPPPPPPp",
        "pppppppppppppppp",
    });

    public static Sprite SlimeFlat => slimeFlat ? slimeFlat : slimeFlat = Make(Concat(Repeat(new[] { "" }, 13),
        ".PPPPPPPPPPPPPP.",
        "pPPPKPPPPPPKPPPp",
        "pppppppppppppppp"));

    public static Sprite Gem => gem ? gem : gem = Make(new[]
    {
        "",
        "....cccccccc....",
        "...cCCWCCCCCc...",
        "..cCCWCCCCCCCc..",
        ".cCCWCCCCCCCCCc.",
        "cccccccccccccccc",
        ".cCCCCCCCCCCCCc.",
        "..cCCCCCCCCCCc..",
        "...cCCCCCCCCc...",
        "....cCCCCCCc....",
        ".....cCCCCc.....",
        "......cCCc......",
        ".......cc.......",
        "", "", "",
    });

    public static Sprite Cloud => cloud ? cloud : cloud = Make(new[]
    {
        "", "", "", "",
        ".....WWWW.......",
        "...WWWWWWWW.....",
        "..WWWWWWWWWWWW..",
        ".WWWWWWWWWWWWWW.",
        "WWWWWWWWWWWWWWWW",
        "WWWWWWWWWWWWWWWW",
        ".LLLLLLLLLLLLLL.",
        "", "", "", "", "",
    });

    public static SpriteRenderer AddRenderer(GameObject go, Sprite sprite, int order = 0)
    {
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        // Unlit so sprites look the same whether or not the scene has 2D lights.
        if (material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader != null) material = new Material(shader);
        }
        if (material != null) sr.sharedMaterial = material;
        return sr;
    }

    static Sprite Make(string[] rows) => Make(rows, new Vector2(0.5f, 0.5f));

    static Sprite Make(string[] rows, Vector2 pivot)
    {
        int h = rows.Length, w = 0;
        foreach (var r in rows) w = Mathf.Max(w, r.Length);
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                char c = x < rows[y].Length ? rows[y][x] : '.';
                pixels[(h - 1 - y) * w + x] = Palette.TryGetValue(c, out var col) ? col : new Color32(0, 0, 0, 0);
            }
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), pivot, 16);
    }

    static string[] Concat(string[] a, params string[] b)
    {
        var result = new string[a.Length + b.Length];
        a.CopyTo(result, 0);
        b.CopyTo(result, a.Length);
        return result;
    }

    static string[] Repeat(string[] rows, int times)
    {
        var result = new List<string>();
        for (int i = 0; i < times; i++) result.AddRange(rows);
        return result.ToArray();
    }
}
