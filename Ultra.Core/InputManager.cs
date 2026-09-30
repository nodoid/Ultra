using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace Ultra.Core;

/// <summary>
/// Merges multi-touch, hardware keyboard and game pad input into one per-frame snapshot.
/// </summary>
public sealed class InputManager
{
    private KeyboardState _prevKeys;
    private GamePadState _prevPad;

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

            if (layout.FireButton.Contains(p))
            {
                Fire = true;
                FirePressed |= isNew;
            }
            else if (layout.LeftButton.Contains(p))
                Left = true;
            else if (layout.RightButton.Contains(p))
                Right = true;
            else if (layout.PauseButton.Contains(p))
                PausePressed |= isNew;

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

        _prevKeys = keys;
        _prevPad = pad;
    }
}
