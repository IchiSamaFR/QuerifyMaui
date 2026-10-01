using System.Windows.Input;
using SqlToolManagerMaui.Models.Editors.Highlighting;

namespace SqlToolManagerMaui.Views.Controls.Editors;

/// <summary>
/// Menu "Affichage" d'un editeur : trois bascules dont le libelle suit l'etat courant.
/// MenuFlyoutItem n'ayant pas d'etat "coche", c'est le texte qui porte l'action a venir ;
/// le basculement est declare en XAML (DataTrigger sur les proprietes d'etat).
/// </summary>
public partial class ViewMenuBarItem : MenuBarItem
{
    public static readonly BindableProperty ToggleLineNumbersCommandProperty = BindableProperty.Create(
        nameof(ToggleLineNumbersCommand), typeof(ICommand), typeof(ViewMenuBarItem), null);

    /// <summary>Bascule l'affichage des numeros de ligne.</summary>
    public ICommand? ToggleLineNumbersCommand
    {
        get => (ICommand?)GetValue(ToggleLineNumbersCommandProperty);
        set => SetValue(ToggleLineNumbersCommandProperty, value);
    }

    public static readonly BindableProperty AreLineNumbersVisibleProperty = BindableProperty.Create(
        nameof(AreLineNumbersVisible), typeof(bool), typeof(ViewMenuBarItem), false);

    /// <summary>Etat courant des numeros de ligne.</summary>
    public bool AreLineNumbersVisible
    {
        get => (bool)GetValue(AreLineNumbersVisibleProperty);
        set => SetValue(AreLineNumbersVisibleProperty, value);
    }

    public static readonly BindableProperty ToggleWordWrapCommandProperty = BindableProperty.Create(
        nameof(ToggleWordWrapCommand), typeof(ICommand), typeof(ViewMenuBarItem), null);

    /// <summary>Bascule le retour a la ligne automatique.</summary>
    public ICommand? ToggleWordWrapCommand
    {
        get => (ICommand?)GetValue(ToggleWordWrapCommandProperty);
        set => SetValue(ToggleWordWrapCommandProperty, value);
    }

    public static readonly BindableProperty IsWordWrapEnabledProperty = BindableProperty.Create(
        nameof(IsWordWrapEnabled), typeof(bool), typeof(ViewMenuBarItem), false);

    /// <summary>Etat courant du retour a la ligne.</summary>
    public bool IsWordWrapEnabled
    {
        get => (bool)GetValue(IsWordWrapEnabledProperty);
        set => SetValue(IsWordWrapEnabledProperty, value);
    }

    public static readonly BindableProperty ToggleTabIndentationCommandProperty = BindableProperty.Create(
        nameof(ToggleTabIndentationCommand), typeof(ICommand), typeof(ViewMenuBarItem), null);

    /// <summary>Bascule l'indentation par tabulations.</summary>
    public ICommand? ToggleTabIndentationCommand
    {
        get => (ICommand?)GetValue(ToggleTabIndentationCommandProperty);
        set => SetValue(ToggleTabIndentationCommandProperty, value);
    }

    public static readonly BindableProperty IsTabIndentationEnabledProperty = BindableProperty.Create(
        nameof(IsTabIndentationEnabled), typeof(bool), typeof(ViewMenuBarItem), false);

    /// <summary>Etat courant de l'indentation par tabulations.</summary>
    public bool IsTabIndentationEnabled
    {
        get => (bool)GetValue(IsTabIndentationEnabledProperty);
        set => SetValue(IsTabIndentationEnabledProperty, value);
    }

    public static readonly BindableProperty LanguageProperty = BindableProperty.Create(
        nameof(Language), typeof(SyntaxLanguage), typeof(ViewMenuBarItem), SyntaxLanguage.Sql);

    /// <summary>Langage de l'onglet actif, coche dans le sous-menu.</summary>
    public SyntaxLanguage Language
    {
        get => (SyntaxLanguage)GetValue(LanguageProperty);
        set => SetValue(LanguageProperty, value);
    }

    public static readonly BindableProperty SetLanguageCommandProperty = BindableProperty.Create(
        nameof(SetLanguageCommand), typeof(ICommand), typeof(ViewMenuBarItem), null);

    /// <summary>Change le langage de l'onglet actif ; parametre : la valeur de <see cref="SyntaxLanguage"/>.</summary>
    public ICommand? SetLanguageCommand
    {
        get => (ICommand?)GetValue(SetLanguageCommandProperty);
        set => SetValue(SetLanguageCommandProperty, value);
    }

    public ViewMenuBarItem()
    {
        InitializeComponent();
    }
}
