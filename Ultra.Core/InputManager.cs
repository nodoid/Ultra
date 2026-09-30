using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace Ultra.Core;

/// <summary>
/// Merges tilt steering, multi-touch, hardware keyboard and game pad input into one
/// per-frame snapshot. Tilting the device steers; touching anywhere fires.
/// </summary>
public sealed class InputManager
{
    private readonly ITiltSensor _tilt;
    private readonly Settings _settings;
    private KeyboardState _prevKeys;
    private GamePadState _prevPad;
    private float _smoothedTilt;

    public InputManager(ITiltSensor tilt, Settings settings)
    {
        _tilt = tilt;
        _settings = settings;
    }

    public bool HasTilt => _tilt?.IsAvailable == true;

    /// <summary>Horizontal movement, -1 (full left) to 1 (full right).</summary>
    public float Move { get; private set; }

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

    /// <summary>Keys that went down this frame.</summary>
    public List<Keys> NewKeys { get; } = new();

    public void Update(ScreenLayout layout)
    {
        Taps.Clear();
        NewKeys.Clear();
        Left = Right = Fire = FirePressed = PausePressed = BackPressed = ConfirmPressed = AnyPressed = false;

        foreach (var touch in TouchPanel.GetState())
        {
            if (touch.State != TouchLocationState.Pressed && touch.State != TouchLocationState.Moved)
                continue;

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

        var keys = Keyboard.GetState();
        foreach (var k in keys.GetPressedKeys())
            if (!_prevKeys.IsKeyDown(k))
                NewKeys.Add(k);

        var pad = GamePad.GetState(PlayerIndex.One);

        Left |= keys.IsKeyDown(Keys.Left) || pad.DPad.Left == ButtonState.Pressed || pad.ThumbSticks.Left.X < -0.4f;
        Right |= keys.IsKeyDown(Keys.Right) || pad.DPad.Right == ButtonState.Pressed || pad.ThumbSticks.Left.X > 0.4f;
        Fire |= keys.IsKeyDown(Keys.Space) || keys.IsKeyDown(Keys.LeftControl) || pad.Buttons.A == ButtonState.Pressed;

        FirePressed |= NewKeys.Contains(Keys.Space) || NewKeys.Contains(Keys.LeftControl) ||
                       (pad.Buttons.A == ButtonState.Pressed && _prevPad.Buttons.A == ButtonState.Released);
        ConfirmPressed = FirePressed || NewKeys.Contains(Keys.Enter) ||
                         (pad.Buttons.Start == ButtonState.Pressed && _prevPad.Buttons.Start == ButtonState.Released);
        PausePressed |= NewKeys.Contains(Keys.P);
        BackPressed = NewKeys.Contains(Keys.Escape) ||
                      (pad.Buttons.Back == ButtonState.Pressed && _prevPad.Buttons.Back == ButtonState.Released);
        AnyPressed |= NewKeys.Any() || FirePressed || ConfirmPressed;

        // Digital controls win over tilt so a keyboard or pad always works.
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
}
