using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ultra.Core;

public sealed class HighScoreEntry
{
    public string Name { get; set; } = "";
    public int Score { get; set; }
    public int Wave { get; set; }
}

/// <summary>
/// The "Hall of Fame". The table is written to the app's private storage every time a
/// new score is entered and read back when the game launches, so scores carry over
/// between runs.
/// </summary>
public sealed class HighScoreTable
{
    public const int Capacity = 10;
    public const int MaxNameLength = 10;
    private const string FileName = "ultra_hiscores.txt";

    private static readonly (string Name, int Score, int Wave)[] Defaults =
    {
        ("ULTRA", 5000, 8), ("LIBERATOR", 4500, 7), ("ORAC", 4000, 7), ("ZEN", 3500, 6),
        ("ORIC", 3000, 5), ("ATMOS", 2500, 5), ("PSS", 2000, 4), ("TANGERINE", 1500, 3),
        ("MICRODISC", 1000, 2), ("CUMANA", 500, 1),
    };

    private readonly string _path;
    public List<HighScoreEntry> Entries { get; } = new();

    public HighScoreTable()
    {
        string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrEmpty(folder))
            folder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        _path = Path.Combine(folder, FileName);
        Load();
    }

    public int Best => Entries.Count > 0 ? Entries[0].Score : 0;

    public bool Qualifies(int score) =>
        score > 0 && (Entries.Count < Capacity || score > Entries[^1].Score);

    /// <summary>Inserts a score, saves the table and returns the zero-based rank.</summary>
    public int Insert(string name, int score, int wave)
    {
        name = Sanitise(name);
        if (name.Length == 0)
            name = "ANON";

        int rank = Entries.FindIndex(e => score > e.Score);
        if (rank < 0)
            rank = Entries.Count;

        Entries.Insert(rank, new HighScoreEntry { Name = name, Score = score, Wave = wave });
        while (Entries.Count > Capacity)
            Entries.RemoveAt(Entries.Count - 1);

        Save();
        return rank;
    }

    /// <summary>The rank a score would take, or -1 if it would not make the table.</summary>
    public int RankFor(int score)
    {
        if (!Qualifies(score))
            return -1;
        int rank = Entries.FindIndex(e => score > e.Score);
        return rank < 0 ? Entries.Count : rank;
    }

    private void Load()
    {
        Entries.Clear();
        try
        {
            if (File.Exists(_path))
            {
                foreach (var line in File.ReadAllLines(_path, Encoding.UTF8))
                {
                    var parts = line.Split('|', 3);
                    if (parts.Length == 3 && int.TryParse(parts[0], out int score) && int.TryParse(parts[1], out int wave))
                        Entries.Add(new HighScoreEntry { Score = score, Wave = wave, Name = Sanitise(parts[2]) });
                }
            }
        }
        catch (Exception)
        {
            Entries.Clear();
        }

        if (Entries.Count == 0)
            Entries.AddRange(Defaults.Select(d => new HighScoreEntry { Name = d.Name, Score = d.Score, Wave = d.Wave }));

        Entries.Sort((a, b) => b.Score.CompareTo(a.Score));
        if (Entries.Count > Capacity)
            Entries.RemoveRange(Capacity, Entries.Count - Capacity);
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            string temp = _path + ".tmp";
            File.WriteAllLines(temp, Entries.Select(e => $"{e.Score}|{e.Wave}|{e.Name}"), Encoding.UTF8);
            File.Move(temp, _path, true);
        }
        catch (Exception)
        {
            // Storage unavailable - the table still works for this session.
        }
    }

    private static string Sanitise(string name)
    {
        var sb = new StringBuilder();
        foreach (char c in (name ?? "").ToUpperInvariant())
        {
            if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == ' ' || c == '.' || c == '-' || c == '!')
                sb.Append(c);
            if (sb.Length == MaxNameLength)
                break;
        }
        return sb.ToString().Trim();
    }
}
