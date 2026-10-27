using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace notatnik;

public static class NoteStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "notatnik",
        "notatki.json");

    public static List<Note> Load()
    {
        if (!File.Exists(FilePath))
        {
            return [];
        }

        var json = File.ReadAllText(FilePath);
        return JsonSerializer.Deserialize<List<Note>>(json, JsonOptions) ?? [];
    }

    public static void Save(IReadOnlyList<Note> notes)
    {
        var directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(FilePath, JsonSerializer.Serialize(notes, JsonOptions));
    }
}
