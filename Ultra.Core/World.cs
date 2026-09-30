using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Ultra.Core;

public enum AlienState { Formation, Charging, Diving, Returning }

public sealed class Alien
{
    public float X, Y, HomeX, HomeY, VX, VY, T;
    public int Index, Row, Col, Group;
    public bool Alive = true, Visible = true, Entered;
    public AlienState State;
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
/// One game of The Ultra: sixteen waves of aliens, each with its own graphics and attack
/// pattern, an over-heating machine gun whose temperature carries from wave to wave, and
/// three lives. After wave 16 the cycle repeats faster and for more points.
/// </summary>
public sealed class World
{
    private enum Phase { Intro, Play, Dying, Clear, Over }

    public const float PlayerY = 194f;
    private const float PlayTop = 18f;
    private const float PlayerSpeed = 95f;
    private const float ShotSpeed = 260f;
    private const float FireInterval = 0.11f;
    private const float HeatPerShot = 7.5f;
    private const float Cooling = 26f;
    private const float OverheatCooling = 22f;
    private const float OverheatResume = 25f;
    private const int BonusLifeEvery = 10000;

    public static readonly string[] WaveNames =
    {
        "MARCHERS", "WAVERS", "SWOOPERS", "BOUNCERS", "ORBITERS", "ZIGZAGGERS", "LOOPERS", "KAMIKAZES",
        "CENTIPEDES", "SPIRALLERS", "RAINDROPS", "PENDULUMS", "PHANTOMS", "CROSSFIRE", "HUNTERS", "THE ULTRA",
    };

    public static readonly Color[] WaveColors =
    {
        Palette.Green, Palette.Magenta, Palette.Yellow, Palette.Cyan, Palette.Red, Palette.White,
        Palette.Magenta, Palette.Yellow, Palette.Green, Palette.Cyan, Palette.White, Palette.Red,
        Palette.Magenta, Palette.Cyan, Palette.Yellow, Palette.Red,
    };

    public static int PointsFor(int pattern, int cycle) => (10 + 5 * pattern) * (cycle + 1);

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
    private float _waveTime;
    private float _playerX = 120f;
    private bool _playerAlive = true;
    private float _invulnerable;
    private float _fireCooldown;
    private float _groupX, _groupY, _groupDir = 1f;
    private float _diveTimer, _bombTimer, _stepTimer;
    private int _nextBonus = BonusLifeEvery;
    private float _clock;

    public int Score { get; private set; }
    public int Lives { get; private set; } = 3;
    public int Wave { get; private set; } = 1;
    public float Heat { get; private set; }
    public bool Overheated { get; private set; }
    public bool IsFinished => _phase == Phase.Over && _phaseTimer <= 0f;
    public bool IsGameOver => _phase == Phase.Over;

    private int Pattern => (Wave - 1) % 16;
    private int Cycle => (Wave - 1) / 16;
    private float Mult => 1f + 0.18f * Cycle;

    public World(Sfx sfx)
    {
        _sfx = sfx;
        for (int i = 0; i < _stars.Length; i++)
            _stars[i] = new Vector3(_rng.Next(Renderer.Width), PlayTop + _rng.Next(190), 4 + _rng.Next(16));
        SetupWave();
    }

    /// <summary>Ends the game immediately (player quit from the pause menu).</summary>
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
                _waveTime += dt;
                UpdatePlayer(dt, input);
                UpdatePattern(dt);
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
                _waveTime += dt;
                UpdatePattern(dt);
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
                    Wave++;
                    SetupWave();
                }
                break;

