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
        ['N'] = new Color32(240, 212, 135, 255), // sand
        ['n'] = new Color32(212, 170, 95, 255),  // dark sand
        ['t'] = new Color32(170, 120, 60, 255),  // sandstone
        ['U'] = new Color32(85, 95, 125, 255),   // cave rock
        ['u'] = new Color32(55, 60, 85, 255),    // dark cave rock
        ['V'] = new Color32(120, 105, 130, 255), // castle brick
        ['v'] = new Color32(85, 70, 95, 255),    // dark castle brick
        ['k'] = new Color32(45, 40, 55, 255),    // mortar
    };

    // How each world recolors the meadow's ground: grass, dark grass, dirt, dark dirt.
    static readonly Dictionary<Theme, string> GroundColors = new()
    {
        [Theme.Meadow] = "GgDd",
        [Theme.Desert] = "Nnnt",
        [Theme.Cave] = "gUUu",
        [Theme.Castle] = "Vvvk",
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

    static readonly string[] HeroBigRows = Concat(Concat(HeroBigTop, Repeat(new[] { "....BBB..BBB...." }, 8)),
        "...KKKK..KKKK...",
        "..KKKKK..KKKKK..");

    static readonly string[] HeroBigWalkRows = Concat(Concat(HeroBigTop,
        Repeat(new[] { "...BBB....BBB..." }, 4)), Concat(Repeat(new[] { "..BBB......BBB.." }, 4),
        "..KKK......KKK..",
        ".KKKK......KKKK."));

    static Sprite hero, heroWalk, heroBig, heroBigWalk, heroFire, heroFireWalk, lava, lavaDeep, stone, bonus, usedBonus, plank, coin,
        slime, slimeFlat, spiky, bat, gem, cloud, berry, pepper, star, fireball, brickBit,
        frog, frogJump, cactus, seed, ghost, ember, shockwave, boss;
    static Material material;

    // Hero sprites have their pivot at the feet, so growing or shrinking keeps them on the ground.
    static readonly Vector2 Feet = new(0.5f, 0f);

    public static Sprite Hero => hero ? hero : hero = Make(Concat(HeroTop, "....KKK..KKK....", "...KKKK..KKKK..."), Feet);
    public static Sprite HeroWalk => heroWalk ? heroWalk : heroWalk = Make(Concat(HeroTop, "...KKK....KKK...", "..KKK......KKK.."), Feet);
    public static Sprite HeroBig => heroBig ? heroBig : heroBig = Make(HeroBigRows, Feet);
    public static Sprite HeroBigWalk => heroBigWalk ? heroBigWalk : heroBigWalk = Make(HeroBigWalkRows, Feet);

    // Fire hero: red clothes, white bandana, orange buttons.
    public static Sprite HeroFire => heroFire ? heroFire : heroFire = Make(Recolor(HeroBigRows, "BRY", "RWO"), Feet);
    public static Sprite HeroFireWalk => heroFireWalk ? heroFireWalk : heroFireWalk = Make(Recolor(HeroBigWalkRows, "BRY", "RWO"), Feet);

    // Fire power-up: an orange chili pepper.
    public static Sprite Pepper => pepper ? pepper : pepper = Make(new[]
    {
        "",
        ".........gg.....",
        "........gG......",
        ".......OOO......",
        "......OOOOO.....",
        ".....OOYOOOO....",
        ".....OYOOOOO....",
        "....OOYOOOOO....",
        "....OOOOOOOO....",
        "...OOOOOOOOR....",
        "...OOOOOOOR.....",
        "..OOOOOOOR......",
        "..OOOOOOR.......",
        ".OOOOOR.........",
        ".OOR............",
        "",
    });

    // Invincibility power-up: a smiling gold star.
    public static Sprite Star => star ? star : star = Make(new[]
    {
        "",
        ".......OO.......",
        "......OYYO......",
        "......OYYO......",
        ".....OYWYYO.....",
        "OOOOOYWYYYYOOOOO",
        ".OYYYWYYYYYYYYO.",
        "..OYYYKYYKYYYO..",
        "...OYYKYYKYYO...",
        "....OYYYYYYO....",
        "....OYYYYYYO....",
        "...OYYYOOYYYO...",
        "...OYYO..OYYO...",
        "..OYYO....OYYO..",
        "..OOO......OOO..",
        "",
    });

    public static Sprite Fireball =>fireball ? fireball : fireball = Make(new[]
    {
        "..OOOO..",
        ".OYYYYO.",
        "OYYWWYYO",
        "OYWWWWYO",
        "OYWWWWYO",
        "OYYWWYYO",
        ".OYYYYO.",
        "..OOOO..",
    });

    // A hedgehog covered in spikes: it can't be stomped. Faces right.
    public static Sprite Spiky => spiky ? spiky : spiky = Make(new[]
    {
        "", "", "", "", "",
        ".....A.A.A......",
        "...A.AAAAAA.A...",
        "..AAaAAaAAaAA...",
        ".AaAAaAAaAASSS..",
        "AAAaAAaAAaSSSKS.",
        ".AaAAaAAaASSSSSK",
        "AAAAAAAAAASSSSS.",
        ".aaaaaaaaaSSSS..",
        "..aaaaaaaaaSS...",
        "...KK....KK.....",
        "..KKK...KKK.....",
    });

    // A bat that flies back and forth.
    public static Sprite Bat => bat ? bat : bat = Make(new[]
    {
        "", "", "",
        "......p..p......",
        "......pppp......",
        "KK...pWppWp...KK",
        "KpKK.pKppKp.KKpK",
        "KppppppppppppppK",
        ".KppppppppppppK.",
        "..KpppKppKpppK..",
        "...KK.W..W.KK...",
        "", "", "", "", "",
    });

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

    static readonly string[] GrassRows = Concat(new[]
    {
        "GGGGGGGGGGGGGGGG",
        "GGgGGGGGGgGGGGGG",
        "gGggGGgGggGGgGgg",
        "DgDDgDDDDgDDDgDD",
    }, Repeat(DirtRows, 3));

    static readonly Dictionary<(Theme, bool), Sprite> ground = new();

    // A ground tile in the colors of the given world: the top one (grass, sand...) or the fill below it.
    public static Sprite Ground(Theme theme, bool top)
    {
        if (ground.TryGetValue((theme, top), out var sprite) && sprite) return sprite;
        return ground[(theme, top)] = Make(Recolor(top ? GrassRows : Repeat(DirtRows, 4), "GgDd", GroundColors[theme]));
    }


    // Lava: a bubbling surface and the glowing fill under it.
    public static Sprite Lava => lava ? lava : lava = Make(Concat(new[]
    {
        "", "", "",
        "..YY......YY....",
        ".YOOY....YOOY...",
        "YOOOOYYYYOOOOYYY",
        "OOROOOOOOOOROOOO",
        "OOOOOOROOOOOOOOR",
    }, Repeat(new[] { "ROOORRRROOORRRRO", "RRRRROOORRRRROOR" }, 4)));

    public static Sprite LavaDeep => lavaDeep ? lavaDeep : lavaDeep = Make(Repeat(new[] { "RRRORRRRRRRORRRR", "RRRRRRORRRRRRROR" }, 8));

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

    // A frog that hops toward the player. Faces left.
    static readonly string[] FrogHead =
    {
        "...WW....WW.....",
        "..WKKW..WKKW....",
        "..WKWW..WKWW....",
        ".GGGGGGGGGGGG...",
        "GGGGGGGGGGGGGG..",
        "KKKKKGGGGGGGGGG.",
        ".GYYYYYYYGGGGGG.",
        ".GYYYYYYYYGGGgG.",
        "..GgGGGGGGGgGg..",
    };

    public static Sprite Frog => frog ? frog : frog = Make(Concat(Concat(Repeat(new[] { "" }, 5), FrogHead),
        ".gg.gg...gg.gg..",
        ""));

    public static Sprite FrogJump => frogJump ? frogJump : frogJump = Make(Concat(Concat(Repeat(new[] { "" }, 3), FrogHead),
        "...g.g....g..g..",
        "..gg.gg...gg.gg.",
        "..g...g....g..g.",
        ""));

    // A cactus in a pot that spits seeds at the player.
    public static Sprite Cactus => cactus ? cactus : cactus = Make(new[]
    {
        "",
        "......G..G......",
        ".....GGGGGG.....",
        "....GGgGGgGG....",
        "..G.GKWGGKWG.G..",
        ".GG.GKKGGKKG.GG.",
        ".GG.GGGGGGGG.GG.",
        ".GGGGGKKKKGGGGG.",
        "..GGGKMMMMKGGG..",
        "....GGKKKKGG....",
        "....GgGGGGgG....",
        "....GGGgGGGG....",
        "....GgGGGgGG....",
        "...DDDDDDDDDD...",
        "...dDDDDDDDDd...",
        "....dddddddd....",
    });

    public static Sprite Seed => seed ? seed : seed = Make(new[] { "..gg..", ".gGGg.", "gGGWGg", "gGGGGg", ".gGGg.", "..gg.." });

    // A ghost that floats through walls toward the player. Faces left.
    public static Sprite Ghost => ghost ? ghost : ghost = Make(new[]
    {
        "", "",
        ".....WWWWWW.....",
        "...WWWWWWWWWW...",
        "..WWWWWWWWWWWW..",
        "..WKKWWWWKKWWW..",
        ".WWKKWWWWKKWWWW.",
        ".WWWWWWWWWWWWWW.",
        ".WWWMMMMWWWWWWW.",
        ".WWWMMMMWWWWWWW.",
        ".WWWWWWWWWWWWWW.",
        ".LWWWWWWWWWWWWL.",
        ".LLWWWLWWLWWWLL.",
        ".L.LLL.LL.LLL.L.",
        "", "",
    });

    // A living spark that leaps out of the lava.
    public static Sprite Ember => ember ? ember : ember = Make(new[]
    {
        "",
        ".......Y........",
        "......YOY.......",
        ".....YOOOY......",
        "....YOOYOOY.....",
        "...YOOYWYOOY....",
        "...OOYWWWYOO....",
        "..OOYWWWWWYOO...",
        "..OOYKWWWKYOO...",
        "..OOYKWWWKYOO...",
        "..OOOYWWWYOOO...",
        "...OOOYYYOOO....",
        "....ROOOOOR.....",
        ".....RRRRR......",
        "", "",
    });

    // The wave of fire the boss sends along the floor when it lands.
    public static Sprite Shockwave => shockwave ? shockwave : shockwave = Make(Concat(Repeat(new[] { "" }, 8),
        "......Y.........",
        ".....YOY....Y...",
        "..Y..YOOY..YOY..",
        ".YOY.YOOOYYOOY..",
        "YOOOYOOOOOOOOOY.",
        "OOROOOOROOOOROOO",
        "ORRROOORRRORRROO",
        "RRRRRRRRRRRRRRRR"));

    // The final boss: a crowned stone golem with a molten core (32x32, pivot at the feet).
    public static Sprite Boss => boss ? boss : boss = Make(Mirror(new[]
    {
        "................",
        "........Y.....Y.",
        "........YY...YYY",
        "........YYYYYYYY",
        "........YOYYYYRY",
        "......KKKKKKKKKK",
        ".....KLLLLLLLLLL",
        "....KLAAAAAAAAAA",
        "....KAAKKKKAAAAA",
        "....KAKOYYKAAAAA",
        "....KAAKKKKAAAAA",
        "....KaAAAAAAAAAA",
        "....KaAKKKKKKKKK",
        "....KaAKRORORORO",
        ".....KaKKKKKKKKK",
        "..KKKKKaaaaaaaaa",
        ".KAAAAKKKKKKKKKK",
        "KALAAKAAAAAAAAAA",
        "KAAAAKAAAAKKKKKK",
        "KAAAAKAAAKOOOOOO",
        "KAAAAKAAAKOYYYYY",
        "KAAAAKAAAKOYYWWW",
        "KaAAAKAAAKOYYYYY",
        "KaAAAKAAAKOOOOOO",
        "KKKKKKaAAAKKKKKK",
        ".KaaKKaAAAAAAAAA",
        "..KK.Kaaaaaaaaaa",
        ".....KKKKKKKKKKK",
        ".....KAAAAAK....",
        ".....KAAAAAK....",
        "....KaaaaaaaK...",
        "....KKKKKKKKK...",
    }), Feet);

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

    // Swaps colors: every char in `from` becomes the char at the same index in `to`.
    static string[] Recolor(string[] rows, string from, string to) => System.Array.ConvertAll(rows, row =>
    {
        var chars = row.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            int index = from.IndexOf(chars[i]);
            if (index >= 0) chars[i] = to[index];
        }
        return new string(chars);
    });

    // Builds a symmetric picture from its left half (16 chars per row).
    static string[] Mirror(string[] leftHalves) => System.Array.ConvertAll(leftHalves, half =>
    {
        var left = half.PadRight(16, '.').Substring(0, 16).ToCharArray();
        var right = (char[])left.Clone();
        System.Array.Reverse(right);
        return new string(left) + new string(right);
    });

    static string[] Repeat(string[] rows, int times)
    {
        var result = new List<string>();
        for (int i = 0; i < times; i++) result.AddRange(rows);
        return result.ToArray();
    }
}
