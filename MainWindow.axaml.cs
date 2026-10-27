using System;
using System.Collections.Generic;
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
        ContentBox.Text = note.Content.Replace("\n", Environment.NewLine);
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

        ReplaceNotes(NoteStore.AddNew(title, content));
    }

    private void ReplaceNotes(List<Note> notes)
    {
        _loadingSelection = true;
        _notes.Clear();
        foreach (var note in notes)
        {
            _notes.Add(note);
        }

        NotesList.SelectedItem = _notes.Count > 0 ? _notes[0] : null;
        _loadingSelection = false;

        if (NotesList.SelectedItem is Note current)
        {
            TitleBox.Text = current.Title;
            ContentBox.Text = current.Content.Replace("\n", Environment.NewLine);
        }
    }
}
