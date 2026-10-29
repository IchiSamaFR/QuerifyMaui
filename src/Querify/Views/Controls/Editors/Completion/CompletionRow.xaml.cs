using CommunityToolkit.Maui;
using Quill.Code.Completion;

namespace Querify.Views.Controls.Editors.Completion;

/// <summary>Ligne recyclee de <see cref="CompletionPopup"/> : seule la proposition affichee change au filtrage ou au defilement.</summary>
public partial class CompletionRow : ContentView
{
    /// <summary>Proposition affichee ; null masque la ligne.</summary>
    [BindableProperty]
    public partial CompletionItem? Item { get; set; }

    [BindableProperty]
    public partial bool IsSelected { get; set; }

    public CompletionRow()
    {
        InitializeComponent();
    }
}
