using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Ultra.Core;

public sealed class Alien
{
    public int Col, Row, PathIndex;
    public bool Alive = true;

    // Centre of the alien in virtual pixels. Aliens are two 6x8 text cells wide; the playfield
    // sits just below the (taller than original) status bar.
    public float X => Col * 6 + 6;
    public float Y => Row * 8 + 4 + World.PlayfieldOffset;
}

public sealed class Projectile
{
    public float X, Y, VY;
}

public sealed class Particle
{
    public float X, Y, VX, VY, Life;
    public Color Color;
}

public sealed class Boom
{
    public float X, Y, T;
    public Color Color;
}

/// <summary>
/// One game of The Ultra. The sixteen sheets use the aliens, movement paths and formations of the
/// original PSS game (see <see cref="OriginalData"/>): every alien steps one text cell at a time
/// along its sheet's path, wrapping at the screen edges exactly as on the Oric. The machine gun
/// overheats and its temperature carries from one sheet to the next.
/// </summary>
public sealed class World
{
    private enum Phase { Intro, Play, Dying, Clear, WellDone, Over }

    // Screen layout, following the Oric text screen: a status bar, then text rows 1-27 of play.
    private const float HudHeight = 12f;
    public const float PlayfieldOffset = HudHeight - 8f;
    private const float PlayTop = HudHeight;
    public const float PlayerY = 212f;

    // In-game sprites are drawn at 1.5x their pixel-art size.
    public const float SpriteScale = 1.5f;
    private const float ShotLength = 4f * SpriteScale;
    private const float PlayerHalfW = 9f * SpriteScale - 3f;
    private const float PlayerSpeed = 110f;
    private const float ShotSpeed = 260f;
    private const float FireInterval = 0.11f;
    private const float HeatPerShot = 7.5f;
    private const float Cooling = 26f;
    private const float OverheatCooling = 22f;
    private const float OverheatResume = 25f;
    private const int StartingLives = 5;
    private const int SheetBonus = 5000;
    private static readonly int[] BonusLivesAt = { 2000, 10000 };
    private static readonly int[] FrameSequence = { 0, 1, 2, 1 };

    public const int Sheets = 16;

    public static readonly string[] WaveNames =
    {
        "MARCHERS", "DIAMOND", "CONGA LINE", "BLOBS", "BOW TIES", "MUSHROOMS", "GIRDERS", "ARROWHEADS",
        "CROSSES", "SLASHERS", "BARCODES", "BIRDS", "SPARKS", "FROGS", "TEARDROPS", "THE ULTRA",
    };

    /// <summary>All the original aliens are green.</summary>
    public static readonly Color[] WaveColors = Enumerable.Repeat(Palette.Green, Sheets).ToArray();

    /// <summary>10 points a kill on sheet 1, 20 on sheet 2 and so on, capped at sheet 8.</summary>
    public static int PointsFor(int pattern, int cycle) => 10 * Math.Min(pattern + 1 + Sheets * cycle, 8);

    private readonly Sfx _sfx;
    private readonly Random _rng = new();
    private readonly List<Alien> _aliens = new();
    private readonly List<Projectile> _shots = new();
    private readonly List<Projectile> _bombs = new();
    private readonly List<Particle> _particles = new();
    private readonly List<Boom> _booms = new();
    private readonly Vector3[] _stars = new Vector3[40];

    private Phase _phase = Phase.Intro;
    private float _phaseTimer;
    private float _playerX = 120f;
    private bool _playerAlive = true;
    private float _invulnerable;
    private float _fireCooldown;
    private float _stepTimer, _bombTimer;
    private int _animStep;
    private int _bonusLivesGiven;
    private float _clock;

    // Collision box of the current sheet's alien, relative to its centre (already scaled).
    private float _alienLeft, _alienRight, _alienTop, _alienBottom;

    public int Score { get; private set; }
    public int Lives { get; private set; } = StartingLives;
    public int Wave { get; private set; } = 1;
    public float Heat { get; private set; }
    public bool Overheated { get; private set; }
    public bool IsFinished => _phase == Phase.Over && _phaseTimer <= 0f;
    public bool IsGameOver => _phase == Phase.Over;

