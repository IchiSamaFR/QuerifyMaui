using System.Collections;

namespace SqlToolManagerMaui.UI.Inputs;

public partial class PickerField
{
    // ── Bindable Properties ───────────────────────────────────────────────────

    /// <summary>Source des éléments : un <see cref="IDictionary"/> (clé/valeur) ou une liste simple.</summary>
    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource),
        typeof(IEnumerable),
        typeof(PickerField),
        propertyChanged: (bindable, _, _) => ((PickerField)bindable).OnItemsSourceChanged());

    /// <summary>Chemin de la propriété servant de libellé (ex. "Name", "User.Name"). Vide = <c>ToString()</c>.</summary>
    public string? DisplayMemberPath
    {
        get => (string?)GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }

    public static readonly BindableProperty DisplayMemberPathProperty = BindableProperty.Create(
        nameof(DisplayMemberPath),
        typeof(string),
        typeof(PickerField),
        propertyChanged: (bindable, _, _) => ((PickerField)bindable).OnDisplayMemberPathChanged());

    /// <summary>Élément source sélectionné (KeyValuePair pour un dictionnaire, l'objet pour une liste).</summary>
    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public static readonly BindableProperty SelectedItemProperty = BindableProperty.Create(
        nameof(SelectedItem),
        typeof(object),
        typeof(PickerField),
        defaultBindingMode: BindingMode.TwoWay,
        propertyChanged: (bindable, _, _) => ((PickerField)bindable).OnSelectedItemChanged());

    /// <summary>Valeur sélectionnée (valeur du dictionnaire, ou l'élément pour une liste).</summary>
    public object? SelectedValue
    {
        get => GetValue(SelectedValueProperty);
        set => SetValue(SelectedValueProperty, value);
    }

    public static readonly BindableProperty SelectedValueProperty = BindableProperty.Create(
        nameof(SelectedValue),
        typeof(object),
        typeof(PickerField),
        defaultBindingMode: BindingMode.TwoWay,
        propertyChanged: (bindable, _, _) => ((PickerField)bindable).OnSelectedValueChanged());

    /// <summary>Clé sélectionnée (clé du dictionnaire, <c>null</c> pour une liste).</summary>
    public object? SelectedKey
    {
        get => GetValue(SelectedKeyProperty);
        set => SetValue(SelectedKeyProperty, value);
    }

    public static readonly BindableProperty SelectedKeyProperty = BindableProperty.Create(
        nameof(SelectedKey),
        typeof(object),
        typeof(PickerField),
        defaultBindingMode: BindingMode.TwoWay,
        propertyChanged: (bindable, _, _) => ((PickerField)bindable).OnSelectedKeyChanged());

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(
        nameof(Placeholder),
        typeof(string),
        typeof(PickerField),
        defaultValue: string.Empty);

    public Color PlaceholderColor
    {
        get => (Color)GetValue(PlaceholderColorProperty);
        set => SetValue(PlaceholderColorProperty, value);
    }

    public static readonly BindableProperty PlaceholderColorProperty = BindableProperty.Create(
        nameof(PlaceholderColor),
        typeof(Color),
        typeof(PickerField),
        defaultValue: Picker.TitleColorProperty.DefaultValue);

    public TextAlignment HorizontalTextAlignment
    {
        get => (TextAlignment)GetValue(HorizontalTextAlignmentProperty);
        set => SetValue(HorizontalTextAlignmentProperty, value);
    }

    public static readonly BindableProperty HorizontalTextAlignmentProperty = BindableProperty.Create(
        nameof(HorizontalTextAlignment),
        typeof(TextAlignment),
        typeof(PickerField),
        defaultValue: TextAlignment.Start);
}
