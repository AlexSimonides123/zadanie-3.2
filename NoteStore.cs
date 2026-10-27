using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace notatnik;

public static class NoteStore
{
    private const string HeaderSeparator = " — ";
    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly Regex HeaderPattern = new(
        @"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2})\s+[—–-]\s+(.+)$",
        RegexOptions.Compiled);

    public static string FilePath { get; } = Path.Combine(FindProjectFolder(), "notatki.md");

    public static List<Note> Load()
    {
        if (!File.Exists(FilePath))
        {
            return [];
        }

        return Parse(File.ReadAllText(FilePath, Utf8));
    }

    public static List<Note> AddNew(string title, string content)
    {
        var note = new Note
        {
            Title = title,
            Content = content,
            SavedAt = DateTime.Now
        };

        var notes = Load();
        var raw = File.Exists(FilePath) ? File.ReadAllText(FilePath, Utf8) : string.Empty;

        if (notes.Count == 0 && !string.IsNullOrWhiteSpace(raw))
        {
            File.WriteAllText(FilePath, FormatNote(note) + "\n" + StripBom(raw), Utf8);
            return Load();
        }

        notes.Insert(0, note);
        WriteAll(notes);
        return notes;
    }

    public static List<Note> Update(Note original, string title, string content)
    {
        var notes = Load();
        var index = notes.FindIndex(note => SameEntry(note, original));
        if (index >= 0)
        {
            notes.RemoveAt(index);
        }

        notes.Insert(0, new Note
        {
            Title = title,
            Content = content,
            SavedAt = DateTime.Now
        });

        WriteAll(notes);
        return notes;
    }

    private static bool SameEntry(Note left, Note right) =>
        left.Title == right.Title &&
        FormatTimestamp(left.SavedAt) == FormatTimestamp(right.SavedAt);

    private static void WriteAll(List<Note> notes)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < notes.Count; i++)
        {
            builder.Append(FormatNote(notes[i]));
            if (i < notes.Count - 1)
            {
                builder.Append('\n');
            }
        }

        File.WriteAllText(FilePath, builder.ToString(), Utf8);
    }

    private static string FormatNote(Note note)
    {
        var builder = new StringBuilder();
        builder.Append(FormatTimestamp(note.SavedAt));
        builder.Append(HeaderSeparator);
        builder.Append(note.Title);
        builder.Append('\n');
        builder.Append('\n');

        var content = (note.Content ?? string.Empty).Replace("\r\n", "\n").TrimEnd('\n', '\r');
        if (content.Length > 0)
        {
            builder.Append(content);
            builder.Append('\n');
        }

        return builder.ToString();
    }

    private static string FormatTimestamp(DateTime value) =>
        value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    private static List<Note> Parse(string text)
    {
        var lines = StripBom(text).Replace("\r\n", "\n").Split('\n');
        var notes = new List<Note>();

        for (var i = 0; i < lines.Length;)
        {
            if (lines[i].Length == 0)
            {
                i++;
                continue;
            }

            var match = HeaderPattern.Match(lines[i]);
            if (!match.Success)
            {
                i++;
                continue;
            }

            var savedAt = DateTime.ParseExact(
                match.Groups[1].Value,
                "yyyy-MM-dd HH:mm",
                CultureInfo.InvariantCulture);
            var title = match.Groups[2].Value;
            i++;

            if (i < lines.Length && lines[i].Length == 0)
            {
                i++;
            }

            var contentLines = new List<string>();
            while (i < lines.Length && !HeaderPattern.IsMatch(lines[i]))
            {
                contentLines.Add(lines[i]);
                i++;
            }

            while (contentLines.Count > 0 && contentLines[^1].Length == 0)
            {
                contentLines.RemoveAt(contentLines.Count - 1);
            }

            notes.Add(new Note
            {
                Title = title,
                Content = string.Join("\n", contentLines),
                SavedAt = savedAt
            });
        }

        return notes;
    }

    private static string StripBom(string text) =>
        text.Length > 0 && text[0] == '\uFEFF' ? text[1..] : text;

    private static string FindProjectFolder()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var directory = new DirectoryInfo(Path.GetFullPath(start));
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "notatnik.csproj")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }
        }

        return Directory.GetCurrentDirectory();
    }
}