    private int Pattern => (Wave - 1) % Sheets;
    private int Cycle => (Wave - 1) / Sheets;
    private float Mult => 1f + 0.18f * Cycle;

    public World(Sfx sfx)
    {
        _sfx = sfx;
        for (int i = 0; i < _stars.Length; i++)
            _stars[i] = new Vector3(_rng.Next(Renderer.Width), PlayTop + _rng.Next(200), 4 + _rng.Next(16));
        SetupWave();
    }

    /// <summary>Ends the game (player quit from the pause menu).</summary>
    public void Abandon()
    {
        _phase = Phase.Over;
        _phaseTimer = 0f;
    }

    // ------------------------------------------------------------------ update

    public void Update(float dt, InputManager input)
    {
        _clock += dt;
        UpdateStars(dt);
        UpdateEffects(dt);

        switch (_phase)
        {
            case Phase.Intro:
                _phaseTimer -= dt;
                if (_phaseTimer <= 0f)
                    _phase = Phase.Play;
                break;

            case Phase.Play:
                UpdatePlayer(dt, input);
                UpdateAliens(dt);
                UpdateShots(dt);
                UpdateBombs(dt, true);
                CheckCollisions();
                if (_phase == Phase.Play && _aliens.All(a => !a.Alive))
                {
                    _phase = Phase.Clear;
                    _phaseTimer = 1.6f;
                    _bombs.Clear();
                }
                break;

            case Phase.Dying:
                UpdateAliens(dt);
                UpdateShots(dt);
                UpdateBombs(dt, false);
                _phaseTimer -= dt;
                if (_phaseTimer <= 0f)
                    Respawn();
                break;

            case Phase.Clear:
                UpdateShots(dt);
                _phaseTimer -= dt;
                if (_phaseTimer <= 0f)
                {
                    if (Pattern == Sheets - 1)
                    {
                        // All sixteen sheets cleared.
                        _phase = Phase.WellDone;
                        _phaseTimer = 6f;
                        AddScore(SheetBonus);
                        _sfx.HighScore();
                    }
                    else
                        NextWave();
                }
                break;

            case Phase.WellDone:
                _phaseTimer -= dt;
                if (_phaseTimer <= 0f)
                    NextWave();
                break;

            case Phase.Over:
                _phaseTimer -= dt;
                break;
        }
    }

    private void NextWave()
    {
        Wave++;
        SetupWave();
    }

    private void UpdatePlayer(float dt, InputManager input)
    {
        if (_invulnerable > 0f)
            _invulnerable -= dt;

        _playerX = Math.Clamp(_playerX + input.Move * PlayerSpeed * dt, 14f, 226f);

        _fireCooldown -= dt;
        if (Overheated)
        {
            Heat -= OverheatCooling * dt;
            if (Heat <= OverheatResume)
                Overheated = false;
        }
        else
        {
            Heat -= Cooling * dt;
            if (input.Fire && _fireCooldown <= 0f && _shots.Count < 8)
            {
                _shots.Add(new Projectile { X = _playerX, Y = PlayerY - 12f - ShotLength });
                _fireCooldown = FireInterval;
                Heat += HeatPerShot;
                _sfx.Shoot();
                if (Heat >= 100f)
                {
                    Heat = 100f;
                    Overheated = true;
                    _sfx.Overheat();
                }
            }
        }
        Heat = Math.Max(0f, Heat);
    }

    /// <summary>The original movement engine: one text-cell step along the sheet's path.</summary>
    private void UpdateAliens(float dt)
    {
        _stepTimer -= dt;
        if (_stepTimer > 0f)
            return;
        _stepTimer += OriginalData.StepIntervals[Pattern] / Mult;
        _animStep++;

        var path = OriginalData.Paths[Pattern];
        foreach (var a in _aliens)
        {
            if (!a.Alive)
                continue;
            byte dir = path[a.PathIndex];
            a.PathIndex = (a.PathIndex + 1) % path.Length;
            Step(a, dir);
        }
    }

