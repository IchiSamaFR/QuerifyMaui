namespace Querify.UI.Inputs;

/// <summary>
/// Bascule a deux etats dans l'habillage d'un <see cref="ButtonView"/> : un clic n'importe ou inverse
/// <see cref="IsToggled"/> puis execute <see cref="ButtonView.Command"/>. Les deux icones restent visibles,
/// celle de l'etat courant en avant ; le libelle nomme l'etat courant. L'etat se lie au choix par
/// <see cref="IsToggled"/> (bool) ou par <see cref="ToggledValue"/> entre <see cref="ToggledOffValue"/>
/// et <see cref="ToggledOnValue"/> (enum, objet...).
///
/// Exemple :
/// <![CDATA[
///   <inp:ToggleButtonField IsToggled="{Binding IsDesignMode}"
///                          OffIcon="{mk:FluentImage Icon=ContentView20}"
///                          OffText="Dashboard"
///                          OnIcon="{mk:FluentImage Icon=ResizeSmall20}"
///                          OnText="Concepteur" />
///
///   <inp:ToggleButtonField ToggledValue="{Binding Mode}"
///                          ToggledOffValue="{x:Static en:DashboardMode.View}"
///                          ToggledOnValue="{x:Static en:DashboardMode.Design}" ... />
/// ]]>
/// </summary>
public partial class ToggleButtonField : ButtonView
{
    protected override Layout _layout { get; } = new HorizontalStackLayout
    {
        Spacing = 4,
        VerticalOptions = LayoutOptions.Center,
    };

    private bool _isSyncing;

    private readonly Image _offImage = new() { VerticalOptions = LayoutOptions.Center };
    private readonly Image _onImage = new() { VerticalOptions = LayoutOptions.Center };
    private readonly Label _label = new()
    {
        VerticalTextAlignment = TextAlignment.Center,
        Margin = new Thickness(2, 0, 0, 0),
    };

    public ToggleButtonField()
    {
        _offImage.Margin = new Thickness(0, 2, 0, 0);
        _onImage.Margin = new Thickness(0, 2, 0, 0);

        BindParts();

        // Clicked part avant Command : la commande voit deja le nouvel etat.
        Clicked += (_, _) => IsToggled = !IsToggled;
        UpdateState();
    }

    private void BindParts()
    {
        _layout.SetBinding(Microsoft.Maui.Controls.Layout.PaddingProperty, new Binding(nameof(Padding), source: this));
        _offImage.SetBinding(Image.SourceProperty, new Binding(nameof(OffIcon), source: this));
        _onImage.SetBinding(Image.SourceProperty, new Binding(nameof(OnIcon), source: this));

        foreach (var image in new[] { _offImage, _onImage })
        {
            image.SetBinding(WidthRequestProperty, new Binding(nameof(IconSize), source: this));
            image.SetBinding(HeightRequestProperty, new Binding(nameof(IconSize), source: this));
        }

        _label.SetBinding(Label.FontSizeProperty, new Binding(nameof(FontSize), source: this));
        _label.SetBinding(WidthRequestProperty, new Binding(nameof(LabelWidthRequest), source: this));
    }

    /// <summary>Mode valeur actif des qu'une des deux valeurs est posee : sinon seul IsToggled compte.</summary>
    private bool UsesValues => ToggledOnValue is not null || ToggledOffValue is not null;

    private void OnIsToggledChanged()
    {
        UpdateState();

        if (_isSyncing || !UsesValues)
            return;

        _isSyncing = true;
        ToggledValue = IsToggled ? ToggledOnValue : ToggledOffValue;
        _isSyncing = false;
    }

    /// <summary>Ne repousse pas ToggledValue : une valeur hors des deux (ex. vue Source) est conservee.</summary>
    private void SyncIsToggledFromValue()
    {
        if (_isSyncing || !UsesValues)
            return;

        _isSyncing = true;
        IsToggled = ToggledOnValue is not null && Equals(ToggledValue, ToggledOnValue);
        _isSyncing = false;
    }

    /// <summary>L'opacite et le libelle dependent de deux proprietes a la fois : mis a jour ici plutot que par converters.</summary>
    private void UpdateState()
    {
        Children.Clear();
        if (_offImage?.Source != null)
        {
            _offImage.Opacity = IsToggled ? InactiveIconOpacity : 1d;
            Children.Add(_offImage);
        }
        if (_onImage?.Source != null)
        {
            _onImage.Opacity = IsToggled ? 1d : InactiveIconOpacity;
            Children.Add(_onImage);
        }
        if (_label != null)
        {
            _label.Text = IsToggled ? OnText : OffText;
            _label.IsVisible = !string.IsNullOrEmpty(_label.Text);

            if (_label.IsVisible)
                Children.Add(_label);
        }

        // Sans infobulle d'etat, celle posee directement (ToolTipProperties.Text) est conservee.
        if (OnToolTip is not null || OffToolTip is not null)
            ToolTipProperties.SetText(this, IsToggled ? OnToolTip : OffToolTip);
    }
}
