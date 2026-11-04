using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Collections.Concurrent;

namespace LiveTest.Client.Components;

/// <summary>
///     Demonstrates removing the rich text editor, switching its record, and changing its value from .NET.
/// </summary>
public partial class RichTextEditorLifecycleDemo : ComponentBase {
    // Static so a Server commit sent while the page unloads can be read back after the reload.
    private static readonly ConcurrentQueue<string?> _commits = new();

    private readonly Note _firstNote = new() { Body = "<p>Initial</p>" };
    private readonly Note _secondNote = new() { Body = "<p>Second</p>" };
    private Note _selected;
    private bool _showEditor = true;

    private bool BindOnInput { get; set; }

    public RichTextEditorLifecycleDemo() => _selected = _firstNote;

    private void HandleShortcut(KeyboardEventArgs args) {
        if (args.Key == "F8") {
            OpenSecondNote(new MouseEventArgs());
        }
        else if (args.Key == "F9") {
            SetValue(new MouseEventArgs());
        }
    }

    private void OpenSecondNote(MouseEventArgs args) {
        _selected = _secondNote;
        _showEditor = false;
    }

    private static void RecordCommit(string? value) => _commits.Enqueue(value);

    private void SetValue(MouseEventArgs args) => _selected.Body = "<p>Set from .NET</p>";

    private void ToggleBindOnInput(MouseEventArgs args) => BindOnInput = !BindOnInput;

    private void ToggleEditor(MouseEventArgs args) => _showEditor = !_showEditor;

    private sealed class Note {
        public string? Body { get; set; }
    }
}
