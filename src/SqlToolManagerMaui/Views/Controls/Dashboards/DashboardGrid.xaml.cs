using CommunityToolkit.Maui;
using SqlToolManagerMaui.Core.Enums.Dashboards;
using SqlToolManagerMaui.Core.Models.Dashboards;
using SqlToolManagerMaui.Core.Models.Editors.Highlighting;
using SqlToolManagerMaui.Core.Models.Settings;
using SqlToolManagerMaui.Databases.Models.Connections;
using System.Windows.Input;

namespace SqlToolManagerMaui.Views.Controls.Dashboards;

/// <summary>
/// Dashboard d'un onglet .schx : parametres, fil d'Ariane et grille de cartes. Dans le concepteur, les onglets
/// des pages remplacent le fil d'Ariane, les parametres se declarent dans leur barre, les cases libres recoivent
/// les cartes ajoutees ou glissees et le panneau du bas edite la carte selectionnee.
/// </summary>
public partial class DashboardGrid : ContentView
{
    /// <summary>Rendus proposes pour une carte.</summary>
    public static DashboardCardKind[] CardKinds { get; } = Enum.GetValues<DashboardCardKind>();

    [BindableProperty]
    public partial DashboardSession? Session { get; set; }

    [BindableProperty]
    public partial SyntaxConfiguration? SyntaxConfiguration { get; set; }

    [BindableProperty]
    public partial EditorPreferences? EditorPreferences { get; set; }

    [BindableProperty]
    public partial DatabaseConnectionInfo? Connection { get; set; }

    [BindableProperty]
    public partial bool IsDashboardParamsOpen { get; set; }

    /// <summary>Nombre de lignes de la grille (les definitions de lignes sont creees en code).</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnGridSizeChanged))]
    public partial int RowCount { get; set; } = 6;

    [BindableProperty(PropertyChangedMethodName = nameof(OnGridSizeChanged))]
    public partial int ColumnCount { get; set; } = 12;

    /// <summary>Hauteur d'une ligne de la grille.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnGridSizeChanged))]
    public partial double RowHeight { get; set; } = 140d;

    /// <summary>Relit la carte (parametre : <see cref="DashboardCardState"/>).</summary>
    [BindableProperty]
    public partial ICommand? RunCardCommand { get; set; }

    /// <summary>Applique les valeurs saisies et relit les cartes.</summary>
    [BindableProperty]
    public partial ICommand? ApplyParametersCommand { get; set; }

    [BindableProperty]
    public partial ICommand? BackCommand { get; set; }

    /// <summary>Revient a une etape du fil d'Ariane (parametre : <see cref="DashboardPageState"/>).</summary>
    [BindableProperty]
    public partial ICommand? GoToStepCommand { get; set; }

    /// <summary>Concepteur : selectionne une carte (parametre : <see cref="DashboardCardState"/>).</summary>
    [BindableProperty]
    public partial ICommand? SelectCardCommand { get; set; }

    /// <summary>Concepteur : carte selectionnee, cible des touches Echap (deselection) et Suppr (suppression).</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnSelectedCardChanged))]
    public partial DashboardCardState? SelectedCard { get; set; }

    /// <summary>Concepteur : supprime une carte (parametre : <see cref="DashboardCardState"/>).</summary>
    [BindableProperty]
    public partial ICommand? DeleteCardCommand { get; set; }

    /// <summary>Concepteur : ajoute une carte dans une case libre (parametre : <see cref="DashboardCell"/>).</summary>
    [BindableProperty]
    public partial ICommand? AddCardCommand { get; set; }

    /// <summary>Concepteur : debut du glisser d'une carte (parametre : <see cref="DashboardCardState"/>).</summary>
    [BindableProperty]
    public partial ICommand? StartDragCommand { get; set; }

    /// <summary>Concepteur : depot de la carte glissee sur une case (parametre : <see cref="DashboardCell"/>).</summary>
    [BindableProperty]
    public partial ICommand? DropCommand { get; set; }

    /// <summary>Concepteur : la carte glissee survole une case (parametre : <see cref="DashboardCell"/>).</summary>
    [BindableProperty]
    public partial ICommand? DragOverCommand { get; set; }

    /// <summary>Concepteur : la carte glissee quitte une case (parametre : <see cref="DashboardCell"/>).</summary>
    [BindableProperty]
    public partial ICommand? DragLeaveCommand { get; set; }