    private static void Step(Alien a, byte dir)
    {
        switch (dir & 7)
        {
            case 0: a.Col--; break;
            case 1: a.Col--; a.Row--; break;
            case 2: a.Row--; break;
            case 3: a.Col++; a.Row--; break;
            case 4: a.Col++; break;
            case 5: a.Col++; a.Row++; break;
            case 6: a.Row++; break;
            case 7: a.Col--; a.Row++; break;
        }

        // Wrap as the Oric does: across the 37 playable columns and down past the bottom to row 1.
        if (a.Col < 2) a.Col += 37;
        if (a.Col > 38) a.Col -= 37;
        if (a.Row > 27) a.Row -= 27;
        if (a.Row < 1) a.Row += 27;
    }

    private void UpdateShots(float dt)
    {
        foreach (var s in _shots)
            s.Y -= ShotSpeed * dt;
        _shots.RemoveAll(s => s.Y < PlayTop);
    }

    private void UpdateBombs(float dt, bool spawn)
    {
        foreach (var b in _bombs)
            b.Y += b.VY * dt;
        _bombs.RemoveAll(b => b.Y > Renderer.Height);

        if (!spawn)
            return;

        _bombTimer -= dt;
        if (_bombTimer > 0f)
            return;

        float interval = Math.Max(0.35f, 1.2f - Pattern * 0.04f - Cycle * 0.1f);
        _bombTimer = interval * (0.7f + (float)_rng.NextDouble() * 0.6f);

        if (_bombs.Count >= 3 + Wave / 4)
            return;

        var candidates = _aliens.Where(a => a.Alive && a.Y < PlayerY - 30f).ToList();
        if (candidates.Count == 0)
            return;

        var shooter = _rng.Next(2) == 0
            ? candidates.OrderBy(a => Math.Abs(a.X - _playerX)).First()
            : candidates[_rng.Next(candidates.Count)];

        _bombs.Add(new Projectile { X = shooter.X, Y = shooter.Y + _alienBottom, VY = (75f + Pattern * 3f) * Mult });
    }

    private bool HitsAlien(Alien a, float x, float top, float bottom) =>
        x >= a.X + _alienLeft && x <= a.X + _alienRight && bottom >= a.Y + _alienTop && top <= a.Y + _alienBottom;

    private void CheckCollisions()
    {
        foreach (var s in _shots)
        {
            foreach (var a in _aliens)
            {
                if (a.Alive && HitsAlien(a, s.X, s.Y, s.Y + ShotLength))
                {
                    KillAlien(a);
                    s.Y = -100f;
                    break;
                }
            }
        }
        _shots.RemoveAll(s => s.Y < -50f);

        if (!_playerAlive || _invulnerable > 0f)
            return;

        foreach (var b in _bombs)
        {
            if (Math.Abs(b.X - _playerX) <= PlayerHalfW && b.Y + 3f >= PlayerY - 10f && b.Y - 3f <= PlayerY + 12f)
            {
                KillPlayer();
                return;
            }
        }

        // An alien flying into the ship destroys it, as in the original.
        foreach (var a in _aliens)
        {
            if (!a.Alive)
                continue;
            bool overlapX = a.X + _alienRight >= _playerX - PlayerHalfW && a.X + _alienLeft <= _playerX + PlayerHalfW;
            bool overlapY = a.Y + _alienBottom >= PlayerY - 10f && a.Y + _alienTop <= PlayerY + 12f;
            if (overlapX && overlapY)
            {
                KillPlayer();
                return;
            }
        }
    }

    private void KillAlien(Alien a)
    {
        a.Alive = false;
        _booms.Add(new Boom { X = a.X, Y = a.Y, Color = Palette.Green });
        Burst(a.X, a.Y, 14, Palette.Green, 70f);
        _sfx.AlienExplode();
        AddScore(PointsFor(Pattern, Cycle));
    }

    private void AddScore(int points)
    {
        Score += points;
        while (_bonusLivesGiven < BonusLivesAt.Length && Score >= BonusLivesAt[_bonusLivesGiven])
        {
            _bonusLivesGiven++;
            Lives++;
            _sfx.ExtraLife();
        }
    }

