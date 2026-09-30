using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ultra.Core;

/// <summary>
/// Thin drawing helper around a SpriteBatch working in the 240x224 virtual Oric screen.
/// </summary>
public sealed class Renderer
{
    public const int Width = 240;
    public const int Height = 224;

    /// <summary>
    /// The virtual screen is rendered at this multiple of 240x224 so sprites can be drawn at
    /// 1.5x while every Oric pixel stays a whole number of render-target pixels.
    /// </summary>
    public const int Supersample = 2;

    public SpriteBatch Batch { get; }
    public PixelFont Font { get; }
    public SpriteArt Art { get; }

    public Renderer(GraphicsDevice device)
    {
        Batch = new SpriteBatch(device);
        Font = new PixelFont(device);
        Art = new SpriteArt(device);
    }

    public void Rect(float x, float y, float w, float h, Color color) =>
        Batch.Draw(Art.Pixel, new Vector2(x, y), null, color, 0f, Vector2.Zero, new Vector2(w, h), SpriteEffects.None, 0f);

    public void Frame(float x, float y, float w, float h, Color color, float thickness = 1f)
    {
        Rect(x, y, w, thickness, color);
        Rect(x, y + h - thickness, w, thickness, color);
        Rect(x, y, thickness, h, color);
        Rect(x + w - thickness, y, thickness, h, color);
    }

    public void Text(string text, float x, float y, Color color, float scale = 1f) =>
        Font.Draw(Batch, text, new Vector2((int)x, (int)y), color, scale);

    public void TextCentered(string text, float y, Color color, float scale = 1f, float centreX = Width / 2f) =>
        Text(text, centreX - PixelFont.Measure(text, scale) / 2f, y, color, scale);

    /// <summary>Draws a sprite centred on (cx, cy), snapped to whole virtual pixels.</summary>
    public void Sprite(Texture2D texture, float cx, float cy, Color color, float scale = 1f)
    {
        var pos = new Vector2((int)(cx - texture.Width * scale / 2f), (int)(cy - texture.Height * scale / 2f));
        Batch.Draw(texture, pos, null, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
}
