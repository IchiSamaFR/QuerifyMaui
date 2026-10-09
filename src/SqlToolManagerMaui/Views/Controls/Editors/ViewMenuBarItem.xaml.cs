using CommunityToolkit.Maui;
using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Models.Editors.Highlighting;
using System.Windows.Input;

namespace SqlToolManagerMaui.Views.Controls.Editors;

/// <summary>
/// Menu "Affichage" d'un editeur : trois bascules dont le libelle suit l'etat courant.
/// MenuFlyoutItem n'ayant pas d'etat "coche", c'est le texte qui porte l'action a venir ;
/// le basculement est declare en XAML (DataTrigger sur les proprietes d'etat).
/// </summary>
public partial class ViewMenuBarItem : MenuBarItem
{
    /// <summary>Bascule l'affichage des numeros de ligne.</summary>
    [BindableProperty]
    public partial ICommand? ToggleLineNumbersCommand { get; set; }

    /// <summary>Etat courant des numeros de ligne.</summary>
    [BindableProperty]
    public partial bool AreLineNumbersVisible { get; set; }

    /// <summary>Bascule le retour a la ligne automatique.</summary>
    [BindableProperty]
    public partial ICommand? ToggleWordWrapCommand { get; set; }

    /// <summary>Etat courant du retour a la ligne.</summary>
    [BindableProperty]
    public partial bool IsWordWrapEnabled { get; set; }

    /// <summary>Bascule l'indentation par tabulations.</summary>
    [BindableProperty]
    public partial ICommand? ToggleTabIndentationCommand { get; set; }

    /// <summary>Etat courant de l'indentation par tabulations.</summary>
    [BindableProperty]
    public partial bool IsTabIndentationEnabled { get; set; }

    /// <summary>Langage de l'onglet actif, coche dans le sous-menu.</summary>
    [BindableProperty]
    public partial SyntaxLanguage Language { get; set; } = SyntaxLanguage.Sql;

    /// <summary>Change le langage de l'onglet actif ; parametre : la valeur de <see cref="SyntaxLanguage"/>.</summary>
    [BindableProperty]
    public partial ICommand? SetLanguageCommand { get; set; }

    public ViewMenuBarItem()
    {
        InitializeComponent();
    }
}
