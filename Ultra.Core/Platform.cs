using System;

namespace Ultra.Core;

/// <summary>Which kind of device the game is running on.</summary>
public static class Platform
{
    /// <summary>
    /// True on Windows and macOS (and Linux): played in a window with a mouse and keyboard
    /// rather than by tilting and touching the screen.
    /// </summary>
    public static bool IsDesktop => OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsLinux();
}
