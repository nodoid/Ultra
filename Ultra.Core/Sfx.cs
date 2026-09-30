using System;
using Microsoft.Xna.Framework.Audio;

namespace Ultra.Core;

/// <summary>
/// Sound effects synthesised at start-up in the spirit of the Oric's AY-3-8912:
/// square-wave tones and sample-and-hold noise. No external audio assets are needed.
/// </summary>
public sealed class Sfx
{
    private const int Rate = 22050;
    private static readonly Random Rng = new();

    private readonly SoundEffect _shoot, _alienBoom, _playerBoom, _overheat, _waveStart, _extraLife, _step, _cheer;
    private readonly bool _enabled;

    public Sfx()
    {
        try
        {
            _shoot = Tone(0.07f, p => 1600 - 1100 * p, p => 1 - p);
            _alienBoom = Noise(0.3f, p => 2 + p * 14, p => MathF.Pow(1 - p, 1.5f));
            _playerBoom = Noise(1.3f, p => 3 + p * 40, p => 1 - p);
            _overheat = Tone(0.45f, p => ((int)(p * 9) % 2 == 0) ? 900 : 600, _ => 0.7f);
            _waveStart = Tone(0.6f, p => 200 + 1000 * p + MathF.Sin(p * 60) * 40, p => 0.6f * (1 - p * 0.5f));
            _extraLife = Tone(0.45f, p => new[] { 1000f, 1300f, 1600f }[Math.Min(2, (int)(p * 3))],
                p => (p * 3 % 1f) < 0.8f ? 0.7f : 0f);
            _step = Tone(0.06f, _ => 90, p => 1 - p);
            _cheer = Tone(0.9f, p => new[] { 523f, 659f, 784f, 1047f, 784f, 1047f }[Math.Min(5, (int)(p * 6))],
                p => (p * 6 % 1f) < 0.85f ? 0.6f : 0f);
            _enabled = true;
        }
        catch (Exception)
        {
            // Audio is optional - never let it stop the game starting.
            _enabled = false;
        }
    }

    public void Shoot() => Play(_shoot, 0.25f);
    public void AlienExplode() => Play(_alienBoom, 0.5f);
    public void PlayerExplode() => Play(_playerBoom, 0.7f);
    public void Overheat() => Play(_overheat, 0.5f);
    public void WaveStart() => Play(_waveStart, 0.4f);
    public void ExtraLife() => Play(_extraLife, 0.5f);
    public void Step() => Play(_step, 0.35f);
    public void HighScore() => Play(_cheer, 0.5f);

    private void Play(SoundEffect effect, float volume)
    {
        if (!_enabled || effect == null)
            return;
        try
        {
            effect.Play(volume, 0f, 0f);
        }
        catch (Exception)
        {
            // Ignore - running out of voices must not crash the game.
        }
    }

    private static SoundEffect Tone(float seconds, Func<float, float> frequency, Func<float, float> amplitude)
    {
        float phase = 0;
        return Make(seconds, p =>
        {
            phase += frequency(p) / Rate;
            phase -= MathF.Floor(phase);
            return (phase < 0.5f ? 1f : -1f) * amplitude(p);
        });
    }

    private static SoundEffect Noise(float seconds, Func<float, float> holdSamples, Func<float, float> amplitude)
    {
        float value = 0, counter = 0;
        return Make(seconds, p =>
        {
            if (--counter <= 0)
            {
                value = Rng.Next(2) == 0 ? -1f : 1f;
                counter = holdSamples(p);
            }
            return value * amplitude(p);
        });
    }

    private static SoundEffect Make(float seconds, Func<float, float> generator)
    {
        int count = (int)(Rate * seconds);
        var buffer = new byte[count * 2];
        for (int i = 0; i < count; i++)
        {
            float v = Math.Clamp(generator(i / (float)count), -1f, 1f);
            short s = (short)(v * short.MaxValue * 0.6f);
            buffer[i * 2] = (byte)s;
            buffer[i * 2 + 1] = (byte)(s >> 8);
        }
        return new SoundEffect(buffer, Rate, AudioChannels.Mono);
    }
}
