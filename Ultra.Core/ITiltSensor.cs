namespace Ultra.Core;

/// <summary>
/// Platform motion sensor used for steering. Implementations use the gyroscope-fused
/// gravity vector where the device has one.
/// </summary>
public interface ITiltSensor
{
    /// <summary>True when the device has a usable motion sensor.</summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Sideways tilt of the landscape screen in units of g (-1..1).
    /// Positive when the right-hand edge of the screen is lower than the left.
    /// </summary>
    float Tilt { get; }
}
