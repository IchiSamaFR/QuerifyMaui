using CommunityToolkit.Maui;
using SqlToolManagerMaui.Core.Models.Editors;
using System.Collections;
using System.Windows.Input;

namespace SqlToolManagerMaui.Views.Controls.Editors;

/// <summary>
/// Barre d'onglets facon Windows 11 pour les documents ouverts de l'editeur :
/// selection a deux sens, fermeture par onglet, bouton "+" et defilement horizontal
/// qui garde l'onglet actif visible.
/// </summary>
public partial class FileTabControl : ContentView
{
    [BindableProperty]
    public partial IEnumerable? ItemsSource { get; set; }

    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay, PropertyChangedMethodName = nameof(OnSelectedItemChanged))]
    public partial EditorDocument? SelectedItem { get; set; }

    /// <summary>Recoit l'<see cref="EditorDocument"/> de l'onglet a fermer en parametre.</summary>
    [BindableProperty]
    public partial ICommand? CloseCommand { get; set; }

    [BindableProperty]
    public partial ICommand? NewCommand { get; set; }

    public FileTabControl()
    {
        InitializeComponent();
    }

    private void OnTabSelected(object? sender, EventArgs e)
    {
        if (sender is FileTabItem { Document: { } document })
            SelectedItem = document;
    }

    private static void OnSelectedItemChanged(BindableObject bindable, object oldValue, object newValue)
        => ((FileTabControl)bindable).ScrollToSelected();

    /// <summary>Fait defiler la barre jusqu'a l'onglet actif (ex. onglet fraichement ouvert hors ecran).</summary>
    private void ScrollToSelected()
    {
        // L'onglet n'existe pas encore tant que le BindableLayout n'a pas reagi a l'ajout.
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(80), async () =>
        {
            var tab = TabsHost.Children
                .OfType<FileTabItem>()
                .FirstOrDefault(t => ReferenceEquals(t.Document, SelectedItem));

            if (tab is not null)
                await TabScroll.ScrollToAsync(tab, ScrollToPosition.MakeVisible, animated: true);
        });
    }
}
