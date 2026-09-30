using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ultra.Core;

/// <summary>
/// All game graphics are defined here as pixel strings ('X' = ink) and turned into
/// white textures at start-up, then tinted with an Oric palette colour when drawn.
/// </summary>
public sealed class SpriteArt
{
    public const int AlienTypes = 16;

    // Each alien: 6 rows of body, then two alternative 2-row "leg" rows for the animation frames.
    private static readonly string[][] Aliens =
    {
        new[] { "..X......X..", "...X....X...", "..XXXXXXXX..", ".XX.XXXX.XX.", "XXXXXXXXXXXX", "X.XXXXXXXX.X",
                "X.X......X.X", "...XX..XX...", "..X......X..", ".X........X." },
        new[] { "....XXXX....", "..XXXXXXXX..", ".XXXXXXXXXX.", ".XX..XX..XX.", ".XXXXXXXXXX.", "...XX..XX...",
                "..XX.XX.XX..", "XX........XX", "..X..XX..X..", ".X.X....X.X." },
        new[] { "X....XX....X", "XX..XXXX..XX", "XXX.X..X.XXX", ".XXXXXXXXXX.", "..XXXXXXXX..", "....XXXX....",
                "...X....X...", "..X......X..", "....X..X....", "....X..X...." },
        new[] { "...XXXXXX...", ".XXXXXXXXXX.", "XXX..XX..XXX", "XXX.XXXX.XXX", "XXXXXXXXXXXX", ".XXXXXXXXXX.",
                "..X.X..X.X..", ".X...XX...X.", "...X.XX.X...", "..X..XX..X.." },
        new[] { ".....XX.....", "....XXXX....", "...XX..XX...", "..XX.XX.XX..", ".XX.XXXX.XX.", "..XX.XX.XX..",
                "...XX..XX...", "....XXXX....", "...X.XX.X...", "....X..X...." },
        new[] { "X..........X", "XX...XX...XX", "XXX.XXXX.XXX", "XXXXX..XXXXX", ".XXXXXXXXXX.", "..XXX..XXX..",
                "...X....X...", "............", "..X......X..", ".X........X." },
        new[] { "...XXXXXX...", "..XXXXXXXX..", ".XX.XXXX.XX.", ".XXXXXXXXXX.", ".X.X.XX.X.X.", ".X.X.XX.X.X.",
                "X.X..XX..X.X", "..X......X..", ".X.X.XX.X.X.", ".X...XX...X." },
        new[] { ".....XX.....", "....XXXX....", "...XXXXXX...", "..XX.XX.XX..", "..XXXXXXXX..", ".XXXXXXXXXX.",
                "XX.X.XX.X.XX", "X..X....X..X", "XX.X.XX.X.XX", "...X....X..." },
        new[] { "...XXXXXX...", ".XXXXXXXXXX.", "XX.XXXXXX.XX", "XXXXXXXXXXXX", "XXXXXXXXXXXX", ".XXXXXXXXXX.",
                "..X..XX..X..", ".X...XX...X.", ".X...XX...X.", "..X..XX..X.." },
        new[] { ".....XX.....", ".....XX.....", "..X.XXXX.X..", "...XXXXXX...", "XXXXX..XXXXX", "...XXXXXX...",
                "..X.XXXX.X..", ".....XX.....", ".X..XXXX..X.", "X....XX....X" },
        new[] { ".....XX.....", "....XXXX....", "...XXXXXX...", "..XX.XX.XX..", "..XXXXXXXX..", "...XXXXXX...",
                "....XXXX....", ".....XX.....", "...X.XX.X...", "....X..X...." },
        new[] { "....XXXX....", "...XXXXXX...", "..XXXXXXXX..", "..X.XXXX.X..", ".XXXXXXXXXX.", "XXXXXXXXXXXX",
                ".....XX.....", ".....XX.....", "....X..X....", "...X....X..." },
        new[] { "...XXXXXX...", "..XXXXXXXX..", ".XX..XX..XX.", ".XX..XX..XX.", ".XXXXXXXXXX.", ".XXXXXXXXXX.",
                ".XX.XX.XX.X.", ".X...X...X..", ".X.XX.XX.XX.", "..X...X...X." },
        new[] { "X....XX....X", "X...XXXX...X", "XX.XX..XX.XX", "XXXXXXXXXXXX", "XX..XXXX..XX", "X....XX....X",
                ".....XX.....", "....X..X....", "....XXXX....", "...X....X..." },
        new[] { "...XXXXXX...", "..XXXXXXXX..", ".XX..XX..XX.", ".XXXXXXXXXX.", "..XXX..XXX..", "...XXXXXX...",
                "...X.XX.X...", "............", "....X..X....", "...X.XX.X..." },
        new[] { "X.XXXXXXXX.X", "XXX.XXXX.XXX", "XXXXXXXXXXXX", ".XX.X..X.XX.", "..XXXXXXXX..", ".X.X.XX.X.X.",
                "X..X....X..X", ".X........X.", ".X.X....X.X.", "X..........X" },
    };

