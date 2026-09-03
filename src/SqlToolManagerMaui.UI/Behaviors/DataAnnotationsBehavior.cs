using SqlToolManagerMaui.UI.Inputs.Core;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace SqlToolManagerMaui.UI.Behaviors;

/// <summary>
/// Applique les ValidationAttributes (DataAnnotations) de la propriété bindée
/// au champ (IValidatable). Remplace UraniumUI.Validations.DataAnnotationsBehavior.
/// Usage : &lt;b:DataAnnotationsBehavior Binding="{Binding UserName}" /&gt;
/// </summary>
public class DataAnnotationsBehavior : Behavior<View>
{
    private bool _isFocused;

    protected View? _view { get; private set; }

    /// <summary>Binding vers la propriété du BindingContext portant les annotations.
    /// Attention : ne fonctionne pas avec les compiled bindings (x:DataType) ;
    /// utiliser <see cref="PropertyName"/> dans ce cas.</summary>
    public BindingBase? Binding { get; set; }

    /// <summary>Nom (ou chemin "A.B") de la propriété portant les annotations.</summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// Si true, la validation ne s'affiche qu'à la perte de focus (Unfocused).
    /// La validation déclenchée depuis le VM (submit) s'affiche toujours.
    /// </summary>
    public static readonly BindableProperty ValidateOnUnfocusedProperty = BindableProperty.Create(
        nameof(ValidateOnUnfocused), typeof(bool), typeof(DataAnnotationsBehavior), true,
        propertyChanged: (bindable, _, _) => ((DataAnnotationsBehavior)bindable).ApplyAnnotations());

    public bool ValidateOnUnfocused
    {
        get => (bool)GetValue(ValidateOnUnfocusedProperty);
        set => SetValue(ValidateOnUnfocusedProperty, value);
    }

    protected override void OnAttachedTo(View bindable)
    {
        base.OnAttachedTo(bindable);
        _view = bindable;
        bindable.PropertyChanged += OnAssociatedObjectPropertyChanged;
        bindable.BindingContextChanged += OnViewBindingContextChanged;
        ApplyAnnotations();
    }

    private void OnAssociatedObjectPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(VisualElement.IsFocused))
            return;

        _isFocused = _view?.IsFocused ?? false;
    }

    protected override void OnDetachingFrom(View bindable)
    {
        bindable.BindingContextChanged -= OnViewBindingContextChanged;
        _view = null;
        base.OnDetachingFrom(bindable);
    }

    private void OnViewBindingContextChanged(object? sender, EventArgs e) => ApplyAnnotations();

    protected virtual void ApplyAnnotations()
    {
        if (_view is not IValidatable validatable)
            return;

        var path = PropertyName ?? (Binding as Binding)?.Path;
        var context = _view.BindingContext;

        validatable.Validations.Clear();

        if (string.IsNullOrEmpty(path) || context is null)
        {
            validatable.DisplayValidation();
            return;
        }

        // Si ValidateOnUnfocused=true et que le champ est actif, on ignore le changement :
        // la validation sera affichée à l'Unfocused via InputField.OnContentUnfocused.
        if (ValidateOnUnfocused && _isFocused)
            return;

        var propertyInfo = ResolveProperty(context.GetType(), path);
        if (propertyInfo is null)
        {
            validatable.DisplayValidation();
            return;
        }

        AddValidations(validatable, propertyInfo);
        validatable.DisplayValidation();
    }

    internal static PropertyInfo? ResolveProperty(Type type, string path)
    {
        PropertyInfo? property = null;
        Type currentType = type;

        foreach (var segment in path.Split('.'))
        {
            property = currentType.GetProperty(segment);
            if (property is null)
                return null;
            currentType = property.PropertyType;
        }

        return property;
    }

    internal static void AddValidations(IValidatable validatable, PropertyInfo propertyInfo)
    {
        var displayAttribute = propertyInfo.GetCustomAttribute<DisplayAttribute>(true);
        string propertyName = displayAttribute?.GetName() ?? propertyInfo.Name;

        foreach (var attribute in propertyInfo.GetCustomAttributes<ValidationAttribute>(true))
            validatable.Validations.Add(new DataAnnotationValidation(attribute, propertyName));
    }
}
