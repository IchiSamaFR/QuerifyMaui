namespace Querify.UI.Inputs;

/// <summary>
/// Entry native de Querify : API MAUI intacte, rendu compact et sans bordure,
/// à placer dans un <see cref="FormField"/> qui dessine le cadre.
/// </summary>
public partial class TextInput : Entry
{
    static TextInput() => Core.BorderlessHandlers.Init();
}
