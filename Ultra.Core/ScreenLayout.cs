using System;
using Microsoft.Xna.Framework;

namespace Ultra.Core;

/// <summary>
/// Places the scaled 240x224 game screen in the middle of the landscape display and
/// uses the space either side for the touch controls: movement on the left, fire on the right.
/// </summary>
public sealed class ScreenLayout
{
    public Rectangle GameRect { get; private set; }
    public float Scale { get; private set; } = 1f;
    public Rectangle LeftPanel { get; private set; }
    public Rectangle RightPanel { get; private set; }
    public Rectangle PauseButton { get; private set; }
    public Rectangle LeftButton { get; private set; }
    public Rectangle RightButton { get; private set; }
    public Rectangle FireButton { get; private set; }
    public int ScreenWidth { get; private set; }
    public int ScreenHeight { get; private set; }

    public void Update(int width, int height)
    {
        if (width == ScreenWidth && height == ScreenHeight)
            return;

        ScreenWidth = width;
        ScreenHeight = height;

        int panel = (int)(width * 0.18f);
        Scale = Math.Min((width - 2f * panel) / Renderer.Width, height / (float)Renderer.Height);
        int gw = (int)(Renderer.Width * Scale);
        int gh = (int)(Renderer.Height * Scale);
        GameRect = new Rectangle((width - gw) / 2, (height - gh) / 2, gw, gh);

        LeftPanel = new Rectangle(0, 0, GameRect.Left, height);
        RightPanel = new Rectangle(GameRect.Right, 0, width - GameRect.Right, height);

        int pauseHeight = (int)(height * 0.2f);
        PauseButton = new Rectangle(0, 0, LeftPanel.Width, pauseHeight);
        int half = LeftPanel.Width / 2;
        LeftButton = new Rectangle(0, pauseHeight, half, height - pauseHeight);
        RightButton = new Rectangle(half, pauseHeight, LeftPanel.Width - half, height - pauseHeight);
        FireButton = RightPanel;
    }

    public Vector2 ToVirtual(Vector2 screen) =>
        new((screen.X - GameRect.X) / Scale, (screen.Y - GameRect.Y) / Scale);
}
