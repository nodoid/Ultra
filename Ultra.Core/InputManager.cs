using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace Ultra.Core;

/// <summary>
/// Merges tilt steering, multi-touch, mouse, keyboard and game pad input into one
/// per-frame snapshot. On phones and tablets tilting the device steers and touching anywhere
/// fires; on a desktop the ship follows the mouse and a click fires.
/// </summary>
public sealed class InputManager
{
    private const float SwipeFraction = 0.08f;     // of screen width
    private const float TapSlopFraction = 0.03f;   // movement still counted as a tap
    private const long SwipeMaxMs = 700;

    private readonly Dictionary<int, (Vector2 Start, long Time, bool OnPause)> _touchStarts = new();
    private readonly ITiltSensor _tilt;
    private readonly Settings _settings;
    private KeyboardState _prevKeys;
    private GamePadState _prevPad;
    private MouseState _prevMouse;
    private (Vector2 Start, bool OnPause)? _mouseDown;
    private bool _mouseSteering;
    private float _smoothedTilt;

    public InputManager(ITiltSensor tilt, Settings settings)
    {
        _tilt = tilt;
        _settings = settings;
    }

    public bool HasTilt => _tilt?.IsAvailable == true;

    /// <summary>Horizontal movement, -1 (full left) to 1 (full right).</summary>
    public float Move { get; private set; }

    /// <summary>
    /// Desktop: the virtual X position the ship should steer towards (the mouse pointer), or
    /// null when the keys are steering.
    /// </summary>
    public float? PointerX { get; private set; }

    /// <summary>Smoothed raw tilt in g, for the on-screen meter.</summary>
    public float TiltValue => _smoothedTilt;

    public bool Left { get; private set; }
    public bool Right { get; private set; }
    public bool Fire { get; private set; }
    public bool FirePressed { get; private set; }
    public bool PausePressed { get; private set; }
    public bool BackPressed { get; private set; }
    public bool ConfirmPressed { get; private set; }
    public bool AnyPressed { get; private set; }

    /// <summary>New touches this frame, in virtual (240x224) coordinates.</summary>
    public List<Vector2> Taps { get; } = new();

    /// <summary>Touches lifted this frame without moving far, in virtual coordinates.</summary>
    public List<Vector2> TapReleases { get; } = new();

    /// <summary>-1 for a swipe to the left, 1 for a swipe to the right, 0 for none.</summary>
    public int Swipe { get; private set; }

    /// <summary>Keys that went down this frame.</summary>
    public List<Keys> NewKeys { get; } = new();

