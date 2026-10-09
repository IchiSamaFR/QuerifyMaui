using CommunityToolkit.Mvvm.ComponentModel;

namespace SqlToolManagerMaui.Core.Models.Editors.Completion;

/// <summary>Ligne de la liste d'autocompletion : la suggestion et son etat de surbrillance.</summary>
public partial class CompletionEntry : ObservableObject
{
    public CompletionEntry(CompletionItem item)
    {
        Item = item;
    }

    public CompletionItem Item { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