    private void KillPlayer()
    {
        _playerAlive = false;
        _phase = Phase.Dying;
        _phaseTimer = 2.2f;
        Burst(_playerX, PlayerY, 40, Palette.Cyan, 120f);
        Burst(_playerX, PlayerY, 25, Palette.Yellow, 80f);
        _booms.Add(new Boom { X = _playerX, Y = PlayerY, Color = Palette.White });
        _sfx.PlayerExplode();
    }

    private void Respawn()
    {
        Lives--;
        if (Lives <= 0)
        {
            _phase = Phase.Over;
            _phaseTimer = 3f;
            return;
        }

        _playerX = 120f;
        _playerAlive = true;
        _invulnerable = 2f;
        Heat = 0f;
        Overheated = false;
        _bombs.Clear();
        _shots.Clear();
        _phase = Phase.Play;
    }

    private void Burst(float x, float y, int count, Color color, float speed)
    {
        for (int i = 0; i < count; i++)
        {
            float ang = (float)(_rng.NextDouble() * Math.PI * 2);
            float sp = speed * (0.3f + (float)_rng.NextDouble());
            _particles.Add(new Particle
            {
                X = x, Y = y, VX = MathF.Cos(ang) * sp, VY = MathF.Sin(ang) * sp,
                Life = 0.4f + (float)_rng.NextDouble() * 0.6f, Color = color,
            });
        }
    }

    private void UpdateEffects(float dt)
    {
        foreach (var p in _particles)
        {
            p.X += p.VX * dt;
            p.Y += p.VY * dt;
            p.VY += 40f * dt;
            p.Life -= dt;
        }
        _particles.RemoveAll(p => p.Life <= 0f);

        foreach (var b in _booms)
            b.T += dt;
        _booms.RemoveAll(b => b.T > 0.35f);
    }

    private void UpdateStars(float dt)
    {
        // Stars stream past faster during the hyperspace jump.
        float speed = _phase == Phase.Intro ? 12f : 1f;
        for (int i = 0; i < _stars.Length; i++)
        {
            var s = _stars[i];
            s.Y += s.Z * speed * dt;
            if (s.Y > Renderer.Height)
            {
                s.Y = PlayTop;
                s.X = _rng.Next(Renderer.Width);
            }
            _stars[i] = s;
        }
    }

    private void SetupWave()
    {
        _aliens.Clear();
        _shots.Clear();
        _bombs.Clear();
        _stepTimer = 0f;
        _bombTimer = 1.5f;
        _animStep = 0;

        foreach (var (row, col, index) in OriginalData.Starts[Pattern])
            _aliens.Add(new Alien { Row = row, Col = col, PathIndex = index });

        // Collision box from the union of the sheet's animation frames.
        int x0 = 12, x1 = -1, y0 = 8, y1 = -1;
        foreach (var frame in OriginalData.AlienFrames[Pattern])
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 12; x++)
                    if (frame[y][x] == 'X')
                    {
                        x0 = Math.Min(x0, x); x1 = Math.Max(x1, x);
                        y0 = Math.Min(y0, y); y1 = Math.Max(y1, y);
                    }
        if (x1 < 0) { x0 = 0; x1 = 11; y0 = 0; y1 = 7; }
        _alienLeft = (x0 - 6f) * SpriteScale;
        _alienRight = (x1 + 1 - 6f) * SpriteScale;
        _alienTop = (y0 - 4f) * SpriteScale;
        _alienBottom = (y1 + 1 - 4f) * SpriteScale;

