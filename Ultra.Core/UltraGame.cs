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

    // Title page arrows (touch areas are larger than the drawn buttons)
    private static readonly Rectangle PrevPageKey = new(4, 208, 20, 14);
    private static readonly Rectangle NextPageKey = new(216, 208, 20, 14);
    private static readonly Rectangle PrevPageZone = new(-40, 196, 84, 60);
    private static readonly Rectangle NextPageZone = new(196, 196, 84, 60);
    private const float ManualPageHold = 10f;

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

    // Desktop options screen
    private static readonly Rectangle MouseKey = new(160, 44, 60, 16);
    private static readonly Rectangle FullScreenKey = new(160, 66, 60, 16);

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
    private float _pageTime;
    private bool _resizing;

    /// <summary>
    /// When set, the finished frame is drawn into this target instead of the window, at the
    /// target's size. Used to capture store screenshots at any resolution.
    /// </summary>
    public RenderTarget2D OutputTarget { get; set; }

    public UltraGame(ITiltSensor tiltSensor = null)
    {
        _input = new InputManager(tiltSensor, _settings);
        _graphics = new GraphicsDeviceManager(this)
        {
            IsFullScreen = !Platform.IsDesktop,
            SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight,
            SynchronizeWithVerticalRetrace = true,
        };
        IsFixedTimeStep = false;
        IsMouseVisible = true;
        Window.AllowUserResizing = Platform.IsDesktop;
        if (Platform.IsDesktop)
        {
            // Full screen uses a borderless window the size of the display, without a mode change.
            _graphics.HardwareModeSwitch = false;
            Window.Title = "The Ultra";
            Window.ClientSizeChanged += OnClientSizeChanged;
        }
    }

    protected override void Initialize()
    {
        if (Platform.IsDesktop)
            ApplyWindowMode(_settings.FullScreen);
        base.Initialize();
        _scores = new HighScoreTable();
        Deactivated += (_, _) => { if (_state == State.Playing) _paused = true; };
    }

    /// <summary>Starts a game on the given sheet that plays itself, for store screenshots.</summary>
    public void StartDemo(int sheet)
    {
        _world = new World(_sfx, sheet) { Autopilot = true };
        _paused = false;
        SetState(State.Playing);
    }

    /// <summary>Shows one of the title pages and holds it there, for store screenshots.</summary>
    public void ShowTitlePage(int page)
    {
        SetState(State.Title);
        _titlePage = page;
        _pageTime = -1000f;
    }

    // ------------------------------------------------------------------ desktop window

    private void ApplyWindowMode(bool fullScreen)
    {
        _resizing = true;
        var display = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
        if (fullScreen)
        {
            _graphics.PreferredBackBufferWidth = display.Width;
            _graphics.PreferredBackBufferHeight = display.Height;
        }
        else
        {
            // The largest whole-number scale of the Oric screen that fits, with side margins.
            int scale = Math.Max(2, Math.Min((display.Height - 160) / Renderer.Height, (display.Width - 80) * 3 / (Renderer.Width * 4)));
            _graphics.PreferredBackBufferHeight = Renderer.Height * scale;
            _graphics.PreferredBackBufferWidth = Renderer.Width * scale * 4 / 3;
        }
        _graphics.IsFullScreen = fullScreen;
        _graphics.ApplyChanges();
        _resizing = false;
        _settings.SetFullScreen(fullScreen);
    }

    private void OnClientSizeChanged(object sender, EventArgs e)
    {
        var size = Window.ClientBounds;
        if (_resizing || _graphics.IsFullScreen || size.Width <= 0 || size.Height <= 0)
            return;
        if (size.Width == _graphics.PreferredBackBufferWidth && size.Height == _graphics.PreferredBackBufferHeight)
            return;
        _resizing = true;
        _graphics.PreferredBackBufferWidth = size.Width;
        _graphics.PreferredBackBufferHeight = size.Height;
        _graphics.ApplyChanges();
        _resizing = false;
    }

    private void UpdateDesktop()
    {
        var keys = Keyboard.GetState();
        bool alt = keys.IsKeyDown(Keys.LeftAlt) || keys.IsKeyDown(Keys.RightAlt);
        bool command = keys.IsKeyDown(Keys.LeftWindows) || keys.IsKeyDown(Keys.RightWindows);
        bool control = keys.IsKeyDown(Keys.LeftControl) || keys.IsKeyDown(Keys.RightControl);
        if (_input.NewKeys.Contains(Keys.F11) || (alt && _input.NewKeys.Contains(Keys.Enter)) ||
            (command && control && _input.NewKeys.Contains(Keys.F)))
            ApplyWindowMode(!_graphics.IsFullScreen);

        // The ship is the pointer while playing with the mouse.
        IsMouseVisible = !(_state == State.Playing && !_paused && _settings.MouseSteering && IsActive);
    }

    protected override void LoadContent()
    {
        _screen = new RenderTarget2D(GraphicsDevice, Renderer.Width * Renderer.Supersample, Renderer.Height * Renderer.Supersample);
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
        if (OutputTarget != null)
            _layout.Update(OutputTarget.Width, OutputTarget.Height, false);
        else
            _layout.Update(pp.BackBufferWidth, pp.BackBufferHeight, !_input.HasTilt && !Platform.IsDesktop);
        _input.Update(_layout);
        if (Platform.IsDesktop && OutputTarget == null)
            UpdateDesktop();

        switch (_state)
        {
            case State.Splash: UpdateSplash(); break;
            case State.Title: UpdateTitle(dt); break;
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
        if (state == State.Title)
            _pageTime = 0f;
    }

    private void UpdateSplash()
    {
        if (_stateTime > SplashTime || (_stateTime > 0.6f && _input.AnyPressed))
        {
            _titlePage = 0;
            SetState(State.Title);
        }
    }

    private void UpdateTitle(float dt)
    {
        _pageTime += dt;

        if (_input.BackPressed)
        {
            // On a Mac, Command-Q quits.
            if (OperatingSystem.IsAndroid() || OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
                Exit();
            return;
        }

        if (_input.PausePressed || _input.NewKeys.Contains(Keys.O))
        {
            OpenOptions(false);
            return;
        }

        if (_input.Swipe != 0)
        {
            // Swipe left for the next page, as with a book.
            ChangeTitlePage(-_input.Swipe, true);
            return;
        }

        if (_input.NewKeys.Contains(Keys.Left))
            ChangeTitlePage(-1, true);
        else if (_input.NewKeys.Contains(Keys.Right))
            ChangeTitlePage(1, true);

        bool start = _input.ConfirmPressed;
        foreach (var tap in _input.TapReleases)
        {
            var p = tap.ToPoint();
            if (PrevPageZone.Contains(p))
                ChangeTitlePage(-1, true);
            else if (NextPageZone.Contains(p))
                ChangeTitlePage(1, true);
            else
                start = true;
        }

        if (_stateTime > 0.4f && start)
        {
            _world = new World(_sfx);
            _paused = false;
            _highlightRank = -1;
            SetState(State.Playing);
            return;
        }

        if (_pageTime > TitlePageTimes[_titlePage])
            ChangeTitlePage(1, false);
    }

    private void ChangeTitlePage(int step, bool manual)
    {
        _titlePage = (_titlePage + step + TitlePages) % TitlePages;
        _pageTime = manual ? -ManualPageHold : 0f;
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

        if (Platform.IsDesktop)
        {
            UpdateDesktopOptions();
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

    private void UpdateDesktopOptions()
    {
        if (_input.NewKeys.Contains(Keys.M))
            _settings.ToggleMouseSteering();
        if (_input.NewKeys.Contains(Keys.F))
            ApplyWindowMode(!_graphics.IsFullScreen);

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

            if (MouseKey.Contains(p))
                _settings.ToggleMouseSteering();
            else if (FullScreenKey.Contains(p))
                ApplyWindowMode(!_graphics.IsFullScreen);
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
        batch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: Matrix.CreateScale(Renderer.Supersample));
        switch (_state)
        {
            case State.Splash: DrawSplash(); break;
            case State.Title: DrawTitle(); break;
            case State.Playing: DrawPlaying(); break;
            case State.EnterName: DrawEnterName(); break;
            case State.Options: DrawOptions(); break;
        }
        batch.End();

        GraphicsDevice.SetRenderTarget(OutputTarget);
        GraphicsDevice.Clear(PanelColor);

        batch.Begin(samplerState: SamplerState.PointClamp);
        batch.Draw(_screen, _layout.GameRect, Color.White);
        if (_state == State.Playing)
            DrawTouchControls();
        else if (_state == State.Title)
            DrawOptionsIcon();
        batch.End();

        if (OutputTarget != null)
            GraphicsDevice.SetRenderTarget(null);
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
        r.Sprite(r.Art.Player, 120, 92, Palette.Cyan * fade);
        for (int i = 0; i < 5; i++)
            r.Sprite(r.Art.Alien[i * 3, SpriteArt.FrameAt((int)(_clock * 6f))], 48 + i * 36, 120, World.WaveColors[i * 3] * fade);

        r.TextCentered("BY PFJ", 150, Palette.White * fade, 2f);
        r.TextCentered("BASED ON THE PSS ORIC GAME", 180, Palette.Yellow * fade);
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
            r.TextCentered("BY PFJ - BASED ON THE PSS ORIC GAME", 196, Palette.Magenta);
        else if (Platform.IsDesktop)
            r.TextCentered("MOUSE OR Z X TO MOVE - CLICK TO FIRE", 196, Palette.Cyan);
        else
            r.TextCentered(_input.HasTilt ? "TILT TO MOVE - TAP TO FIRE" : "ARROWS TO MOVE - TAP TO FIRE", 196, Palette.Cyan);
        if ((int)(_clock * 2f) % 2 == 0)
            r.TextCentered(Platform.IsDesktop ? "CLICK OR SPACE TO PLAY" : "TOUCH TO PLAY", 210, Palette.Yellow);

        DrawKey(PrevPageKey, "<", Palette.White);
        DrawKey(NextPageKey, ">", Palette.White);
        for (int i = 0; i < TitlePages; i++)
            r.Rect(30 + i * 6, 213, 3, 3, i == _titlePage ? Palette.Yellow : Palette.Blue);
    }

    private void DrawHowToPlay()
    {
        var r = _renderer;
        r.TextCentered("HOW TO PLAY", 34, Palette.Cyan);

        string move = _input.HasTilt ? "TILT THE DEVICE TO STEER YOUR SHIP." : "USE THE ARROW BUTTONS TO MOVE.";
        var controls = Platform.IsDesktop
            ? new (string Text, Color Color)[]
            {
                ("MOVE WITH THE MOUSE, OR Z (LEFT) AND", Palette.Green),
                ("X (RIGHT). THE ARROW KEYS WORK TOO.", Palette.Green),
                ("CLICK OR PRESS SPACE TO FIRE - HOLD", Palette.Green),
                ("IT DOWN FOR RAPID FIRE. P PAUSES.", Palette.Green),
            }
            : new (string Text, Color Color)[]
            {
                (move, Palette.Green),
                ("TAP ANYWHERE TO FIRE. HOLD YOUR", Palette.Green),
                ("FINGER DOWN FOR RAPID FIRE.", Palette.Green),
                ("", Palette.White),
            };
        var lines = new (string Text, Color Color)[]
        {
            ("DESTROY ALL 16 SHEETS OF ALIENS -", Palette.White),
            ("EACH SHEET MOVES IN ITS OWN WAY.", Palette.White),
            ("", Palette.White),
            controls[0], controls[1], controls[2], controls[3],
            ("WATCH THE HEAT GAUGE! IF THE GUN", Palette.Yellow),
            ("OVERHEATS IT LOCKS UNTIL IT COOLS,", Palette.Yellow),
            ("AND THE HEAT CARRIES ON INTO THE", Palette.Yellow),
            ("NEXT WAVE.", Palette.Yellow),
            ("", Palette.White),
            ("DODGE THE BOMBS AND THE ALIENS.", Palette.Red),
            ("BONUS LIVES AT 2000 AND 10000.", Palette.Magenta),
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
            r.Sprite(r.Art.Alien[i, SpriteArt.FrameAt((int)(_clock * 6f) + i)], x + 6, y + 3, World.WaveColors[i]);
            r.Text(World.WaveNames[i], x + 16, y, World.WaveColors[i]);
            string pts = World.PointsFor(i, 0).ToString();
            r.Text(pts, x + 112 - PixelFont.Measure(pts), y, Palette.White);
        }
        r.TextCentered("CLEAR ALL 16 FOR A 5000 BONUS", 180, Palette.Green);
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

        if (Platform.IsDesktop)
        {
            DrawDesktopOptions();
            return;
        }

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
            r.Sprite(r.Art.Player, x, 156, Palette.Cyan);
        }
        else
            r.TextCentered("NO MOTION SENSOR FOUND", 150, Palette.Red);

        DrawKey(DoneKey, "DONE", Palette.Green);
    }

    private void DrawDesktopOptions()
    {
        var r = _renderer;
        r.Text("MOUSE STEERING", 20, MouseKey.Y + 4, Palette.Cyan);
        DrawKey(MouseKey, _settings.MouseSteering ? "ON" : "OFF", _settings.MouseSteering ? Palette.Green : Palette.White);
        r.Text("FULL SCREEN", 20, FullScreenKey.Y + 4, Palette.Cyan);
        DrawKey(FullScreenKey, _graphics.IsFullScreen ? "ON" : "OFF", _graphics.IsFullScreen ? Palette.Green : Palette.White);

        string screenKeys = OperatingSystem.IsMacOS() ? "CMD CTRL F" : "F11, ALT+ENTER";
        var lines = new (string Key, string Action)[]
        {
            ("MOVE", "MOUSE, Z X OR ARROWS"),
            ("FIRE", "CLICK, SPACE OR CTRL"),
            ("PAUSE", "P, ESC OR TOP-LEFT"),
            ("SCREEN", screenKeys),
            ("OPTIONS", "M MOUSE  F FULL SCREEN"),
        };
        r.TextCentered("CONTROLS", 100, Palette.Yellow);
        for (int i = 0; i < lines.Length; i++)
        {
            r.Text(lines[i].Key, 20, 116 + i * 12, Palette.Cyan);
            r.Text(lines[i].Action, 74, 116 + i * 12, Palette.White);
        }

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
