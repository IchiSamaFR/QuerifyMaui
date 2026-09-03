using System.ComponentModel.DataAnnotations;

namespace SqlToolManagerMaui.UI.Inputs.Core;

/// <summary>
/// Une règle de validation applicable à un champ de saisie.
/// </summary>
public interface IFieldValidation
{
    string Message { get; }

    bool Validate(object? value);
}

/// <summary>
/// Un control capable de porter des validations (équivalent maison de InputKit.IValidatable).
/// </summary>
public interface IValidatable
{
    IList<IFieldValidation> Validations { get; }

    bool IsValid { get; }

    void DisplayValidation();

    void ResetValidation();
}

/// <summary>
/// Adapte un <see cref="ValidationAttribute"/> (DataAnnotations) en <see cref="IFieldValidation"/>.
/// </summary>
public class DataAnnotationValidation : IFieldValidation
{
    private readonly ValidationAttribute _attribute;
    private readonly string _displayName;

    public DataAnnotationValidation(ValidationAttribute attribute, string displayName)
    {
        _attribute = attribute;
        _displayName = displayName;
    }

    public string Message => _attribute.FormatErrorMessage(_displayName);

    public bool Validate(object? value)
    {
        // Aligne le comportement string vide / null sur DataAnnotations côté serveur.
        if (value is string s && s.Length == 0)
            value = null;

        return _attribute.IsValid(value);
    }
}