        _phase = Phase.Intro;
        _phaseTimer = 2.4f;
        _sfx.WaveStart();
    }

    // ------------------------------------------------------------------ draw

    public void Draw(Renderer r, int highScore)
    {
        var art = r.Art;

        foreach (var s in _stars)
        {
            var c = s.Z > 14 ? Palette.White : s.Z > 9 ? Palette.Cyan : Palette.Green;
            float len = _phase == Phase.Intro ? 4f : 1f;
            r.Rect((int)s.X, (int)s.Y, 1, len, c * 0.7f);
        }

        if (_phase != Phase.Intro && _phase != Phase.WellDone)
        {
            int frame = FrameSequence[_animStep % FrameSequence.Length];
            var tex = art.Alien[Pattern, frame];
            foreach (var a in _aliens)
                if (a.Alive)
                    r.Sprite(tex, a.X, a.Y, Palette.Green, SpriteScale);
        }

        foreach (var b in _booms)
            r.Sprite(art.Explosion[b.T < 0.15f ? 0 : 1], b.X, b.Y, b.Color, SpriteScale);

        foreach (var s in _shots)
            r.Rect((int)s.X - 0.5f, (int)s.Y, SpriteScale, ShotLength, Palette.White);

        foreach (var b in _bombs)
            r.Sprite(art.Bomb[0], b.X, b.Y, Palette.Green, SpriteScale);

        if (_playerAlive && (_invulnerable <= 0f || (int)(_invulnerable * 10f) % 2 == 0))
        {
            // During the hyperspace jump the ship flies up from below the screen.
            float y = _phase == Phase.Intro ? PlayerY + Math.Max(0f, _phaseTimer - 0.6f) * 40f : PlayerY;
            r.Sprite(art.Player, _playerX, y, Palette.Cyan, SpriteScale);
        }

        foreach (var p in _particles)
            r.Rect((int)p.X, (int)p.Y, SpriteScale, SpriteScale, p.Color);

        DrawHud(r, highScore);

        bool flash = (int)(_clock * 4f) % 2 == 0;
        switch (_phase)
        {
            case Phase.Intro:
                r.Rect(24, 110, 192, 10, Palette.Blue);
                r.TextCentered("PREPARE FOR HYPERSPACE JUMP", 111, Palette.White);
                r.TextCentered($"SHEET {Wave:D2} - {WaveNames[Pattern]}", 132, flash ? Palette.Yellow : Palette.White);
                r.TextCentered($"{PointsFor(Pattern, Cycle)} POINTS EACH", 144, Palette.Cyan);
                break;
            case Phase.Clear:
                r.TextCentered("SHEET CLEARED", 108, flash ? Palette.Green : Palette.White);
                break;
            case Phase.WellDone:
                r.TextCentered("WELL DONE !", 56, Palette.Yellow, 2f);
                r.TextCentered("THE EVIL ULTRA HAVE BEEN WIPED OUT.", 84, Palette.White);
                r.TextCentered("FOR THIS FEAT OF TREMENDOUS BRAVERY", 100, Palette.White);
                r.TextCentered("AND HEROISM YOU EARN YOURSELF A", 110, Palette.White);
                r.TextCentered($"BONUS OF {SheetBonus} POINTS", 126, Palette.Green);
                r.TextCentered("STAND-BY FOR YOUR NEXT MISSION", 150, flash ? Palette.Cyan : Palette.White);
                break;
            case Phase.Over:
                r.Rect(60, 92, 120, 28, Palette.Black);
                r.Frame(60, 92, 120, 28, Palette.Red);
                r.TextCentered("GAME OVER", 98, Palette.Red, 2f);
                break;
        }

        if (Overheated && _phase == Phase.Play && flash)
            r.TextCentered("OVERHEATED", HudHeight + 4, Palette.Red);
    }

    /// <summary>The original's status bar: black on red, with the contents centred.</summary>
    private void DrawHud(Renderer r, int highScore)
    {
        const float gaugeW = 40f, gap = 6f;
        string text = $"SCORE {Score:D6} HI {Math.Max(highScore, Score):D6} LIVES {Math.Min(Lives, 9)}";
        float textW = PixelFont.Measure(text);
        float x = (Renderer.Width - (textW + gap + gaugeW + 1f)) / 2f;
        float textY = (HudHeight - 7f) / 2f;

        r.Rect(0, 0, Renderer.Width, HudHeight, Palette.Red);
        r.Text(text, x, textY, Palette.Black);

        // Heat gauge: a growing black bar, flashing yellow when the gun locks.
        float gaugeX = x + textW + gap;
        var gauge = Overheated && (int)(_clock * 8f) % 2 == 0 ? Palette.Yellow : Palette.Black;
        r.Rect(gaugeX, textY + 1f, gaugeW * Heat / 100f, 5f, gauge);
        r.Rect(gaugeX + gaugeW, textY, 1f, 7f, Palette.Black);
    }
}
