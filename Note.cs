using System;

namespace notatnik;

public sealed class Note
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SavedAt { get; set; } = DateTime.Now;

    public override string ToString() =>
        string.IsNullOrWhiteSpace(Title) ? "(bez tytułu)" : Title;
}
