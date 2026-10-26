using CommunityToolkit.Maui;
using System.Windows.Input;

namespace Querify.UI.Views.Controls.DataGrids;

/// <summary>Saisie du filtre d'une colonne, ouverte par le bouton de son en-tete.</summary>
public partial class DataGridFilterPopup : ContentView
{
    [BindableProperty]
    public partial DataGridColumn? Column { get; set; }

    /// <summary>Ferme la saisie (touche Entree).</summary>
    [BindableProperty]
    public partial ICommand? CloseCommand { get; set; }

    /// <summary>Vide le filtre de <see cref="Column"/>.</summary>
    [BindableProperty(DefaultValueCreatorMethodName = nameof(CreateClearCommand))]
    public partial ICommand ClearCommand { get; private set; }

    public DataGridFilterPopup()
    {
        InitializeComponent();
    }

    private static object CreateClearCommand(BindableObject bindable)
        => new Command(((DataGridFilterPopup)bindable).ClearFilter);

    /// <summary>Donne le focus a la saisie, texte selectionne pour etre remplace d'un coup.</summary>
    public void FocusInput()
    {
    }

    private void ClearFilter()
    {
        if (Column is not null)
            Column.FilterText = string.Empty;

        FilterInput.EntryView.Focus();
    }
}
