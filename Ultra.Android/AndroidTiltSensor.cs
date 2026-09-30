using System;
using Android.Content;
using Android.Hardware;
using Android.Runtime;
using Android.Views;
using Ultra.Core;

namespace Ultra.Android;

/// <summary>
/// Tilt steering from the gyroscope-fused gravity sensor, falling back to a low-pass
/// filtered accelerometer on devices without a gyroscope.
/// </summary>
public sealed class AndroidTiltSensor : Java.Lang.Object, ITiltSensor, ISensorEventListener
{
    private const float Gravity = SensorManager.GravityEarth;

    private readonly SensorManager _manager;
    private readonly Sensor _sensor;
    private readonly bool _filter;
    private readonly IWindowManager _windowManager;
    private float _deviceX, _deviceY;
    private volatile float _tilt;

    public AndroidTiltSensor(Context context)
    {
        _manager = (SensorManager)context.GetSystemService(Context.SensorService);
        _windowManager = context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
        _sensor = _manager?.GetDefaultSensor(SensorType.Gravity);
        if (_sensor == null)
        {
            _sensor = _manager?.GetDefaultSensor(SensorType.Accelerometer);
            _filter = true;
        }
    }

    public bool IsAvailable => _sensor != null;

    public float Tilt => _tilt;

    public void Start()
    {
        if (_sensor != null)
            _manager.RegisterListener(this, _sensor, SensorDelay.Game);
    }

    public void Stop()
    {
        _manager?.UnregisterListener(this);
        _tilt = 0f;
    }

    public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
    {
    }

    public void OnSensorChanged(SensorEvent e)
    {
        if (e?.Values == null || e.Values.Count < 2)
            return;

        float x = e.Values[0], y = e.Values[1];
        if (_filter)
        {
            _deviceX += (x - _deviceX) * 0.2f;
            _deviceY += (y - _deviceY) * 0.2f;
        }
        else
        {
            _deviceX = x;
            _deviceY = y;
        }

        // Sensor axes follow the device's natural orientation; convert to the
        // horizontal axis of the landscape screen. At rest the sensor reads "up",
        // so a lowered right-hand edge gives a negative screen-X reading.
        float screenX;
#pragma warning disable CA1422
        var rotation = _windowManager?.DefaultDisplay?.Rotation ?? SurfaceOrientation.Rotation90;
#pragma warning restore CA1422
        switch (rotation)
        {
            case SurfaceOrientation.Rotation90: screenX = -_deviceY; break;
            case SurfaceOrientation.Rotation270: screenX = _deviceY; break;
            case SurfaceOrientation.Rotation180: screenX = -_deviceX; break;
            default: screenX = _deviceX; break;
        }

        _tilt = Math.Clamp(-screenX / Gravity, -1f, 1f);
    }
}
