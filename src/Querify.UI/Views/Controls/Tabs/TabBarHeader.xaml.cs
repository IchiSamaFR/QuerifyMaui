using CommunityToolkit.Maui;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using System.Windows.Input;
using WinUIElement = Microsoft.UI.Xaml.UIElement;

namespace Querify.UI.Views.Controls.Tabs;

public partial class TabBarHeader : ContentView
{
    [BindableProperty]
    public partial string? Title { get; set; }

    [BindableProperty]
    public partial string? ToolTip { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnTitleMemberPathChanged))]
    public partial string? TitleMemberPath { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnToolTipMemberPathChanged))]
    public partial string? ToolTipMemberPath { get; set; }

    [BindableProperty]
    public partial bool IsClosable { get; set; }

    /// <summary>Execute avec l'element de l'onglet (BindingContext) au clic sur le bouton fermer.</summary>
    [BindableProperty]
    public partial ICommand? CloseCommand { get; set; }

    private WinUIElement? _nativeView;
    private PointerEventHandler? _pressed, _released;

    /// <summary>Le bouton du milieu a ete enfonce sur cet onglet : seul son relachement ici ferme l'onglet.</summary>
    private bool _isMiddlePressed;

    public TabBarHeader()
    {
        InitializeComponent();
    }

    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        base.OnHandlerChanging(args);
        Detach();
    }

    // Clic molette = fermer l'onglet ; les gestes MAUI ne connaissent pas le bouton du milieu.
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler?.PlatformView is not WinUIElement view)
            return;

        _nativeView = view;

        // handledEventsToo : MAUI peut deja marquer les evenements pointeur comme traites.
        view.AddHandler(WinUIElement.PointerPressedEvent, _pressed = OnPointerPressed, true);
        view.AddHandler(WinUIElement.PointerReleasedEvent, _released = OnPointerReleased, true);
    }

    private void Detach()
    {
        if (_nativeView is null)
            return;

        _nativeView.RemoveHandler(WinUIElement.PointerPressedEvent, _pressed!);
        _nativeView.RemoveHandler(WinUIElement.PointerReleasedEvent, _released!);
        _nativeView = null;
        _isMiddlePressed = false;
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_nativeView is null || e.Pointer.PointerDeviceType != PointerDeviceType.Mouse)
            return;

        _isMiddlePressed = e.GetCurrentPoint(_nativeView).Properties.IsMiddleButtonPressed;
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_nativeView is null || !_isMiddlePressed)
            return;

        if (e.GetCurrentPoint(_nativeView).Properties.PointerUpdateKind != PointerUpdateKind.MiddleButtonReleased)
            return;

        _isMiddlePressed = false;
        e.Handled = true;

        if (IsClosable)
            Close();
    }

    private static void OnTitleMemberPathChanged(BindableObject bindable, object? oldValue, object? newValue)
        => ((TabBarHeader)bindable).ApplyPath(TitleProperty, newValue as string);

    private static void OnToolTipMemberPathChanged(BindableObject bindable, object? oldValue, object? newValue)
        => ((TabBarHeader)bindable).ApplyPath(ToolTipProperty, newValue as string);

    private void ApplyPath(BindableProperty property, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            RemoveBinding(property);
            return;
        }

        // Pas de Source : le binding suit le BindingContext du header (l'element de ItemsSource),
        // y compris quand il arrive apres le path ou change au recyclage des cellules.
        SetBinding(property, new Binding(path, BindingMode.OneWay));
    }

    private void ImageButton_Clicked(object sender, EventArgs e) => Close();

    private void Close()
    {
        if (CloseCommand?.CanExecute(BindingContext) == true)
            CloseCommand.Execute(BindingContext);
    }
}