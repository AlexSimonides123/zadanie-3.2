using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace notatnik;

public static class NoteStore
{
    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly Regex HeaderPattern = new(
        @"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}) — (.+)$",
        RegexOptions.Compiled);

    public static string FilePath { get; } = Path.Combine(FindProjectFolder(), "notatki.md");

    public static List<Note> Load()
    {
        if (!File.Exists(FilePath))
        {
            return [];
        }

        var text = File.ReadAllText(FilePath, Utf8);
        var lines = text.Replace("\r\n", "\n").Split('\n');
        var notes = new List<Note>();

        for (var i = 0; i < lines.Length;)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
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
                Content = string.Join(Environment.NewLine, contentLines),
                SavedAt = savedAt
            });
        }

        return notes;
    }

    public static void Save(IReadOnlyList<Note> notes)
    {
        var ordered = notes.OrderByDescending(note => note.SavedAt).ToList();
        var builder = new StringBuilder();

        for (var i = 0; i < ordered.Count; i++)
        {
            var note = ordered[i];
            builder.Append(note.SavedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
            builder.Append(" — ");
            builder.AppendLine(note.Title);
            builder.AppendLine();

            var content = (note.Content ?? string.Empty).Replace("\r\n", "\n").TrimEnd();
            if (content.Length > 0)
            {
                foreach (var line in content.Split('\n'))
                {
                    builder.AppendLine(line);
                }
            }

            if (i < ordered.Count - 1)
            {
                builder.AppendLine();
            }
        }

        File.WriteAllText(FilePath, builder.ToString(), Utf8);
    }

    private static string FindProjectFolder()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (directory.GetFiles("*.csproj").Length > 0)
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return AppContext.BaseDirectory;
    }
}
