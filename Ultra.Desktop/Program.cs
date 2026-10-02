using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ultra.Core;

namespace Ultra.Desktop;

/// <summary>
/// Windows and macOS entry point. The game runs in a resizable window (or full screen) and is
/// played with the mouse and keyboard.
/// <para>
/// <c>TheUltra --screenshots &lt;folder&gt; &lt;width&gt;x&lt;height&gt; [...]</c> captures the store
/// screenshots at each size instead of starting the game.
/// </para>
/// </summary>
internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length >= 3 && args[0] == "--screenshots")
        {
            var sizes = new List<Point>();
            for (int i = 2; i < args.Length; i++)
            {
                var wh = args[i].Split('x');
                sizes.Add(new Point(int.Parse(wh[0]), int.Parse(wh[1])));
            }
            using var shots = new ScreenshotGame(args[1], sizes);
            shots.Run();
            return;
        }

        using var game = new UltraGame();
        game.Run();
    }
}

/// <summary>Plays each store scene by itself and saves it as a PNG at every requested size.</summary>
internal sealed class ScreenshotGame : UltraGame
{
    // Name, sheet to play (0 for a title page), title page, seconds to wait before capturing.
    private static readonly (string Name, int Sheet, int Page, float Delay)[] Scenes =
    {
        ("01-marchers", 1, 0, 5.0f),
        ("02-diamond", 2, 0, 4.6f),
        ("03-conga-line", 3, 0, 4.4f),
        ("04-bow-ties", 5, 0, 4.8f),
        ("05-mushrooms", 6, 0, 4.2f),
        ("06-arrowheads", 8, 0, 4.6f),
        ("07-sparks", 13, 0, 4.4f),
        ("08-points-per-alien", 0, 1, 1.0f),
        ("09-how-to-play", 0, 0, 1.0f),
    };

    private readonly string _folder;
    private readonly List<Point> _sizes;
    private int _scene = -1;
    private float _wait;

    public ScreenshotGame(string folder, List<Point> sizes)
    {
        _folder = folder;
        _sizes = sizes;
    }

    protected override void Update(GameTime gameTime)
    {
        if (_scene < 0)
            StartScene(0);
        base.Update(gameTime);
        _wait -= (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    protected override void Draw(GameTime gameTime)
    {
        if (_wait > 0f)
        {
            base.Draw(gameTime);
            return;
        }

        // Draw the same moment at every size.
        foreach (var size in _sizes)
        {
            using var target = new RenderTarget2D(GraphicsDevice, size.X, size.Y);
            OutputTarget = target;
            base.Update(new GameTime(gameTime.TotalGameTime, TimeSpan.Zero));
            base.Draw(gameTime);
            string dir = Path.Combine(_folder, $"{size.X}x{size.Y}");
            Directory.CreateDirectory(dir);
            using var file = File.Create(Path.Combine(dir, Scenes[_scene].Name + ".png"));
            target.SaveAsPng(file, size.X, size.Y);
            Console.WriteLine($"wrote {dir}/{Scenes[_scene].Name}.png");
        }
        OutputTarget = null;

        if (_scene + 1 < Scenes.Length)
            StartScene(_scene + 1);
        else
            Exit();
    }

    private void StartScene(int index)
    {
        _scene = index;
        var scene = Scenes[index];
        if (scene.Sheet > 0)
            StartDemo(scene.Sheet);
        else
            ShowTitlePage(scene.Page);
        _wait = scene.Delay;
    }
}
