namespace Querify.UI.Inputs.Core;

/// <summary>
/// Version "nue" du Picker natif (sans underline Android / bordure iOS),
/// destinée à être habillée par <see cref="InputField"/>.
/// </summary>
public class BorderlessPicker : Picker
{
    static BorderlessPicker() => BorderlessHandlers.Init();
}