    private static readonly string[] PlayerRows =
    {
        ".......X.......",
        "......XXX......",
        "......XXX......",
        "..X..XXXXX..X..",
        "..X.XXXXXXX.X..",
        ".XXXXX.X.XXXXX.",
        "XXXXXXXXXXXXXXX",
        "XX..XX...XX..XX",
        "X....X...X....X",
    };

    private static readonly string[] Boom1 =
    {
        "....X..X....", ".X...XX...X.", "...X.XX.X...", "..XXX..XXX..",
        "...X.XX.X...", ".X...XX...X.", "....X..X....", "............",
    };

    private static readonly string[] Boom2 =
    {
        "X....X....X.", "..X......X..", "X...X..X...X", "............",
        "X...X..X...X", "..X......X..", "X....X....X.", "............",
    };

    private static readonly string[] Bomb1 = { ".X.", "X..", ".X.", "..X", ".X." };
    private static readonly string[] Bomb2 = { ".X.", "..X", ".X.", "X..", ".X." };

    public Texture2D Pixel { get; }
    public Texture2D Player { get; }
    public Texture2D[,] Alien { get; } = new Texture2D[AlienTypes, 2];
    public Texture2D[] Explosion { get; } = new Texture2D[2];
    public Texture2D[] Bomb { get; } = new Texture2D[2];
    public Texture2D Triangle { get; }
    public Texture2D Circle { get; }

    public SpriteArt(GraphicsDevice device)
    {
        Pixel = new Texture2D(device, 1, 1);
        Pixel.SetData(new[] { Color.White });

        Player = Build(device, PlayerRows);
        for (int i = 0; i < AlienTypes; i++)
        {
            var a = Aliens[i];
            Alien[i, 0] = Build(device, new[] { a[0], a[1], a[2], a[3], a[4], a[5], a[6], a[7] });
            Alien[i, 1] = Build(device, new[] { a[0], a[1], a[2], a[3], a[4], a[5], a[8], a[9] });
        }

        Explosion[0] = Build(device, Boom1);
        Explosion[1] = Build(device, Boom2);
        Bomb[0] = Build(device, Bomb1);
        Bomb[1] = Build(device, Bomb2);
        Triangle = BuildTriangle(device, 96);
        Circle = BuildCircle(device, 128);
    }

    private static Texture2D Build(GraphicsDevice device, string[] rows)
    {
        int w = rows[0].Length, h = rows.Length;
        var data = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                data[y * w + x] = rows[y][x] == 'X' ? Color.White : Color.Transparent;

        var tex = new Texture2D(device, w, h);
        tex.SetData(data);
        return tex;
    }

    // Right-pointing triangle; flipped horizontally when drawn for "left".
    private static Texture2D BuildTriangle(GraphicsDevice device, int size)
    {
        var data = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            float half = size / 2f;
            float span = (1f - Math.Abs(y + 0.5f - half) / half) * size;
            for (int x = 0; x < size; x++)
                data[y * size + x] = x < span ? Color.White : Color.Transparent;
        }

        var tex = new Texture2D(device, size, size);
        tex.SetData(data);
        return tex;
    }

    private static Texture2D BuildCircle(GraphicsDevice device, int size)
    {
        var data = new Color[size * size];
        float r = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r;
                float d = MathF.Sqrt(dx * dx + dy * dy);
                float a = Math.Clamp(r - d, 0f, 1f);
                data[y * size + x] = Color.White * a;
            }

        var tex = new Texture2D(device, size, size);
        tex.SetData(data);
        return tex;
    }
}
