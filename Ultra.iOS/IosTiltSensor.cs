using System;
using System.Linq;
using CoreMotion;
using UIKit;
using Ultra.Core;

namespace Ultra.iOS;

/// <summary>
/// Tilt steering from CoreMotion device motion, which fuses the gyroscope and
/// accelerometer into a smooth gravity vector.
/// </summary>
public sealed class IosTiltSensor : ITiltSensor
{
    private readonly CMMotionManager _motion = new();

    public IosTiltSensor()
    {
        if (_motion.DeviceMotionAvailable)
        {
            _motion.DeviceMotionUpdateInterval = 1.0 / 60.0;
            _motion.StartDeviceMotionUpdates();
        }
    }

    public bool IsAvailable => _motion.DeviceMotionAvailable;

    public float Tilt
    {
        get
        {
            var gravity = _motion.DeviceMotion?.Gravity;
            if (gravity == null)
                return 0f;

            // Gravity is in portrait device axes (y towards the top of the device) in g.
            // In LandscapeRight the top of the device points left, so screen-right is -y.
            float screenRight = CurrentOrientation() == UIInterfaceOrientation.LandscapeLeft
                ? (float)gravity.Value.Y
                : (float)-gravity.Value.Y;
            return Math.Clamp(screenRight, -1f, 1f);
        }
    }

    private static UIInterfaceOrientation CurrentOrientation()
    {
        var scene = UIApplication.SharedApplication.ConnectedScenes.ToArray().OfType<UIWindowScene>().FirstOrDefault();
        if (scene == null)
            return UIInterfaceOrientation.LandscapeRight;
        if (OperatingSystem.IsIOSVersionAtLeast(26))
            return scene.EffectiveGeometry.InterfaceOrientation;
#pragma warning disable CA1422
        return scene.InterfaceOrientation;
#pragma warning restore CA1422
    }
}
