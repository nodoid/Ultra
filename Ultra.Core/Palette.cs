using Microsoft.Xna.Framework;

namespace Ultra.Core;

/// <summary>
/// The eight fixed colours of the Oric-1 / Atmos ULA.
/// </summary>
public static class Palette
{
    public static readonly Color Black = new(0, 0, 0);
    public static readonly Color Red = new(255, 0, 0);
    public static readonly Color Green = new(0, 255, 0);
    public static readonly Color Yellow = new(255, 255, 0);
    public static readonly Color Blue = new(0, 0, 255);
    public static readonly Color Magenta = new(255, 0, 255);
    public static readonly Color Cyan = new(0, 255, 255);
    public static readonly Color White = new(255, 255, 255);

    public static readonly Color[] Rainbow = { Red, Yellow, Green, Cyan, Blue, Magenta, White };
}
