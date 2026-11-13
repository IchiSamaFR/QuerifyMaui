using CommunityToolkit.Maui;
using System.Windows.Input;

namespace Querify.Views.Controls.Editors.Search;

/// <summary>
/// Barre Rechercher / Remplacer superposee a l'editeur. Elle ne cherche rien elle-meme : saisie, options et
/// commandes sont lues et posees par <see cref="FindReplaceController"/>.
/// </summary>
public partial class FindReplacePopup : ContentView
{
    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
    public partial string SearchText { get; set; } = string.Empty;

    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
    public partial string ReplaceText { get; set; } = string.Empty;

    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
    public partial bool IsMatchCase { get; set; }

    /// <summary>Ligne de remplacement affichee (Ctrl+H).</summary>
    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
    public partial bool IsReplaceMode { get; set; }

    /// <summary>Compteur (« 2 sur 5 », « Aucun résultat »).</summary>
    [BindableProperty]
    public partial string ResultText { get; set; } = string.Empty;

    [BindableProperty]
    public partial ICommand? FindNextCommand { get; set; }

    [BindableProperty]
    public partial ICommand? FindPreviousCommand { get; set; }

    [BindableProperty]
    public partial ICommand? ReplaceCommand { get; set; }

    [BindableProperty]
    public partial ICommand? ReplaceAllCommand { get; set; }

    [BindableProperty]
    public partial ICommand? CloseCommand { get; set; }

    [BindableProperty(DefaultValueCreatorMethodName = nameof(CreateToggleReplaceCommand))]
    public partial ICommand ToggleReplaceCommand { get; private set; }

    [BindableProperty(DefaultValueCreatorMethodName = nameof(CreateToggleMatchCaseCommand))]
    public partial ICommand ToggleMatchCaseCommand { get; private set; }

    public FindReplacePopup()
    {
        InitializeComponent();

#if WINDOWS
        AttachKeys(SearchInput);
        AttachKeys(ReplaceInput);
#endif
    }

    private static object CreateToggleReplaceCommand(BindableObject bindable)
        => new Command(() => ((FindReplacePopup)bindable).IsReplaceMode = !((FindReplacePopup)bindable).IsReplaceMode);

    private static object CreateToggleMatchCaseCommand(BindableObject bindable)
        => new Command(() => ((FindReplacePopup)bindable).IsMatchCase = !((FindReplacePopup)bindable).IsMatchCase);

    /// <summary>Focus sur la recherche, texte selectionne pour etre remplace d'un coup.</summary>
    public void FocusSearch()
    {
        SearchInput.Focus();
        SearchInput.CursorPosition = 0;
        SearchInput.SelectionLength = SearchInput.Text?.Length ?? 0;
    }

    private static void Execute(ICommand? command)
    {
        if (command?.CanExecute(null) == true)
            command.Execute(null);
    }

#if WINDOWS
    // Entree, F3, Echap et Ctrl+F / Ctrl+H arrivent au TextBox natif : MAUI n'expose que Completed.
    private void AttachKeys(Entry field)
    {
        field.HandlerChanged += (_, _) =>
        {
            if (field.Handler?.PlatformView is Microsoft.UI.Xaml.UIElement element)
                element.PreviewKeyDown += (_, e) => e.Handled |= HandleKey(field, e.Key);
        };
    }

    private bool HandleKey(Entry field, Windows.System.VirtualKey key)
    {
        var shift = IsDown(Windows.System.VirtualKey.Shift);
        var control = IsDown(Windows.System.VirtualKey.Control);

        switch (key)
        {
            case Windows.System.VirtualKey.Enter:
                Execute(shift ? FindPreviousCommand : field == ReplaceInput ? ReplaceCommand : FindNextCommand);
                return true;
            case Windows.System.VirtualKey.F3:
                Execute(shift ? FindPreviousCommand : FindNextCommand);
                return true;
            case Windows.System.VirtualKey.Escape:
                Execute(CloseCommand);
                return true;
            case Windows.System.VirtualKey.F or Windows.System.VirtualKey.H when control:
                IsReplaceMode = key == Windows.System.VirtualKey.H;
                FocusSearch();
                return true;
            default:
                return false;
        }
    }

    private static bool IsDown(Windows.System.VirtualKey key)
        => Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
#endif
}
