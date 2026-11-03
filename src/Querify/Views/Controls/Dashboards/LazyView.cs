using CommunityToolkit.Maui;

namespace Querify.Views.Controls.Dashboards;

/// <summary>
/// Contenu cree seulement la premiere fois que <see cref="IsActive"/> passe a vrai, puis simplement masque.
/// Un element masque par IsVisible garde sa vue native : les outils du concepteur (champs, menus, poignees)
/// etaient crees pour chaque carte a chaque ouverture, meme en consultation.
/// Un Grid et non un ContentView : CascadeInputTransparent n'agit que sur un Layout, un ContentView transparent
/// coupait les clics de tout son contenu (poignees, titre, type de carte).
/// </summary>
public sealed partial class LazyView : Grid
{
    [BindableProperty(PropertyChangedMethodName = nameof(OnStateChanged))]
    public partial bool IsActive { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnStateChanged))]
    public partial DataTemplate? ContentTemplate { get; set; }

    public LazyView()
    {
        IsVisible = false;
    }

    private static void OnStateChanged(BindableObject bindable, object oldValue, object newValue)
        => ((LazyView)bindable).Update();

    private void Update()
    {
        if (IsActive && Children.Count == 0 && ContentTemplate?.CreateContent() is View content)
            Children.Add(content);

        IsVisible = IsActive;
    }
}
