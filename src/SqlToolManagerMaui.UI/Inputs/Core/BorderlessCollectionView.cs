namespace SqlToolManagerMaui.UI.Inputs.Core;

/// <summary>
/// CollectionView sans habillage natif des elements (fond de survol/selection, indicateur, coins arrondis WinUI) :
/// le gabarit de l'element porte seul son visuel.
/// </summary>
public class BorderlessCollectionView : CollectionView
{
    static BorderlessCollectionView() => BorderlessHandlers.Init();
}
