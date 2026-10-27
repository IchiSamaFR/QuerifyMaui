using CommunityToolkit.Mvvm.ComponentModel;
using Quill.Code.Completion;

namespace Querify.Core.Models.Editors.Completion;

/// <summary>
/// Ligne recyclee de la liste d'autocompletion : le popup garde un nombre fixe de lignes
/// et ne fait que changer la suggestion qu'elles affichent (pas de vue recreee a chaque touche).
/// </summary>
public partial class CompletionEntry : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasItem))]
    public partial CompletionItem? Item { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public bool HasItem => Item is not null;
}
