using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace notatnik;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<Note> _notes = [];
    private bool _loadingSelection;

    public MainWindow()
    {
        InitializeComponent();
        NotesList.ItemsSource = _notes;
        LoadNotesFromFile();
    }

    private void LoadNotesFromFile()
    {
        _notes.Clear();
        foreach (var note in NoteStore.Load())
        {
            _notes.Add(note);
        }
    }

    private void NotesList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_loadingSelection || NotesList.SelectedItem is not Note note)
        {
            return;
        }

        TitleBox.Text = note.Title;
        ContentBox.Text = note.Content;
    }

    private void NewButton_OnClick(object? sender, RoutedEventArgs e)
    {
        NotesList.SelectedItem = null;
        TitleBox.Text = string.Empty;
        ContentBox.Text = string.Empty;
        TitleBox.Focus();
    }

    private void SaveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var title = TitleBox.Text?.Trim() ?? string.Empty;
        var content = ContentBox.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            title = "Bez tytułu";
        }

        if (NotesList.SelectedItem is Note selected)
        {
            selected.Title = title;
            selected.Content = content;
            selected.SavedAt = DateTime.Now;
            NotesList.ItemsSource = null;
            NotesList.ItemsSource = _notes;
            NotesList.SelectedItem = selected;
        }
        else
        {
            var note = new Note
            {
                Title = title,
                Content = content,
                SavedAt = DateTime.Now
            };
            _notes.Add(note);
            _loadingSelection = true;
            NotesList.SelectedItem = note;
            _loadingSelection = false;
        }

        NoteStore.Save(_notes);
    }
}
