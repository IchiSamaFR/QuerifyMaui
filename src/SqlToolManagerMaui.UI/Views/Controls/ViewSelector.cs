namespace SqlToolManagerMaui.UI.Views.Controls;

/// <summary>
/// Choisit la vue a afficher pour un element parmi des vues deja instanciees.
/// Contrairement a un <see cref="DataTemplateSelector"/>, rien n'est genere : la meme instance est reaffichee d'un element a l'autre.
/// </summary>
public abstract class ViewSelector
{
    public abstract View? SelectView(object item, BindableObject container);
}
