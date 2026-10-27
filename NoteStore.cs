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
    private const char EmDash = '\u2014';
    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly Regex HeaderPattern = new(
        @"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}) \u2014 (.+)$",
        RegexOptions.Compiled);

    public static string FilePath { get; } = ResolveNotesPath();

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
        var block = FormatEntry(DateTime.Now, title, content);
        var previous = ReadExisting();

        var text = previous.Length == 0
            ? block
            : block + "\n" + previous;

        File.WriteAllText(FilePath, text, Utf8);
        return Load();
    }

    private static string ReadExisting()
    {
        if (!File.Exists(FilePath))
        {
            return string.Empty;
        }

        var existing = StripBom(File.ReadAllText(FilePath, Utf8))
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .TrimStart('\n');

        return existing;
    }

    private static string FormatEntry(DateTime savedAt, string title, string content)
    {
        var header =
            savedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
            + " "
            + EmDash
            + " "
            + NormalizeTitle(title);

        var bodyLines = BodyLines(content);
        var builder = new StringBuilder();
        builder.Append(header);
        builder.Append('\n');
        builder.Append('\n');

        for (var i = 0; i < bodyLines.Count; i++)
        {
            builder.Append(bodyLines[i]);
            builder.Append('\n');
            if (i < bodyLines.Count - 1)
            {
                builder.Append('\n');
            }
        }

        return builder.ToString();
    }

    private static List<string> BodyLines(string? content) =>
        (content ?? string.Empty)
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n')
            .Select(line => line.TrimEnd())
            .Where(line => line.Length > 0)
            .ToList();

    private static string NormalizeTitle(string title) =>
        title.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ').Trim();

    private static List<Note> Parse(string text)
    {
        var lines = StripBom(text).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
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

            notes.Add(new Note
            {
                Title = title,
                Content = string.Join("\n", BodyLines(string.Join("\n", contentLines))),
                SavedAt = savedAt
            });
        }

        return notes;
    }

    private static string StripBom(string text) =>
        text.Length > 0 && text[0] == '\uFEFF' ? text[1..] : text;

    private static string ResolveNotesPath()
    {
        string? projectFolder = null;
        string? notesBesideProject = null;

        foreach (var folder in CandidateFolders())
        {
            var notesPath = Path.Combine(folder, "notatki.md");
            var hasProject = Directory.GetFiles(folder, "*.csproj").Length > 0;

            if (hasProject)
            {
                projectFolder ??= folder;
                if (File.Exists(notesPath))
                {
                    notesBesideProject ??= notesPath;
                }
            }
        }

        if (notesBesideProject is not null)
        {
            return notesBesideProject;
        }

        if (projectFolder is not null)
        {
            return Path.Combine(projectFolder, "notatki.md");
        }

        return Path.Combine(Directory.GetCurrentDirectory(), "notatki.md");
    }

    private static IEnumerable<string> CandidateFolders()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            DirectoryInfo? folder;
            try
            {
                folder = new DirectoryInfo(Path.GetFullPath(start));
            }
            catch (Exception)
            {
                continue;
            }

            while (folder is not null)
            {
                if (seen.Add(folder.FullName))
                {
                    yield return folder.FullName;
                }

                folder = folder.Parent;
            }
        }
    }
}
