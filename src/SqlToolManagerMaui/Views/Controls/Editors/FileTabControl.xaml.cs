using System.Collections;
using System.Windows.Input;
using SqlToolManagerMaui.Models.Editors;

namespace SqlToolManagerMaui.Views.Controls.Editors;

/// <summary>
/// Barre d'onglets facon Windows 11 pour les documents ouverts de l'editeur :
/// selection a deux sens, fermeture par onglet, bouton "+" et defilement horizontal
/// qui garde l'onglet actif visible.
/// </summary>
public partial class FileTabControl : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource), typeof(IEnumerable), typeof(FileTabControl), null);

    public static readonly BindableProperty SelectedItemProperty = BindableProperty.Create(
        nameof(SelectedItem), typeof(EditorDocument), typeof(FileTabControl), null,
        BindingMode.TwoWay,
        propertyChanged: (b, _, _) => ((FileTabControl)b).ScrollToSelected());

    /// <summary>Recoit l'<see cref="EditorDocument"/> de l'onglet a fermer en parametre.</summary>
    public static readonly BindableProperty CloseCommandProperty = BindableProperty.Create(
        nameof(CloseCommand), typeof(ICommand), typeof(FileTabControl), null);

    public static readonly BindableProperty NewCommandProperty = BindableProperty.Create(
        nameof(NewCommand), typeof(ICommand), typeof(FileTabControl), null);

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public EditorDocument? SelectedItem
    {
        get => (EditorDocument?)GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public ICommand? CloseCommand
    {
        get => (ICommand?)GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    public ICommand? NewCommand
    {
        get => (ICommand?)GetValue(NewCommandProperty);
        set => SetValue(NewCommandProperty, value);
    }

    public FileTabControl()
    {
        InitializeComponent();
    }

    private void OnTabSelected(object? sender, EventArgs e)
    {
        if (sender is FileTabItem { Document: { } document })
            SelectedItem = document;
    }

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
