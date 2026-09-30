using System;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Ultra.Core;

/// <summary>
/// The Ultra - a remake of the 1983 PSS arcade shooter for the Oric-1 and Atmos.
/// Written by Paul F. Johnson.
/// </summary>
public class UltraGame : Game
{
    private enum State { Splash, Title, Playing, EnterName }

    private const float SplashTime = 3.5f;
    private const float TitlePageTime = 7f;

    private static readonly string[] KeyRows = { "ABCDEFGHIJ", "KLMNOPQRST", "UVWXYZ0123", "456789.-!" };
    private const int KeysX = 20, KeysY = 104, KeyW = 20, KeyH = 18;
    private static readonly Rectangle SpaceKey = new(20, 180, 60, 16);
    private static readonly Rectangle DeleteKey = new(90, 180, 60, 16);
    private static readonly Rectangle EndKey = new(160, 180, 60, 16);

    private static readonly Color PanelColor = new(6, 6, 22);
    private static readonly Color ButtonColor = new(40, 40, 140);
    private static readonly Color ButtonLit = new(90, 90, 255);

    private readonly GraphicsDeviceManager _graphics;
    private readonly ScreenLayout _layout = new();
    private readonly InputManager _input = new();
    private readonly StringBuilder _name = new();

    private RenderTarget2D _screen;
    private Renderer _renderer;
    private Sfx _sfx;
    private HighScoreTable _scores;
    private World _world;

    private State _state = State.Splash;
    private float _stateTime;
    private float _clock;
    private int _titlePage;
    private bool _paused;
    private int _highlightRank = -1;
    private int _pendingRank;