            case Phase.Over:
                _phaseTimer -= dt;
                break;
        }
    }

    private void UpdatePlayer(float dt, InputManager input)
    {
        if (_invulnerable > 0f)
            _invulnerable -= dt;

        float dir = (input.Left ? -1f : 0f) + (input.Right ? 1f : 0f);
        _playerX = Math.Clamp(_playerX + dir * PlayerSpeed * dt, 10f, 230f);

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
                _shots.Add(new Projectile { X = _playerX, Y = PlayerY - 8 });
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

    private void UpdateShots(float dt)
    {
        foreach (var s in _shots)
            s.Y -= ShotSpeed * dt;
        _shots.RemoveAll(s => s.Y < PlayTop - 4);
    }

    private void UpdateBombs(float dt, bool spawn)
    {
        foreach (var b in _bombs)
            b.Y += b.VY * dt;
        _bombs.RemoveAll(b => b.Y > 210);

        if (!spawn)
            return;

        _bombTimer -= dt;
        if (_bombTimer > 0f)
            return;

        float interval = Math.Max(0.3f, 1.2f - Pattern * 0.04f - Cycle * 0.1f);
        if (Pattern == 14)
            interval *= 0.7f;
        _bombTimer = interval * (0.7f + (float)_rng.NextDouble() * 0.6f);

        if (_bombs.Count >= 3 + Wave / 4)
            return;

        var candidates = _aliens.Where(a => a.Alive && a.Visible && a.Y > PlayTop + 2 && a.Y < 170 && a.X > 4 && a.X < 236).ToList();
        if (candidates.Count == 0)
            return;

        var shooter = _rng.Next(2) == 0
            ? candidates.OrderBy(a => Math.Abs(a.X - _playerX)).First()
            : candidates[_rng.Next(candidates.Count)];

        _bombs.Add(new Projectile { X = shooter.X, Y = shooter.Y + 5, VY = (75f + Pattern * 3f) * Mult });
    }

    private void CheckCollisions()
    {
        foreach (var s in _shots)
        {
            foreach (var a in _aliens)
            {
                if (!a.Alive || !a.Visible)
                    continue;
                if (Math.Abs(s.X - a.X) <= 6f && s.Y <= a.Y + 4f && s.Y + 4f >= a.Y - 4f)
                {
                    KillAlien(a, true);
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
            if (Math.Abs(b.X - _playerX) <= 6f && b.Y + 2f >= PlayerY - 3f && b.Y - 2f <= PlayerY + 4f)
            {
                KillPlayer();
                return;
            }
        }

        foreach (var a in _aliens)
        {
            if (a.Alive && a.Visible && Math.Abs(a.X - _playerX) < 11f && Math.Abs(a.Y - PlayerY) < 8f)
            {
                KillAlien(a, false);
                KillPlayer();
                return;
            }
        }
    }

    private void KillAlien(Alien a, bool award)
    {
        a.Alive = false;
        var color = WaveColors[Pattern];
        _booms.Add(new Boom { X = a.X, Y = a.Y, Color = color });
        Burst(a.X, a.Y, 10, color, 50f);
        _sfx.AlienExplode();

        if (!award)
            return;

        int points = PointsFor(Pattern, Cycle);
        if (a.State == AlienState.Diving || a.State == AlienState.Charging)
            points *= 2;
        Score += points;

        if (Score >= _nextBonus)
        {
            Lives++;
            _nextBonus += BonusLifeEvery;
            _sfx.ExtraLife();
        }
    }

    private void KillPlayer()
    {
        _playerAlive = false;
        _phase = Phase.Dying;
        _phaseTimer = 2.2f;
        Burst(_playerX, PlayerY, 40, Palette.Green, 90f);
        Burst(_playerX, PlayerY, 25, Palette.Yellow, 60f);
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
        foreach (var a in _aliens.Where(a => a.State is AlienState.Diving or AlienState.Charging))
        {
            a.State = AlienState.Returning;
            a.Y = -8f;
        }
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
        for (int i = 0; i < _stars.Length; i++)
        {
            var s = _stars[i];
            s.Y += s.Z * dt;
            if (s.Y > 206)
            {
                s.Y = PlayTop;
                s.X = _rng.Next(Renderer.Width);
            }
            _stars[i] = s;
        }
    }

    // ------------------------------------------------------------------ waves

    private void SetupWave()
    {
        _aliens.Clear();
        _shots.Clear();
        _bombs.Clear();
        _waveTime = 0f;
        _groupX = _groupY = 0f;
        _groupDir = 1f;
        _diveTimer = 2f;
        _bombTimer = 1.5f;
        _stepTimer = 0f;
        float m = Mult;

        switch (Pattern)
        {
            case 0: Grid(4, 6); break;
            case 1: Grid(3, 7); break;
            case 2: Grid(3, 7); break;
            case 3:
                for (int i = 0; i < 14; i++)
                {
                    float ang = (float)(_rng.NextDouble() * Math.PI * 2);
                    var a = Add(20 + _rng.Next(200), 24 + _rng.Next(100));
                    a.VX = MathF.Cos(ang) * 55f * m;
                    a.VY = MathF.Sin(ang) * 55f * m;
                    if (Math.Abs(a.VY) < 15f)
                        a.VY = 20f * m;
                }
                break;
            case 4:
                for (int i = 0; i < 16; i++)
                    Add(0, 0).Group = i / 4;
                break;
            case 5:
                for (int i = 0; i < 18; i++)
                    Add(20 + i % 6 * 40, 20 + i / 6 * 24).VX = (i / 6 % 2 == 0 ? 1f : -1f) * 50f * m;
                break;
            case 6:
                for (int i = 0; i < 16; i++)
                    Add(0, 0);
                break;
            case 7: Grid(3, 6); break;
            case 8:
                for (int i = 0; i < 20; i++)
                {
                    int chain = i / 10, k = i % 10;
                    var a = chain == 0 ? Add(-8 - k * 13, 24) : Add(248 + k * 13, 48);
                    a.VX = (chain == 0 ? 70f : -70f) * m;
                    a.Group = chain;
                }
                break;
            case 9:
                for (int i = 0; i < 16; i++)
                    Add(0, 0);
                break;
            case 10:
                for (int i = 0; i < 16; i++)
                    Add(12 + _rng.Next(216), -10 - _rng.Next(180)).VY = (30f + _rng.Next(30)) * m;
                break;
            case 11: Grid(3, 6, 34f, 14f, 0f); break;
            case 12:
                for (int i = 0; i < 16; i++)
                    Add(12 + _rng.Next(216), 26 + _rng.Next(124)).T = 0.5f + (float)_rng.NextDouble() * 2f;
                break;
            case 13:
                for (int i = 0; i < 18; i++)
                {
                    int band = i / 9, k = i % 9;
                    Add(k * 30 + band * 15, 0).Group = band;
                }
                break;
            case 14: Grid(3, 6); break;
            case 15: Grid(4, 6); break;
        }

        UpdatePattern(0f);
        _phase = Phase.Intro;
        _phaseTimer = 2.2f;
        _sfx.WaveStart();
    }

    private Alien Add(float x, float y)
    {
        var a = new Alien { X = x, Y = y, HomeX = x, HomeY = y, Index = _aliens.Count };
        _aliens.Add(a);
        return a;
    }

    private void Grid(int rows, int cols, float sx = 18f, float sy = 14f, float top = 30f)
    {
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                var a = Add(120f - (cols - 1) * sx / 2f + c * sx, top + r * sy);
                a.Row = r;
                a.Col = c;
            }
    }

    private void UpdatePattern(float dt)
    {
        float t = _waveTime;
        float m = Mult;

        switch (Pattern)
        {
            case 0:
                Marchers(dt, m);
                break;

            case 1:
                foreach (var a in _aliens)
                {
                    a.X = a.HomeX + MathF.Sin(t * 1.6f * m + a.Row * 0.9f) * 34f;
                    a.Y = a.HomeY + 16f + MathF.Sin(t * 0.6f) * 18f;
                }
                break;

            case 2:
                Swoopers(dt, m, 1, 1.8f, 0.5f, 20f);
                break;

            case 3:
                foreach (var a in _aliens)
                {
                    a.X += a.VX * dt;
                    a.Y += a.VY * dt;
                    if (a.X < 8f) a.VX = Math.Abs(a.VX);
                    if (a.X > 232f) a.VX = -Math.Abs(a.VX);
                    if (a.Y < 22f) a.VY = Math.Abs(a.VY);
                    if (a.Y > 170f) a.VY = -Math.Abs(a.VY);
                }
                break;

            case 4:
                foreach (var a in _aliens)
                {
                    int g = a.Group, k = a.Index % 4;
                    float cx = 40f + g * 53f + MathF.Sin(t * 0.7f * m + g) * 20f;
                    float cy = 70f + MathF.Sin(t * 0.9f * m + g * 1.3f) * 30f;
                    float ang = t * 2.2f * m * (g % 2 == 0 ? 1f : -1f) + k * MathF.PI / 2f;
                    a.X = cx + MathF.Cos(ang) * 16f;
                    a.Y = cy + MathF.Sin(ang) * 16f;
                }
                break;

            case 5:
                foreach (var a in _aliens)
                {
                    a.X += a.VX * dt;
                    if (a.X < 8f) a.VX = Math.Abs(a.VX);
                    if (a.X > 232f) a.VX = -Math.Abs(a.VX);
                    a.Y += 12f * m * dt;
                    if (a.Y > 172f) a.Y = 20f;
                }
                break;

            case 6:
                foreach (var a in _aliens)
                {
                    float th = t * 0.75f * m - a.Index * 0.3f;
                    a.X = 120f + 104f * MathF.Sin(th);
                    a.Y = 95f + 55f * MathF.Sin(2f * th);
                }
                break;

            case 7:
                Kamikazes(dt, m);
                break;

            case 8:
                foreach (var a in _aliens)
                {
                    a.X += a.VX * dt;
                    if (!a.Entered && a.X >= 8f && a.X <= 232f)
                        a.Entered = true;
                    if (a.Entered)
                    {
                        if (a.X > 232f && a.VX > 0f) { a.X = 232f; a.VX = -a.VX; a.Y += 12f; }
                        if (a.X < 8f && a.VX < 0f) { a.X = 8f; a.VX = -a.VX; a.Y += 12f; }
                    }
                    if (a.Y > 172f) a.Y = 24f;
                }
                break;

            case 9:
                foreach (var a in _aliens)
                {
                    float th = t * 1.3f * m + a.Index * MathF.PI * 2f / 16f;
                    float r = 25f + 55f * (0.5f + 0.5f * MathF.Sin(t * 0.8f));
                    a.X = 120f + r * MathF.Cos(th) * 1.4f;
                    a.Y = 95f + r * MathF.Sin(th) * 0.9f;
                }
                break;

            case 10:
                foreach (var a in _aliens)
                {
                    a.Y += a.VY * dt;
                    a.X += MathF.Sin(t * 2f + a.Index) * 15f * dt;
                    if (a.Y > 176f)
                    {
                        a.Y = -10f - _rng.Next(40);
                        a.X = 12 + _rng.Next(216);
                    }
                }
                break;

            case 11:
                foreach (var a in _aliens)
                {
                    float len = 36f + a.Row * 16f;
                    float ang = 0.45f * MathF.Sin(t * 1.4f * m + a.Col * 0.45f);
                    a.X = a.HomeX + len * MathF.Sin(ang);
                    a.Y = 8f + len * MathF.Cos(ang);
                }
                break;

            case 12:
                foreach (var a in _aliens)
                {
                    a.T -= dt;
                    a.Visible = a.T >= 0.45f || (int)(a.T * 20f) % 2 == 0;
                    if (a.T <= 0f)
                    {
                        a.X = 12 + _rng.Next(216);
                        a.Y = 26 + _rng.Next(124);
                        a.T = (1.2f + (float)_rng.NextDouble() * 1.8f) / m;
                        a.Visible = true;
                    }
                }
                break;

            case 13:
                foreach (var a in _aliens)
                {
                    a.X += (a.Group == 0 ? 1f : -1f) * 55f * m * dt;
                    if (a.X > 255f) a.X -= 270f;
                    if (a.X < -15f) a.X += 270f;
                    float band = a.Group == 0 ? 45f + MathF.Sin(t * 0.4f) * 22f : 100f + MathF.Sin(t * 0.4f + MathF.PI) * 22f;
                    a.Y = band + MathF.Sin(t * 3f + a.Index) * 4f;
                }
                break;

            case 14:
            {
                float target = Math.Clamp(_playerX - 120f, -60f, 60f);
                float step = 35f * m * dt;
                _groupX += Math.Clamp(target - _groupX, -step, step);
                _groupY = 20f + MathF.Sin(t * 0.5f) * 20f;
                foreach (var a in _aliens)
                {
                    a.X = a.HomeX + _groupX;
                    a.Y = a.HomeY + _groupY + MathF.Sin(t * 2.5f + a.Col * 0.7f) * 5f;
                }
                break;
            }

            case 15:
                Swoopers(dt, m, 3, 0.9f, 0.7f, 14f);
                break;
        }
    }

    private void Marchers(float dt, float m)
    {
        var alive = _aliens.Where(a => a.Alive).ToList();
        if (alive.Count == 0)
            return;

        float speed = (12f + (24 - alive.Count) * 2.2f) * m;
        _groupX += _groupDir * speed * dt;

        float minX = alive.Min(a => a.HomeX) + _groupX;
        float maxX = alive.Max(a => a.HomeX) + _groupX;
        if (minX < 10f && _groupDir < 0f)
        {
            _groupDir = 1f;
            _groupY += 6f;
        }
        else if (maxX > 230f && _groupDir > 0f)
        {
            _groupDir = -1f;
            _groupY += 6f;
        }

        foreach (var a in _aliens)
        {
            a.X = a.HomeX + _groupX;
            a.Y = a.HomeY + _groupY;
        }

        if (dt > 0f)
        {
            _stepTimer -= dt;
            if (_stepTimer <= 0f)
            {
                _stepTimer = 0.15f + 0.5f * alive.Count / 24f;
                _sfx.Step();
            }
        }

        if (_playerAlive && alive.Max(a => a.Y) >= PlayerY - 12f)
        {
            _groupY = 0f;
            KillPlayer();
        }
    }

    private void Swoopers(float dt, float m, int maxDivers, float interval, float swaySpeed, float sway)
    {
        _groupX = MathF.Sin(_waveTime * swaySpeed) * sway;

        if (dt > 0f && _phase == Phase.Play)
        {
            _diveTimer -= dt;
            if (_diveTimer <= 0f)
            {
                _diveTimer = interval / m;
                int diving = _aliens.Count(a => a.Alive && a.State != AlienState.Formation);
                var ready = _aliens.Where(a => a.Alive && a.State == AlienState.Formation).ToList();
                if (diving < maxDivers && ready.Count > 0)
                {
                    var d = ready[_rng.Next(ready.Count)];
                    d.State = AlienState.Diving;
                    d.VX = d.X < 120f ? -40f : 40f;
                    d.VY = -50f;
                }
            }
        }

        foreach (var a in _aliens)
        {
            if (!a.Alive)
                continue;

            switch (a.State)
            {
                case AlienState.Formation:
                    a.X = a.HomeX + _groupX;
                    a.Y = a.HomeY;
                    break;

                case AlienState.Diving:
                    a.VY = Math.Min(a.VY + 160f * m * dt, 120f * m);
                    a.VX = Math.Clamp(a.VX + Math.Sign(_playerX - a.X) * 120f * dt, -80f, 80f);
                    a.X += a.VX * dt;
                    a.Y += a.VY * dt;
                    if (a.Y > 216f)
                    {
                        a.Y = -8f;
                        a.State = AlienState.Returning;
                    }
                    break;

                case AlienState.Returning:
                    ReturnHome(a, dt, 80f * m);
                    break;
            }
        }
    }

    private void Kamikazes(float dt, float m)
    {
        _groupX = MathF.Sin(_waveTime * 0.8f) * 10f;

        if (dt > 0f && _phase == Phase.Play)
        {
            _diveTimer -= dt;
            if (_diveTimer <= 0f)
            {
                _diveTimer = 1.3f / m;
                int busy = _aliens.Count(a => a.Alive && a.State != AlienState.Formation);
                var ready = _aliens.Where(a => a.Alive && a.State == AlienState.Formation).ToList();
                if (busy < 3 && ready.Count > 0)
                {
                    var k = ready[_rng.Next(ready.Count)];
                    k.State = AlienState.Charging;
                    k.T = 0f;
                }
            }
        }

        foreach (var a in _aliens)
        {
            if (!a.Alive)
                continue;

            switch (a.State)
            {
                case AlienState.Formation:
                    a.X = a.HomeX + _groupX;
                    a.Y = a.HomeY;
                    break;

                case AlienState.Charging:
                    a.T += dt;
                    a.X = a.HomeX + _groupX + MathF.Sin(a.T * 60f) * 1.5f;
                    a.Y = a.HomeY;
                    if (a.T > 0.5f)
                    {
                        var dir = new Vector2(_playerX - a.X, PlayerY - a.Y);
                        if (dir.LengthSquared() < 1f)
                            dir = Vector2.UnitY;
                        dir.Normalize();
                        a.VX = dir.X * 150f * m;
                        a.VY = Math.Max(dir.Y, 0.4f) * 150f * m;
                        a.State = AlienState.Diving;
                    }
                    break;

                case AlienState.Diving:
                    a.X += a.VX * dt;
                    a.Y += a.VY * dt;
                    if (a.Y > 216f || a.X < -12f || a.X > 252f)
                    {
                        a.X = a.HomeX + _groupX;
                        a.Y = -8f;
                        a.State = AlienState.Returning;
                    }
                    break;

                case AlienState.Returning:
                    ReturnHome(a, dt, 80f * m);
                    break;
            }
        }
    }

    private void ReturnHome(Alien a, float dt, float speed)
    {
        var home = new Vector2(a.HomeX + _groupX, a.HomeY);
        var delta = home - new Vector2(a.X, a.Y);
        float dist = delta.Length();
        if (dist <= speed * dt || dist < 2f)
        {
            a.X = home.X;
            a.Y = home.Y;
            a.State = AlienState.Formation;
        }
        else
        {
            delta /= dist;
            a.X += delta.X * speed * dt;
            a.Y += delta.Y * speed * dt;
        }
    }

    // ------------------------------------------------------------------ draw

    public void Draw(Renderer r, int highScore)
    {
        var art = r.Art;

        for (int i = 0; i < _stars.Length; i++)
        {
            var s = _stars[i];
            var c = s.Z > 14 ? Palette.White : s.Z > 9 ? Palette.Cyan : Palette.Blue;
            r.Rect((int)s.X, (int)s.Y, 1, 1, c * 0.8f);
        }

        var alienColor = WaveColors[Pattern];
        foreach (var a in _aliens)
        {
            if (!a.Alive || !a.Visible || a.Y < PlayTop - 4)
                continue;
            int frame = (int)((_clock + a.Index * 0.1f) * 4f) % 2;
            r.Sprite(art.Alien[Pattern, frame], a.X, a.Y, alienColor);
        }

        foreach (var b in _booms)
            r.Sprite(art.Explosion[b.T < 0.15f ? 0 : 1], b.X, b.Y, b.Color);

        foreach (var s in _shots)
            r.Rect((int)s.X, (int)s.Y, 1, 4, Palette.Yellow);

        int bombFrame = (int)(_clock * 8f) % 2;
        foreach (var b in _bombs)
            r.Sprite(art.Bomb[bombFrame], b.X, b.Y, bombFrame == 0 ? Palette.Red : Palette.White);

        if (_playerAlive && (_invulnerable <= 0f || (int)(_invulnerable * 10f) % 2 == 0))
            r.Sprite(art.Player, _playerX, PlayerY, Palette.Green);

        foreach (var p in _particles)
            r.Rect((int)p.X, (int)p.Y, 1, 1, p.Color);

        DrawHud(r, highScore);

        bool flash = (int)(_clock * 4f) % 2 == 0;
        switch (_phase)
        {
            case Phase.Intro:
                r.TextCentered($"WAVE {Wave:D2}", 92, flash ? Palette.White : Palette.Yellow, 2f);
                r.TextCentered(WaveNames[Pattern], 114, alienColor);
                r.TextCentered($"{PointsFor(Pattern, Cycle)} PTS EACH", 126, Palette.Cyan);
                break;
            case Phase.Clear:
                r.TextCentered("WAVE CLEARED", 100, flash ? Palette.Green : Palette.White);
                break;
            case Phase.Over:
                r.Rect(60, 92, 120, 28, Palette.Black);
                r.Frame(60, 92, 120, 28, Palette.Red);
                r.TextCentered("GAME OVER", 98, Palette.Red, 2f);
                break;
        }
    }

    private void DrawHud(Renderer r, int highScore)
    {
        r.Text($"SCORE {Score:D6}", 4, 4, Palette.White);
        r.Text($"HI {Math.Max(highScore, Score):D6}", 150, 4, Palette.Yellow);
        r.Rect(0, 14, Renderer.Width, 1, Palette.Blue);
        r.Rect(0, 208, Renderer.Width, 1, Palette.Blue);

        r.Text($"W{Wave:D2}", 4, 213, Palette.Cyan);

        r.Text("HEAT", 34, 213, Palette.White);
        r.Frame(60, 212, 82, 9, Palette.White);
        var heatColor = Overheated
            ? ((int)(_clock * 8f) % 2 == 0 ? Palette.Red : Palette.Yellow)
            : Heat < 50f ? Palette.Green : Heat < 80f ? Palette.Yellow : Palette.Red;
        r.Rect(61, 213, 80f * Heat / 100f, 7, heatColor);
        if (Overheated)
            r.Text("HOT!", 146, 213, heatColor);

        int shown = Math.Min(Lives - (_playerAlive ? 1 : 0), 4);
        for (int i = 0; i < shown; i++)
            r.Sprite(r.Art.Player, 228 - i * 17, 216, Palette.Green);
    }
}
