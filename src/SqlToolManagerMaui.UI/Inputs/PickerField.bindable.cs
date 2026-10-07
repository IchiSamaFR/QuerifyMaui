using CommunityToolkit.Maui;
using System.Collections;

namespace SqlToolManagerMaui.UI.Inputs;

public partial class PickerField
{
    // ── Bindable Properties ───────────────────────────────────────────────────

    /// <summary>Source des éléments : un <see cref="IDictionary"/> (clé/valeur) ou une liste simple.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnItemsSourcePropertyChanged))]
    public partial IEnumerable? ItemsSource { get; set; }

    /// <summary>Chemin de la propriété servant de libellé (ex. "Name", "User.Name"). Vide = <c>ToString()</c>.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnDisplayMemberPathPropertyChanged))]
    public partial string? DisplayMemberPath { get; set; }

    /// <summary>Élément source sélectionné (KeyValuePair pour un dictionnaire, l'objet pour une liste).</summary>
    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay, PropertyChangedMethodName = nameof(OnSelectedItemPropertyChanged))]
    public partial object? SelectedItem { get; set; }

    /// <summary>Valeur sélectionnée (valeur du dictionnaire, ou l'élément pour une liste).</summary>
    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay, PropertyChangedMethodName = nameof(OnSelectedValuePropertyChanged))]
    public partial object? SelectedValue { get; set; }

    /// <summary>Clé sélectionnée (clé du dictionnaire, <c>null</c> pour une liste).</summary>
    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay, PropertyChangedMethodName = nameof(OnSelectedKeyPropertyChanged))]
    public partial object? SelectedKey { get; set; }

    [BindableProperty]
    public partial string Placeholder { get; set; } = string.Empty;

    [BindableProperty]
    public partial Color PlaceholderColor { get; set; } = (Color)Picker.TitleColorProperty.DefaultValue;

    [BindableProperty]
    public partial TextAlignment HorizontalTextAlignment { get; set; } = TextAlignment.Start;

    private static void OnItemsSourcePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((PickerField)bindable).OnItemsSourceChanged();

    private static void OnDisplayMemberPathPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((PickerField)bindable).OnDisplayMemberPathChanged();

    private static void OnSelectedItemPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((PickerField)bindable).OnSelectedItemChanged();

    private static void OnSelectedValuePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((PickerField)bindable).OnSelectedValueChanged();

    private static void OnSelectedKeyPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((PickerField)bindable).OnSelectedKeyChanged();
}
