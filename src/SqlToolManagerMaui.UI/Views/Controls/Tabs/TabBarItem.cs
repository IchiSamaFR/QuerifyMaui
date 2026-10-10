using CommunityToolkit.Maui;

namespace SqlToolManagerMaui.UI.Views.Controls.Tabs;

/// <summary>Un onglet de <see cref="TabBar"/> : description de l'en-tete et contenu affiche quand il est selectionne.</summary>
[ContentProperty(nameof(Content))]
public partial class TabBarItem : BindableObject
{
    [BindableProperty]
    public partial string? Header { get; set; }

    [BindableProperty]
    public partial string? ToolTip { get; set; }

    /// <summary>Affiche la pastille "modifie" a la place du bouton fermer (hors survol).</summary>
    [BindableProperty]
    public partial bool IsDirty { get; set; }

    [BindableProperty]
    public partial bool CanClose { get; set; } = true;

    [BindableProperty]
    public partial View? Content { get; set; }

    [BindableProperty]
    public partial bool IsSelected { get; private set; }

    /// <summary>Element de l'ItemsSource represente, sinon l'onglet lui-meme.</summary>
    public object Value => DataItem ?? this;

    internal object? DataItem { get; set; }

    internal void SetSelected(bool selected) => IsSelected = selected;
}