    public UltraGame()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            IsFullScreen = true,
            SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight,
            SynchronizeWithVerticalRetrace = true,
        };
        IsFixedTimeStep = false;
        IsMouseVisible = true;
        Window.AllowUserResizing = false;
    }

    protected override void Initialize()
    {
        base.Initialize();
        _scores = new HighScoreTable();
        Deactivated += (_, _) => { if (_state == State.Playing) _paused = true; };
    }

    protected override void LoadContent()
    {
        _screen = new RenderTarget2D(GraphicsDevice, Renderer.Width, Renderer.Height);
        _renderer = new Renderer(GraphicsDevice);
        _sfx = new Sfx();
    }

    // ------------------------------------------------------------------ update

    protected override void Update(GameTime gameTime)
    {
        float dt = Math.Min((float)gameTime.ElapsedGameTime.TotalSeconds, 1f / 20f);
        _clock += dt;
        _stateTime += dt;

        var pp = GraphicsDevice.PresentationParameters;
        _layout.Update(pp.BackBufferWidth, pp.BackBufferHeight);
        _input.Update(_layout);

        switch (_state)
        {
            case State.Splash: UpdateSplash(); break;
            case State.Title: UpdateTitle(); break;
            case State.Playing: UpdatePlaying(dt); break;
            case State.EnterName: UpdateEnterName(); break;
        }

        base.Update(gameTime);
    }

    private void SetState(State state)
    {
        _state = state;
        _stateTime = 0f;
    }

    private void UpdateSplash()
    {
        if (_stateTime > SplashTime || (_stateTime > 0.6f && _input.AnyPressed))
        {
            _titlePage = 0;
            SetState(State.Title);
        }
    }

    private void UpdateTitle()
    {
        if (_input.BackPressed)
        {
            if (OperatingSystem.IsAndroid())
                Exit();
            return;
        }

        if (_stateTime > 0.4f && (_input.AnyPressed || _input.ConfirmPressed))
        {
            _world = new World(_sfx);
            _paused = false;
            _highlightRank = -1;
            SetState(State.Playing);
            return;
        }

        if (_stateTime > TitlePageTime)
        {
            _titlePage = (_titlePage + 1) % 2;
            _stateTime = 0.4f;
        }
    }

    private void UpdatePlaying(float dt)
    {
        if (_paused)
        {
            if (_input.BackPressed)
            {
                _paused = false;
                _world.Abandon();
            }
            else if (_input.AnyPressed)
                _paused = false;
            return;
        }

        if (_input.PausePressed || _input.BackPressed)
        {
            _paused = true;
            return;
        }

        _world.Update(dt, _input);

        if (_world.IsFinished)
            EndGame();
    }

    private void EndGame()
    {
        _pendingRank = _scores.RankFor(_world.Score);
        if (_pendingRank >= 0)
        {
            _name.Clear();
            _sfx.HighScore();
            SetState(State.EnterName);
        }
        else
        {
            _titlePage = 1;
            SetState(State.Title);
        }
    }

    private void UpdateEnterName()
    {
        foreach (var key in _input.NewKeys)
        {
            if (key >= Keys.A && key <= Keys.Z)
                TypeChar((char)('A' + (key - Keys.A)));
            else if (key >= Keys.D0 && key <= Keys.D9)
                TypeChar((char)('0' + (key - Keys.D0)));
            else if (key == Keys.Space)
                TypeChar(' ');
            else if (key == Keys.OemPeriod)
                TypeChar('.');
            else if (key == Keys.OemMinus)
                TypeChar('-');
            else if (key == Keys.Back)
            {
                if (_name.Length > 0) _name.Length--;
            }
            else if (key == Keys.Enter)
            {
                FinishName();
                return;
            }
        }

        if (_input.BackPressed)
        {
            FinishName();
            return;
        }

        if (_stateTime < 0.5f)
            return;

        foreach (var tap in _input.Taps)
        {
            var p = tap.ToPoint();
            if (EndKey.Contains(p))
            {
                FinishName();
                return;
            }
            if (DeleteKey.Contains(p))
            {
                if (_name.Length > 0) _name.Length--;
                continue;
            }
            if (SpaceKey.Contains(p))
            {
                TypeChar(' ');
                continue;
            }

            int row = (p.Y - KeysY) / KeyH;
            int col = (p.X - KeysX) / KeyW;
            if (p.Y >= KeysY && p.X >= KeysX && row < KeyRows.Length && col >= 0 && col < KeyRows[row].Length)
                TypeChar(KeyRows[row][col]);
        }
    }

    private void TypeChar(char c)
    {
        if (_name.Length < HighScoreTable.MaxNameLength && !(c == ' ' && _name.Length == 0))
            _name.Append(c);
    }

    private void FinishName()
    {
        _highlightRank = _scores.Insert(_name.ToString(), _world.Score, _world.Wave);
        _titlePage = 1;
        SetState(State.Title);
    }

    // ------------------------------------------------------------------ draw

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(_screen);
        GraphicsDevice.Clear(Palette.Black);

        var batch = _renderer.Batch;
        batch.Begin(samplerState: SamplerState.PointClamp);
        switch (_state)
        {
            case State.Splash: DrawSplash(); break;
            case State.Title: DrawTitle(); break;
            case State.Playing: DrawPlaying(); break;
            case State.EnterName: DrawEnterName(); break;
        }
        batch.End();

        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(PanelColor);

        batch.Begin(samplerState: SamplerState.PointClamp);
        batch.Draw(_screen, _layout.GameRect, Color.White);
        if (_state == State.Playing)
            DrawTouchControls();
        batch.End();

        base.Draw(gameTime);
    }

    private void DrawLogo(float y)
    {
        int offset = (int)(_clock * 8f);
        const float scale = 3f;
        const string title = "THE ULTRA";
        float x = Renderer.Width / 2f - PixelFont.Measure(title, scale) / 2f;
        _renderer.Font.DrawBanded(_renderer.Batch, title, new Vector2((int)x, (int)y), Palette.Rainbow, offset, scale);
    }

    private void DrawSplash()
    {
        var r = _renderer;
        float fade = Math.Clamp(_stateTime / 0.5f, 0f, 1f);

        DrawLogo(40);
        r.Sprite(r.Art.Player, 120, 92, Palette.Green * fade);
        for (int i = 0; i < 5; i++)
            r.Sprite(r.Art.Alien[i * 3, (int)(_clock * 4f) % 2], 48 + i * 36, 120, World.WaveColors[i * 3] * fade);

        r.TextCentered("WRITTEN BY", 148, Palette.Cyan * fade);
        r.TextCentered("PAUL F. JOHNSON", 160, Palette.White * fade, 1f);
        r.TextCentered("AFTER THE ORIC CLASSIC", 184, Palette.Yellow * fade);
        r.TextCentered("PUBLISHED BY PSS IN 1983", 194, Palette.Yellow * fade);
    }

    private void DrawTitle()
    {
        var r = _renderer;
        DrawLogo(6);

        if (_titlePage == 0)
        {
            r.TextCentered("SCORE ADVANCE TABLE", 34, Palette.Cyan);
            for (int i = 0; i < SpriteArt.AlienTypes; i++)
            {
                int col = i / 8, row = i % 8;
                float x = 14 + col * 116, y = 52 + row * 16;
                r.Sprite(r.Art.Alien[i, (int)(_clock * 3f + i) % 2], x + 6, y + 3, World.WaveColors[i]);
                r.Text($"{i + 1,2} {World.PointsFor(i, 0),3} PTS", x + 18, y, Palette.White);
            }
            r.TextCentered("DIVING ALIENS SCORE DOUBLE", 182, Palette.Green);
        }
        else
        {
            r.TextCentered("HALL OF FAME", 34, Palette.Cyan);
            for (int i = 0; i < _scores.Entries.Count; i++)
            {
                var e = _scores.Entries[i];
                bool lit = i == _highlightRank && (int)(_clock * 4f) % 2 == 0;
                var color = lit ? Palette.Red : i == 0 ? Palette.Yellow : i < 3 ? Palette.Green : Palette.White;
                r.Text($"{i + 1,2}. {e.Name,-10} {e.Score:D6} W{e.Wave:D2}", 45, 52 + i * 13, color);
            }
        }

        r.TextCentered("BY PAUL F. JOHNSON", 196, Palette.Magenta);
        if ((int)(_clock * 2f) % 2 == 0)
            r.TextCentered("TOUCH TO PLAY", 210, Palette.Yellow);
    }

    private void DrawPlaying()
    {
        var r = _renderer;
        _world.Draw(r, _scores.Best);

        if (_paused)
        {
            r.Rect(40, 80, 160, 56, Palette.Black);
            r.Frame(40, 80, 160, 56, Palette.Cyan);
            r.TextCentered("PAUSED", 88, Palette.Yellow, 2f);
            r.TextCentered("TOUCH TO CONTINUE", 110, Palette.White);
            r.TextCentered("BACK TO QUIT", 122, Palette.Cyan);
        }
    }

    private void DrawEnterName()
    {
        var r = _renderer;
        bool flash = (int)(_clock * 4f) % 2 == 0;

        r.TextCentered("CONGRATULATIONS!", 10, flash ? Palette.Yellow : Palette.Red, 1f);
        r.TextCentered($"YOUR SCORE OF {_world.Score}", 28, Palette.White);
        r.TextCentered($"IS NUMBER {_pendingRank + 1} IN THE", 40, Palette.White);
        r.TextCentered("HALL OF FAME", 52, Palette.Cyan);
        r.TextCentered("PLEASE ENTER YOUR NAME", 68, Palette.Green);

        string shown = _name + (flash && _name.Length < HighScoreTable.MaxNameLength ? "_" : " ");
        r.Frame(52, 80, 136, 16, Palette.Blue);
        r.Text(shown.PadRight(HighScoreTable.MaxNameLength), 60, 84, Palette.Yellow, 1f);

        for (int row = 0; row < KeyRows.Length; row++)
        {
            for (int col = 0; col < KeyRows[row].Length; col++)
            {
                int x = KeysX + col * KeyW, y = KeysY + row * KeyH;
                r.Frame(x, y, KeyW - 2, KeyH - 2, Palette.Blue);
                r.Text(KeyRows[row][col].ToString(), x + 6, y + 4, Palette.White);
            }
        }

        DrawKey(SpaceKey, "SPACE", Palette.White);
        DrawKey(DeleteKey, "DEL", Palette.Red);
        DrawKey(EndKey, "END", Palette.Green);
    }

    private void DrawKey(Rectangle rect, string label, Color color)
    {
        _renderer.Frame(rect.X, rect.Y, rect.Width, rect.Height, Palette.Blue);
        _renderer.TextCentered(label, rect.Y + 4, color, 1f, rect.Center.X);
    }

    private void DrawTouchControls()
    {
        var batch = _renderer.Batch;
        var art = _renderer.Art;
        int textScale = Math.Max(2, (int)_layout.Scale);

        // Pause
        var pause = _layout.PauseButton;
        int bar = Math.Max(4, Math.Min(pause.Width, pause.Height) / 10);
        int barH = bar * 4;
        var pc = pause.Center;
        Fill(new Rectangle(pc.X - bar * 2, pc.Y - barH / 2, bar, barH), ButtonLit);
        Fill(new Rectangle(pc.X + bar, pc.Y - barH / 2, bar, barH), ButtonLit);

        // Left / right
        DrawArrow(_layout.LeftButton, true, _input.Left);
        DrawArrow(_layout.RightButton, false, _input.Right);

        // Fire
        var fire = _layout.FireButton;
        int d = (int)(Math.Min(fire.Width, fire.Height) * 0.7f);
        var circle = new Rectangle(fire.Center.X - d / 2, fire.Center.Y - d / 2, d, d);
        batch.Draw(art.Circle, circle, _input.Fire ? Palette.Red : new Color(140, 0, 0));
        var inner = circle;
        inner.Inflate(-d / 12, -d / 12);
        batch.Draw(art.Circle, inner, _input.Fire ? new Color(255, 80, 80) : new Color(200, 0, 0));
        const string label = "FIRE";
        var size = new Vector2(PixelFont.Measure(label, textScale), PixelFont.CellHeight * textScale);
        _renderer.Font.Draw(batch, label, circle.Center.ToVector2() - size / 2f, Palette.White, textScale);

        if (_world != null && _world.Overheated && (int)(_clock * 6f) % 2 == 0)
        {
            const string hot = "HOT!";
            var hs = new Vector2(PixelFont.Measure(hot, textScale), 0);
            _renderer.Font.Draw(batch, hot, new Vector2(fire.Center.X - hs.X / 2f, circle.Bottom + textScale * 4), Palette.Yellow, textScale);
        }
    }

    private void DrawArrow(Rectangle zone, bool pointLeft, bool lit)
    {
        var batch = _renderer.Batch;
        var box = zone;
        box.Inflate(-Math.Max(4, zone.Width / 10), -Math.Max(4, zone.Width / 10));
        int size = Math.Min(box.Width, box.Height);
        box = new Rectangle(zone.Center.X - size / 2, zone.Bottom - size - zone.Width / 8, size, size);

        Fill(box, lit ? ButtonLit : ButtonColor);
        int t = Math.Max(2, size / 30);
        Fill(new Rectangle(box.X, box.Y, box.Width, t), ButtonLit);
        Fill(new Rectangle(box.X, box.Bottom - t, box.Width, t), ButtonLit);
        Fill(new Rectangle(box.X, box.Y, t, box.Height), ButtonLit);
        Fill(new Rectangle(box.Right - t, box.Y, t, box.Height), ButtonLit);

        int tri = (int)(size * 0.5f);
        var dest = new Rectangle(box.Center.X - tri / 2, box.Center.Y - tri / 2, tri, tri);
        batch.Draw(_renderer.Art.Triangle, dest, null, Palette.White, 0f, Vector2.Zero,
            pointLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
    }

    private void Fill(Rectangle rect, Color color) => _renderer.Batch.Draw(_renderer.Art.Pixel, rect, color);
}
