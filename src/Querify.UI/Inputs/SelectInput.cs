namespace Querify.UI.Inputs;

/// <summary>
/// Picker natif de Querify : API MAUI intacte (ItemsSource, SelectedItem, ItemDisplayBinding),
/// rendu compact et sans bordure, à placer dans un <see cref="FormField"/> qui dessine le cadre.
/// </summary>
public partial class SelectInput : Picker
{
    static SelectInput() => Core.BorderlessHandlers.Init();
}
