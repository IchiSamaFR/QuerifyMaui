using System.Windows.Input;

namespace SqlToolManagerMaui.Views.Controls.Editors;

/// <summary>
/// Menu "Fichier" d'un editeur multi-onglets. Le controle ne connait aucun ViewModel :
/// la page hote branche chaque entree via les commandes exposees ci-dessous.
/// </summary>
public partial class FileMenuBarItem : MenuBarItem
{
    public static readonly BindableProperty NewCommandProperty = BindableProperty.Create(
        nameof(NewCommand), typeof(ICommand), typeof(FileMenuBarItem), null);

    /// <summary>Nouveau document (Ctrl+N).</summary>
    public ICommand? NewCommand
    {
        get => (ICommand?)GetValue(NewCommandProperty);
        set => SetValue(NewCommandProperty, value);
    }

    public static readonly BindableProperty OpenCommandProperty = BindableProperty.Create(
        nameof(OpenCommand), typeof(ICommand), typeof(FileMenuBarItem), null);

    /// <summary>Ouvrir un fichier (Ctrl+O).</summary>
    public ICommand? OpenCommand
    {
        get => (ICommand?)GetValue(OpenCommandProperty);
        set => SetValue(OpenCommandProperty, value);
    }

    public static readonly BindableProperty SaveCommandProperty = BindableProperty.Create(
        nameof(SaveCommand), typeof(ICommand), typeof(FileMenuBarItem), null);

    /// <summary>Enregistrer le document actif (Ctrl+S).</summary>
    public ICommand? SaveCommand
    {
        get => (ICommand?)GetValue(SaveCommandProperty);
        set => SetValue(SaveCommandProperty, value);
    }

    public static readonly BindableProperty SaveAsCommandProperty = BindableProperty.Create(
        nameof(SaveAsCommand), typeof(ICommand), typeof(FileMenuBarItem), null);

    /// <summary>Enregistrer sous (Ctrl+Maj+S).</summary>
    public ICommand? SaveAsCommand
    {
        get => (ICommand?)GetValue(SaveAsCommandProperty);
        set => SetValue(SaveAsCommandProperty, value);
    }

    public static readonly BindableProperty CloseTabCommandProperty = BindableProperty.Create(
        nameof(CloseTabCommand), typeof(ICommand), typeof(FileMenuBarItem), null);

    /// <summary>Fermer l'onglet actif (Ctrl+W).</summary>
    public ICommand? CloseTabCommand
    {
        get => (ICommand?)GetValue(CloseTabCommandProperty);
        set => SetValue(CloseTabCommandProperty, value);
    }

    public static readonly BindableProperty QuitCommandProperty = BindableProperty.Create(
        nameof(QuitCommand), typeof(ICommand), typeof(FileMenuBarItem), null);

    /// <summary>Quitter l'application.</summary>
    public ICommand? QuitCommand
    {
        get => (ICommand?)GetValue(QuitCommandProperty);
        set => SetValue(QuitCommandProperty, value);
    }

    public FileMenuBarItem()
    {
        InitializeComponent();
    }
}
