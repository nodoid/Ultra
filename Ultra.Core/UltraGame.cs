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
    private enum State { Splash, Title, Playing, EnterName, Options }

    private const float SplashTime = 3.5f;
    private const int HowToPlayPage = 0, ScoreTablePage = 1, HallOfFamePage = 2, TitlePages = 3;
    private static readonly float[] TitlePageTimes = { 12f, 8f, 7f };

    private static readonly string[] KeyRows = { "ABCDEFGHIJ", "KLMNOPQRST", "UVWXYZ0123", "456789.-!" };
    private const int KeysX = 20, KeysY = 104, KeyW = 20, KeyH = 18;
    private static readonly Rectangle SpaceKey = new(20, 180, 60, 16);
    private static readonly Rectangle DeleteKey = new(90, 180, 60, 16);
    private static readonly Rectangle EndKey = new(160, 180, 60, 16);

    // Pause menu
    private static readonly Rectangle ResumeKey = new(80, 100, 80, 14);
    private static readonly Rectangle OptionsKey = new(80, 118, 80, 14);
    private static readonly Rectangle QuitKey = new(80, 136, 80, 14);

    // Options screen
    private static readonly Rectangle MinusKey = new(18, 62, 24, 20);
    private static readonly Rectangle PlusKey = new(198, 62, 24, 20);
    private const int SliderX = 50, SliderY = 62, SegmentW = 14, SegmentH = 20;
    private static readonly Rectangle InvertKey = new(150, 106, 60, 16);
    private static readonly Rectangle DoneKey = new(90, 190, 60, 16);

    private static readonly Color PanelColor = new(6, 6, 22);
    private static readonly Color ButtonColor = new(40, 40, 140);
    private static readonly Color ButtonLit = new(90, 90, 255);

    private readonly GraphicsDeviceManager _graphics;
    private readonly ScreenLayout _layout = new();
    private readonly InputManager _input;
    private readonly Settings _settings = new();
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
    private bool _optionsFromPause;

    public UltraGame(ITiltSensor tiltSensor = null)
    {
        _input = new InputManager(tiltSensor, _settings);
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
        _layout.Update(pp.BackBufferWidth, pp.BackBufferHeight, !_input.HasTilt);
        _input.Update(_layout);

        switch (_state)
        {
            case State.Splash: UpdateSplash(); break;
            case State.Title: UpdateTitle(); break;
            case State.Playing: UpdatePlaying(dt); break;
            case State.EnterName: UpdateEnterName(); break;
            case State.Options: UpdateOptions(); break;
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

        if (_input.PausePressed || _input.NewKeys.Contains(Keys.O))
        {
            OpenOptions(false);
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

        if (_stateTime > TitlePageTimes[_titlePage])
        {
            _titlePage = (_titlePage + 1) % TitlePages;
            _stateTime = 0.4f;
        }
    }

    private void UpdatePlaying(float dt)
    {
        if (_paused)
        {
            if (_input.BackPressed)
            {
                QuitGame();
                return;
            }

            if (_input.PausePressed || _input.ConfirmPressed)
            {
                _paused = false;
                return;
            }

            foreach (var tap in _input.Taps)
            {
                var p = tap.ToPoint();
                if (ResumeKey.Contains(p))
                    _paused = false;
                else if (OptionsKey.Contains(p))
                    OpenOptions(true);
                else if (QuitKey.Contains(p))
                    QuitGame();
                else
                    continue;
                break;
            }
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

    private void QuitGame()
    {
        _paused = false;
        _world.Abandon();
    }

    private void OpenOptions(bool fromPause)
    {
        _optionsFromPause = fromPause;
        SetState(State.Options);
    }

    private void CloseOptions()
    {
        if (_optionsFromPause)
        {
            _state = State.Playing;
            _paused = true;
        }
        else
            SetState(State.Title);
    }

    private void UpdateOptions()
    {
        if (_input.BackPressed || _input.NewKeys.Contains(Keys.Enter))
        {
            CloseOptions();
            return;
        }

        if (_input.NewKeys.Contains(Keys.Left) || _input.NewKeys.Contains(Keys.OemMinus))
            _settings.ChangeSensitivity(-1);
        if (_input.NewKeys.Contains(Keys.Right) || _input.NewKeys.Contains(Keys.OemPlus))
            _settings.ChangeSensitivity(1);
        if (_input.NewKeys.Contains(Keys.I))
            _settings.ToggleInvert();

        if (_stateTime < 0.3f)
            return;

        foreach (var tap in _input.Taps)
        {
            var p = tap.ToPoint();
            if (DoneKey.Contains(p))
            {
                CloseOptions();
                return;
            }

            if (MinusKey.Contains(p))
                _settings.ChangeSensitivity(-1);
            else if (PlusKey.Contains(p))
                _settings.ChangeSensitivity(1);
            else if (InvertKey.Contains(p))
                _settings.ToggleInvert();
            else if (p.Y >= SliderY && p.Y < SliderY + SegmentH && p.X >= SliderX && p.X < SliderX + SegmentW * Settings.MaxSensitivity)
            {
                int level = (p.X - SliderX) / SegmentW + 1;
                _settings.ChangeSensitivity(level - _settings.TiltSensitivity);
            }
        }
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
            _titlePage = HallOfFamePage;
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
        _titlePage = HallOfFamePage;
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
            case State.Options: DrawOptions(); break;
        }
        batch.End();

        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(PanelColor);

        batch.Begin(samplerState: SamplerState.PointClamp);
        batch.Draw(_screen, _layout.GameRect, Color.White);
        if (_state == State.Playing)
            DrawTouchControls();
        else if (_state == State.Title)
            DrawOptionsIcon();
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

        switch (_titlePage)
        {
            case HowToPlayPage: DrawHowToPlay(); break;
            case ScoreTablePage: DrawScoreTable(); break;
            default: DrawHallOfFame(); break;
        }

        if ((int)(_clock / 3f) % 2 == 0)
            r.TextCentered("BY PAUL F. JOHNSON", 196, Palette.Magenta);
        else
            r.TextCentered(_input.HasTilt ? "TILT TO MOVE - TAP TO FIRE" : "ARROWS TO MOVE - TAP TO FIRE", 196, Palette.Cyan);
        if ((int)(_clock * 2f) % 2 == 0)
            r.TextCentered("TOUCH TO PLAY", 210, Palette.Yellow);
    }

    private void DrawHowToPlay()
    {
        var r = _renderer;
        r.TextCentered("HOW TO PLAY", 34, Palette.Cyan);

        string move = _input.HasTilt ? "TILT THE DEVICE TO STEER YOUR SHIP." : "USE THE ARROW BUTTONS TO MOVE.";
        var lines = new (string Text, Color Color)[]
        {
            ("DESTROY 16 WAVES OF ALIENS - EACH", Palette.White),
            ("WAVE ATTACKS IN ITS OWN WAY.", Palette.White),
            ("", Palette.White),
            (move, Palette.Green),
            ("TAP ANYWHERE TO FIRE. HOLD YOUR", Palette.Green),
            ("FINGER DOWN FOR RAPID FIRE.", Palette.Green),
            ("", Palette.White),
            ("WATCH THE HEAT GAUGE! IF THE GUN", Palette.Yellow),
            ("OVERHEATS IT LOCKS UNTIL IT COOLS,", Palette.Yellow),
            ("AND THE HEAT CARRIES ON INTO THE", Palette.Yellow),
            ("NEXT WAVE.", Palette.Yellow),
            ("", Palette.White),
            ("DODGE THE BOMBS AND DIVING ALIENS.", Palette.Red),
            ("EXTRA LIFE EVERY 10000 POINTS.", Palette.Magenta),
        };

        for (int i = 0; i < lines.Length; i++)
            r.TextCentered(lines[i].Text, 50 + i * 10, lines[i].Color);
    }

    private void DrawScoreTable()
    {
        var r = _renderer;
        r.TextCentered("POINTS PER ALIEN", 34, Palette.Cyan);
        for (int i = 0; i < SpriteArt.AlienTypes; i++)
        {
            int col = i / 8, row = i % 8;
            float x = 4 + col * 118, y = 50 + row * 16;
            r.Sprite(r.Art.Alien[i, (int)(_clock * 3f + i) % 2], x + 6, y + 3, World.WaveColors[i]);
            r.Text(World.WaveNames[i], x + 16, y, World.WaveColors[i]);
            string pts = World.PointsFor(i, 0).ToString();
            r.Text(pts, x + 112 - PixelFont.Measure(pts), y, Palette.White);
        }
        r.TextCentered("DIVING ALIENS SCORE DOUBLE", 180, Palette.Green);
    }

    private void DrawHallOfFame()
    {
        var r = _renderer;
        r.TextCentered("HALL OF FAME", 34, Palette.Cyan);
        for (int i = 0; i < _scores.Entries.Count; i++)
        {
            var e = _scores.Entries[i];
            bool lit = i == _highlightRank && (int)(_clock * 4f) % 2 == 0;
            var color = lit ? Palette.Red : i == 0 ? Palette.Yellow : i < 3 ? Palette.Green : Palette.White;
            r.Text($"{i + 1,2}. {e.Name,-10} {e.Score:D6} W{e.Wave:D2}", 45, 52 + i * 13, color);
        }
    }

    private void DrawPlaying()
    {
        var r = _renderer;
        _world.Draw(r, _scores.Best);

        if (_paused)
        {
            r.Rect(50, 70, 140, 88, Palette.Black);
            r.Frame(50, 70, 140, 88, Palette.Cyan);
            r.TextCentered("PAUSED", 78, Palette.Yellow, 2f);
            DrawKey(ResumeKey, "RESUME", Palette.Green);
            DrawKey(OptionsKey, "OPTIONS", Palette.White);
            DrawKey(QuitKey, "QUIT", Palette.Red);
        }
    }

    private void DrawOptions()
    {
        var r = _renderer;
        r.TextCentered("OPTIONS", 12, Palette.Yellow, 2f);

        r.TextCentered("TILT SENSITIVITY", 48, Palette.Cyan);
        DrawKey(MinusKey, "-", Palette.White);
        DrawKey(PlusKey, "+", Palette.White);
        for (int i = 0; i < Settings.MaxSensitivity; i++)
        {
            int x = SliderX + i * SegmentW;
            bool on = i < _settings.TiltSensitivity;
            var color = !on ? Palette.Blue : i < 4 ? Palette.Green : i < 7 ? Palette.Yellow : Palette.Red;
            r.Rect(x + 1, SliderY + SegmentH - 4 - i * 1.5f, SegmentW - 3, 4 + i * 1.5f, color);
        }
        r.Text("LOW", SliderX, 88, Palette.White);
        r.TextCentered(_settings.TiltSensitivity.ToString(), 88, Palette.Yellow);
        r.Text("HIGH", SliderX + SegmentW * Settings.MaxSensitivity - PixelFont.Measure("HIGH"), 88, Palette.White);

        r.Text("INVERT TILT", 30, 110, Palette.Cyan);
        DrawKey(InvertKey, _settings.InvertTilt ? "ON" : "OFF", _settings.InvertTilt ? Palette.Green : Palette.White);

        if (_input.HasTilt)
        {
            r.TextCentered("TILT TEST", 136, Palette.Cyan);
            r.Frame(30, 150, 180, 12, Palette.Blue);
            r.Rect(119, 151, 2, 10, Palette.Blue);
            float x = 120 + Math.Clamp(_input.Move, -1f, 1f) * 84f;
            r.Sprite(r.Art.Player, x, 156, Palette.Green);
        }
        else
            r.TextCentered("NO MOTION SENSOR FOUND", 150, Palette.Red);

        DrawKey(DoneKey, "DONE", Palette.Green);
    }

    private void DrawOptionsIcon()
    {
        // Three slider lines with knobs, in the top-left corner (same place as pause in game).
        var zone = _layout.PauseButton;
        int size = Math.Max(24, Math.Min(zone.Width, zone.Height) / 2);
        int line = Math.Max(3, size / 10);
        int knob = line * 3;
        int left = zone.Center.X - size / 2;
        for (int i = 0; i < 3; i++)
        {
            int y = zone.Center.Y - size / 2 + i * size / 2;
            Fill(new Rectangle(left, y - line / 2, size, line), ButtonLit);
            int kx = left + new[] { size / 4, size * 3 / 4, size / 2 }[i] - knob / 2;
            Fill(new Rectangle(kx, y - knob / 2, knob, knob), Palette.White);
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
        int textScale = Math.Max(2, (int)_layout.Scale);

        // Pause
        var pause = _layout.PauseButton;
        int bar = Math.Max(4, Math.Min(pause.Width, pause.Height) / 10);
        int barH = bar * 4;
        var pc = pause.Center;
        Fill(new Rectangle(pc.X - bar * 2, pc.Y - barH / 2, bar, barH), ButtonLit);
        Fill(new Rectangle(pc.X + bar, pc.Y - barH / 2, bar, barH), ButtonLit);

        if (_input.HasTilt)
        {
            // Spirit-level style tilt meter in the left margin.
            var m = _layout.TiltMeter;
            if (m.Width > 16)
            {
                Fill(m, ButtonColor);
                Fill(new Rectangle(m.Center.X - 1, m.Y, 2, m.Height), ButtonLit);
                float pos = Math.Clamp(_input.Move, -1f, 1f);
                int knob = m.Height;
                int kx = (int)(m.Center.X + pos * (m.Width - knob) / 2f - knob / 2f);
                Fill(new Rectangle(kx, m.Y, knob, m.Height), Palette.Green);
                const string tilt = "TILT";
                _renderer.Font.Draw(batch, tilt,
                    new Vector2(m.Center.X - PixelFont.Measure(tilt, textScale) / 2f, m.Y - textScale * 12), ButtonLit, textScale);
            }
        }
        else
        {
            DrawArrow(_layout.LeftButton, true, _input.Left);
            DrawArrow(_layout.RightButton, false, _input.Right);
        }

        // Overheat warning in the right margin.
        int right = _layout.GameRect.Right;
        int marginW = _layout.ScreenWidth - right;
        if (_world != null && _world.Overheated && marginW > textScale * 30 && (int)(_clock * 6f) % 2 == 0)
        {
            const string hot = "HOT!";
            int hs = Math.Max(textScale, Math.Min(marginW / 30, textScale * 2));
            float w = PixelFont.Measure(hot, hs);
            _renderer.Font.Draw(batch, hot, new Vector2(right + (marginW - w) / 2f, _layout.ScreenHeight / 2f - hs * 4), Palette.Red, hs);
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