    /// <summary>Concepteur : fin du glisser, depose ou non.</summary>
    [BindableProperty]
    public partial ICommand? EndDragCommand { get; set; }

    /// <summary>Concepteur : ouvre une page (parametre : <see cref="DashboardPageDefinition"/>).</summary>
    [BindableProperty]
    public partial ICommand? OpenPageCommand { get; set; }

    [BindableProperty]
    public partial ICommand? AddPageCommand { get; set; }

    /// <summary>Concepteur : supprime une page (parametre : <see cref="DashboardPageDefinition"/>).</summary>
    [BindableProperty]
    public partial ICommand? DeletePageCommand { get; set; }

    /// <summary>Concepteur : declare un nouveau parametre.</summary>
    [BindableProperty]
    public partial ICommand? AddParameterCommand { get; set; }

    /// <summary>Concepteur : modifie un parametre (parametre : <see cref="DashboardParameterValue"/>).</summary>
    [BindableProperty]
    public partial ICommand? EditParameterCommand { get; set; }

    /// <summary>Concepteur : supprime un parametre (parametre : <see cref="DashboardParameterValue"/>).</summary>
    [BindableProperty]
    public partial ICommand? DeleteParameterCommand { get; set; }

    public DashboardGrid()
    {
        InitializeComponent();
        ApplyGridSize();
    }

    private static void OnGridSizeChanged(BindableObject bindable, object oldValue, object newValue)
        => ((DashboardGrid)bindable).ApplyGridSize();

    private static void OnSelectedCardChanged(BindableObject bindable, object oldValue, object newValue)
    {
#if WINDOWS
        // Les cartes ne sont pas focalisables : le ScrollViewer natif recoit le clavier des qu'une carte est selectionnee.
        if (newValue is not null)
            ((DashboardGrid)bindable).FocusGrid();
#endif
    }

    private void OnGridScrollHandlerChanged(object? sender, EventArgs e)
    {
#if WINDOWS
        AttachKeyboard();
#endif
    }

#if WINDOWS
    private global::Microsoft.UI.Xaml.Controls.ScrollViewer? _nativeScroll;

    private void AttachKeyboard()
    {
        if (_nativeScroll is not null)
            _nativeScroll.KeyDown -= OnNativeKeyDown;

        _nativeScroll = GridScroll.Handler?.PlatformView as global::Microsoft.UI.Xaml.Controls.ScrollViewer;
        if (_nativeScroll is null)
            return;

        _nativeScroll.IsTabStop = true;
        _nativeScroll.UseSystemFocusVisuals = false;
        _nativeScroll.KeyDown += OnNativeKeyDown;
    }

    // FocusState.Pointer : pas de cadre de focus autour de la grille.
    private void FocusGrid()
        => _nativeScroll?.Focus(global::Microsoft.UI.Xaml.FocusState.Pointer);

    private void OnNativeKeyDown(object sender, global::Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        // Seulement dans le concepteur, et jamais pendant une saisie dans un champ de texte.
        if (Session is not { IsDesigning: true } || SelectedCard is not { } card
            || e.OriginalSource is global::Microsoft.UI.Xaml.Controls.TextBox)
            return;

        ICommand? command;
        object? parameter;
        switch (e.Key)
        {
            case global::Windows.System.VirtualKey.Escape:
                (command, parameter) = (SelectCardCommand, null);
                break;
            case global::Windows.System.VirtualKey.Delete:
                (command, parameter) = (DeleteCardCommand, card);
                break;
            default:
                return;
        }

        if (command?.CanExecute(parameter) == true)
        {
            command.Execute(parameter);
            e.Handled = true;
        }
    }
#endif

    /// <summary>Les calques (cases, cartes, apercu) partagent les memes colonnes et lignes.</summary>
    private void ApplyGridSize()
    {
        foreach (var layer in new[] { CellsLayer, CardsLayer, PreviewLayer })
        {
            layer.ColumnDefinitions.Clear();
            for (var i = 0; i < Math.Max(1, ColumnCount); i++)
                layer.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            layer.RowDefinitions.Clear();
            for (var i = 0; i < Math.Max(1, RowCount); i++)
                layer.RowDefinitions.Add(new RowDefinition(RowHeight));
        }
    }

    private void OnFlowchartButtonClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement platformView)
            return;

        var flyout = Microsoft.UI.Xaml.Controls.Primitives.FlyoutBase.GetAttachedFlyout(platformView)
                     ?? platformView.ContextFlyout;

        flyout?.ShowAt(platformView);
    }
}