    public void Update(ScreenLayout layout)
    {
        Taps.Clear();
        TapReleases.Clear();
        NewKeys.Clear();
        Swipe = 0;
        Left = Right = Fire = FirePressed = PausePressed = BackPressed = ConfirmPressed = AnyPressed = false;

        long now = Environment.TickCount64;
        foreach (var touch in TouchPanel.GetState())
        {
            if (touch.State == TouchLocationState.Released)
            {
                TrackRelease(touch, layout, now);
                continue;
            }
            if (touch.State != TouchLocationState.Pressed && touch.State != TouchLocationState.Moved)
                continue;

            if (touch.State == TouchLocationState.Pressed)
                _touchStarts[touch.Id] = (touch.Position, now, layout.PauseButton.Contains(touch.Position.ToPoint()));

            var p = touch.Position.ToPoint();
            bool isNew = touch.State == TouchLocationState.Pressed;

            if (layout.PauseButton.Contains(p))
                PausePressed |= isNew;
            else if (layout.LeftButton.Contains(p))
                Left = true;
            else if (layout.RightButton.Contains(p))
                Right = true;
            else
            {
                Fire = true;
                FirePressed |= isNew;
            }

            if (isNew)
            {
                AnyPressed = true;
                Taps.Add(layout.ToVirtual(touch.Position));
            }
        }

        if (Platform.IsDesktop)
            UpdateMouse(layout);

        var keys = Keyboard.GetState();
        foreach (var k in keys.GetPressedKeys())
            if (!_prevKeys.IsKeyDown(k))
                NewKeys.Add(k);

        var pad = GamePad.GetState(PlayerIndex.One);

        Left |= keys.IsKeyDown(Keys.Left) || keys.IsKeyDown(Keys.Z) || pad.DPad.Left == ButtonState.Pressed || pad.ThumbSticks.Left.X < -0.4f;
        Right |= keys.IsKeyDown(Keys.Right) || keys.IsKeyDown(Keys.X) || pad.DPad.Right == ButtonState.Pressed || pad.ThumbSticks.Left.X > 0.4f;
        Fire |= keys.IsKeyDown(Keys.Space) || keys.IsKeyDown(Keys.LeftControl) || pad.Buttons.A == ButtonState.Pressed;

        bool keyFire = NewKeys.Contains(Keys.Space) || NewKeys.Contains(Keys.LeftControl) ||
                       (pad.Buttons.A == ButtonState.Pressed && _prevPad.Buttons.A == ButtonState.Released);
        FirePressed |= keyFire;
        // Keyboard / pad only: on touch screens menus act on taps, so a swipe never starts a game.
        ConfirmPressed = keyFire || NewKeys.Contains(Keys.Enter) ||
                         (pad.Buttons.Start == ButtonState.Pressed && _prevPad.Buttons.Start == ButtonState.Released);
        PausePressed |= NewKeys.Contains(Keys.P);
        BackPressed = NewKeys.Contains(Keys.Escape) ||
                      (pad.Buttons.Back == ButtonState.Pressed && _prevPad.Buttons.Back == ButtonState.Released);
        AnyPressed |= NewKeys.Any() || keyFire || ConfirmPressed;

        // Digital controls win over tilt and the mouse so a keyboard or pad always works.
        // The mouse takes over again as soon as it moves.
        if (Left || Right)
            _mouseSteering = false;
        PointerX = _mouseSteering && _settings.MouseSteering
            ? layout.ToVirtual(new Vector2(_prevMouse.X, _prevMouse.Y)).X
            : null;

        if (Left || Right)
            Move = (Left ? -1f : 0f) + (Right ? 1f : 0f);
        else if (HasTilt)
        {
            float raw = _settings.InvertTilt ? -_tilt.Tilt : _tilt.Tilt;
            _smoothedTilt += (raw - _smoothedTilt) * 0.35f;
            float fullTilt = _settings.FullTilt;
            float deadZone = fullTilt * 0.13f;
            float magnitude = Math.Clamp((Math.Abs(_smoothedTilt) - deadZone) / (fullTilt - deadZone), 0f, 1f);
            Move = Math.Sign(_smoothedTilt) * magnitude;
        }
        else
            Move = 0f;

        _prevKeys = keys;
        _prevPad = pad;
    }

    /// <summary>
    /// A left click acts like a touch: it fires (hold for rapid fire), presses on-screen buttons
    /// and pauses from the top-left corner. Moving the mouse hands steering to the pointer.
    /// </summary>
    private void UpdateMouse(ScreenLayout layout)
    {
        var mouse = Mouse.GetState();
        var pos = new Vector2(mouse.X, mouse.Y);
        bool inWindow = mouse.X >= 0 && mouse.Y >= 0 && mouse.X < layout.ScreenWidth && mouse.Y < layout.ScreenHeight;
        bool down = mouse.LeftButton == ButtonState.Pressed;
        bool wasDown = _prevMouse.LeftButton == ButtonState.Pressed;

        if (down && !wasDown && inWindow)
        {
            bool onPause = layout.PauseButton.Contains(mouse.Position);
            _mouseDown = (pos, onPause);
            AnyPressed = true;
            Taps.Add(layout.ToVirtual(pos));
            if (onPause)
                PausePressed = true;
            else
                FirePressed = true;
        }

        if (_mouseDown is { } press)
        {
            if (down)
                Fire |= !press.OnPause;
            else
            {
                _mouseDown = null;
                if (!press.OnPause && (pos - press.Start).Length() <= layout.ScreenWidth * TapSlopFraction)
                    TapReleases.Add(layout.ToVirtual(pos));
            }
        }

        if (inWindow && mouse.Position != _prevMouse.Position)
            _mouseSteering = true;

        _prevMouse = mouse;
    }

    private void TrackRelease(TouchLocation touch, ScreenLayout layout, long now)
    {
        if (!_touchStarts.Remove(touch.Id, out var start) || start.OnPause)
            return;

        var delta = touch.Position - start.Start;
        float width = layout.ScreenWidth;
        if (Math.Abs(delta.X) >= width * SwipeFraction && Math.Abs(delta.X) > Math.Abs(delta.Y) * 1.5f &&
            now - start.Time <= SwipeMaxMs)
            Swipe = delta.X < 0 ? -1 : 1;
        else if (delta.Length() <= width * TapSlopFraction)
            TapReleases.Add(layout.ToVirtual(touch.Position));
    }
}
