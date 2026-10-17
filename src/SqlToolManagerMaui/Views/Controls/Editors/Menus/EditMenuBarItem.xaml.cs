using CommunityToolkit.Maui;
using System.Windows.Input;

namespace SqlToolManagerMaui.Views.Controls.Editors.Menus;

/// <summary>Menu "Edition" d'un editeur. Les commandes sont fournies par l'hote.</summary>
public partial class EditMenuBarItem : MenuBarItem
{
    /// <summary>Annule la derniere modification.</summary>
    [BindableProperty]
    public partial ICommand? UndoCommand { get; set; }

    /// <summary>Retablit la derniere modification annulee.</summary>
    [BindableProperty]
    public partial ICommand? RedoCommand { get; set; }

    /// <summary>Coupe la selection.</summary>
    [BindableProperty]
    public partial ICommand? CutCommand { get; set; }

    /// <summary>Copie la selection.</summary>
    [BindableProperty]
    public partial ICommand? CopyCommand { get; set; }

    /// <summary>Colle le presse-papiers au curseur.</summary>
    [BindableProperty]
    public partial ICommand? PasteCommand { get; set; }

    /// <summary>Selectionne tout le document.</summary>
    [BindableProperty]
    public partial ICommand? SelectAllCommand { get; set; }

    public EditMenuBarItem()
    {
        InitializeComponent();
    }
}
