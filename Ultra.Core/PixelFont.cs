using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ultra.Core;

/// <summary>
/// A 6x8 character-cell bitmap font in the style of the Oric ROM character set.
/// Glyphs are 5x7 pixels stored as one hex byte per row.
/// </summary>
public sealed class PixelFont
{
    public const int CellWidth = 6;
    public const int CellHeight = 8;

    private static readonly Dictionary<char, string> Glyphs = new()
    {
        [' '] = "00000000000000",
        ['0'] = "0E11131519110E", ['1'] = "040C040404040E", ['2'] = "0E11010204081F",
        ['3'] = "1F02040201110E", ['4'] = "02060A121F0202", ['5'] = "1F101E0101110E",
        ['6'] = "0608101E11110E", ['7'] = "1F010204080808", ['8'] = "0E11110E11110E",
        ['9'] = "0E11110F01020C",
        ['A'] = "0E1111111F1111", ['B'] = "1E11111E11111E", ['C'] = "0E11101010110E",
        ['D'] = "1C12111111121C", ['E'] = "1F10101E10101F", ['F'] = "1F10101E101010",
        ['G'] = "0E11101711110F", ['H'] = "1111111F111111", ['I'] = "0E04040404040E",
        ['J'] = "0702020202120C", ['K'] = "11121418141211", ['L'] = "1010101010101F",
        ['M'] = "111B1515111111", ['N'] = "11111915131111", ['O'] = "0E11111111110E",
        ['P'] = "1E11111E101010", ['Q'] = "0E11111115120D", ['R'] = "1E11111E141211",
        ['S'] = "0F10100E01011E", ['T'] = "1F040404040404", ['U'] = "1111111111110E",
        ['V'] = "11111111110A04", ['W'] = "1111111515150A", ['X'] = "11110A040A1111",
        ['Y'] = "1111110A040404", ['Z'] = "1F01020408101F",
        ['.'] = "00000000000C0C", [','] = "000000000C0408", [':'] = "000C0C000C0C00",
        ['-'] = "0000001F000000", ['!'] = "04040404040004", ['?'] = "0E110102040004",
        ['('] = "02040808080402", [')'] = "08040202020408", ['/'] = "00010204081000",
        ['\''] = "0C040800000000", ['<'] = "02040810080402", ['>'] = "08040201020408",
        ['='] = "00001F001F0000", ['*'] = "0004150E150400", ['+'] = "0004041F040400",
        ['#'] = "0A0A1F0A1F0A0A", ['_'] = "0000000000001F",
        ['@'] = "0E11171517110E", // rendered as a copyright-style mark
    };

    private readonly Texture2D _texture;
    private readonly Dictionary<char, Rectangle> _sources = new();

    public PixelFont(GraphicsDevice device)
    {
        var chars = new List<char>(Glyphs.Keys);
        int width = chars.Count * CellWidth;
        var data = new Color[width * CellHeight];

        for (int i = 0; i < chars.Count; i++)
        {
            string hex = Glyphs[chars[i]];
            for (int row = 0; row < 7; row++)
            {
                int bits = Convert.ToInt32(hex.Substring(row * 2, 2), 16);
                for (int col = 0; col < 5; col++)
                {
                    if ((bits & (0x10 >> col)) != 0)
                        data[row * width + i * CellWidth + col] = Color.White;
                }
            }

            _sources[chars[i]] = new Rectangle(i * CellWidth, 0, CellWidth, CellHeight);
        }

        _texture = new Texture2D(device, width, CellHeight);
        _texture.SetData(data);
    }

    public static int Measure(string text, float scale = 1f) => (int)(text.Length * CellWidth * scale);

    public void Draw(SpriteBatch batch, string text, Vector2 position, Color color, float scale = 1f)
    {
        float x = position.X;
        foreach (char raw in text)
        {
            char c = char.ToUpperInvariant(raw);
            if (_sources.TryGetValue(c, out var src))
                batch.Draw(_texture, new Vector2(x, position.Y), src, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            x += CellWidth * scale;
        }
    }

    /// <summary>
    /// Draws text with each pixel row tinted separately, mimicking Oric per-scanline colour attributes.
    /// </summary>
    public void DrawBanded(SpriteBatch batch, string text, Vector2 position, Color[] rowColors, int offset, float scale)
    {
        float x = position.X;
        foreach (char raw in text)
        {
            char c = char.ToUpperInvariant(raw);
            if (_sources.TryGetValue(c, out var src))
            {
                for (int row = 0; row < 7; row++)
                {
                    var rowSrc = new Rectangle(src.X, row, CellWidth, 1);
                    var color = rowColors[(row + offset) % rowColors.Length];
                    batch.Draw(_texture, new Vector2(x, position.Y + row * scale), rowSrc, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
            }
            x += CellWidth * scale;
        }
    }
}
