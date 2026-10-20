using CommunityToolkit.Mvvm.ComponentModel;
using SqlToolManagerMaui.Core.Models.Queries;

namespace SqlToolManagerMaui.Core.Models.Editors;

/// <summary>Onglet de script : le document et le resultat de sa derniere execution.</summary>
public sealed partial class CodeEditorSession : AbstractSession
{
    public CodeEditorSession(EditorDocument document)
        : base(document)
    {
    }

    /// <summary>Resultat de la derniere execution lancee depuis cet onglet.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResultsPanel))]
    public partial QueryExecutionResult? LastResult { get; set; }

    /// <summary>Vrai tant qu'une execution lancee depuis cet onglet est en cours.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResultsPanel))]
    public partial bool IsExecuting { get; set; }

    /// <summary>Le panneau de resultats n'apparait qu'apres une premiere execution.</summary>
    public bool HasResultsPanel => IsExecuting || LastResult is not null;
}
