using System;
using Microsoft.Xna.Framework;

namespace Ultra.Core;

/// <summary>
/// Places the scaled 240x224 game screen in the middle of the landscape display.
/// With tilt steering the game fills the screen height and the pause button sits in the
/// top-left corner; without a motion sensor, side panels are reserved for movement buttons.
/// </summary>
public sealed class ScreenLayout
{
    private bool _buttons;

    public Rectangle GameRect { get; private set; }
    public float Scale { get; private set; } = 1f;
    public Rectangle PauseButton { get; private set; }
    public Rectangle LeftButton { get; private set; }
    public Rectangle RightButton { get; private set; }
    public Rectangle TiltMeter { get; private set; }
    public int ScreenWidth { get; private set; }
    public int ScreenHeight { get; private set; }

    public void Update(int width, int height, bool movementButtons)
    {
        if (width == ScreenWidth && height == ScreenHeight && movementButtons == _buttons)
            return;

        ScreenWidth = width;
        ScreenHeight = height;
        _buttons = movementButtons;

        int panel = movementButtons ? (int)(width * 0.18f) : 0;
        Scale = Math.Min((width - 2f * panel) / Renderer.Width, height / (float)Renderer.Height);
        int gw = (int)(Renderer.Width * Scale);
        int gh = (int)(Renderer.Height * Scale);
        GameRect = new Rectangle((width - gw) / 2, (height - gh) / 2, gw, gh);

        int margin = GameRect.Left;
        int pauseSize = Math.Max(margin, (int)(height * 0.16f));
        PauseButton = new Rectangle(0, 0, pauseSize, (int)(height * 0.2f));

        int pauseHeight = PauseButton.Height;
        int half = margin / 2;
        LeftButton = movementButtons ? new Rectangle(0, pauseHeight, half, height - pauseHeight) : Rectangle.Empty;
        RightButton = movementButtons ? new Rectangle(half, pauseHeight, margin - half, height - pauseHeight) : Rectangle.Empty;

        int meterW = (int)(margin * 0.7f);
        int meterH = Math.Max(8, (int)(height * 0.05f));
        TiltMeter = new Rectangle((margin - meterW) / 2, height - meterH * 3, meterW, meterH);
    }

    public Vector2 ToVirtual(Vector2 screen) =>
        new((screen.X - GameRect.X) / Scale, (screen.Y - GameRect.Y) / Scale);
}
