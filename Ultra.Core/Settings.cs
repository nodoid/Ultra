using System;
using System.IO;
using System.Linq;

namespace Ultra.Core;

/// <summary>
/// Player options, saved to the app's private storage whenever they change and
/// restored at launch.
/// </summary>
public sealed class Settings
{
    public const int MinSensitivity = 1;
    public const int MaxSensitivity = 10;
    public const int DefaultSensitivity = 5;
    private const string FileName = "ultra_settings.txt";

    private readonly string _path = Storage.PathFor(FileName);

    public int TiltSensitivity { get; private set; } = DefaultSensitivity;
    public bool InvertTilt { get; private set; }

    public Settings()
    {
        Load();
    }

    /// <summary>
    /// Tilt, in g, that gives full speed. Each sensitivity step needs 20% less tilt;
    /// the default (5) needs about 17 degrees.
    /// </summary>
    public float FullTilt => 0.30f * MathF.Pow(0.8f, TiltSensitivity - DefaultSensitivity);

    public void ChangeSensitivity(int delta)
    {
        int value = Math.Clamp(TiltSensitivity + delta, MinSensitivity, MaxSensitivity);
        if (value == TiltSensitivity)
            return;
        TiltSensitivity = value;
        Save();
    }

    public void ToggleInvert()
    {
        InvertTilt = !InvertTilt;
        Save();
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_path))
                return;
            foreach (var line in File.ReadAllLines(_path))
            {
                var parts = line.Split('=', 2);
                if (parts.Length != 2)
                    continue;
                switch (parts[0].Trim())
                {
                    case "TiltSensitivity" when int.TryParse(parts[1], out int s):
                        TiltSensitivity = Math.Clamp(s, MinSensitivity, MaxSensitivity);
                        break;
                    case "InvertTilt" when bool.TryParse(parts[1], out bool b):
                        InvertTilt = b;
                        break;
                }
            }
        }
        catch (Exception)
        {
            // Unreadable settings - fall back to the defaults.
        }
    }

    private void Save()
    {
        Storage.WriteAllLines(_path, new[]
        {
            $"TiltSensitivity={TiltSensitivity}",
            $"InvertTilt={InvertTilt}",
        });
    }
}

/// <summary>Location of, and safe writes to, the app's private data files.</summary>
public static class Storage
{
    public static string PathFor(string fileName)
    {
        string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrEmpty(folder))
            folder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        return Path.Combine(folder, fileName);
    }

    /// <summary>Writes via a temporary file so a crash mid-write never corrupts the original.</summary>
    public static void WriteAllLines(string path, System.Collections.Generic.IEnumerable<string> lines)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string temp = path + ".tmp";
            File.WriteAllLines(temp, lines.ToArray());
            File.Move(temp, path, true);
        }
        catch (Exception)
        {
            // Storage unavailable - settings still apply for this session.
        }
    }
}
