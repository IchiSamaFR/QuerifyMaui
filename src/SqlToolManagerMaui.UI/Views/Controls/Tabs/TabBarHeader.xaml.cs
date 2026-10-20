using CommunityToolkit.Maui;

namespace SqlToolManagerMaui.UI.Views.Controls.Tabs;

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

    [BindableProperty]
    public partial Action<object> CloseAction { get; set; }

    public TabBarHeader()
    {
        InitializeComponent();
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

    private void ImageButton_Clicked(object sender, EventArgs e)
    {
        CloseAction?.Invoke(BindingContext);
    }
}