namespace SqlToolManagerMaui.Core.Enums.Editors;

/// <summary>Nature d'un onglet de la page Requete.</summary>
public enum DocumentKind
{
    /// <summary>Script SQL / LINQ (ou texte libre) edite dans le CodeEditor.</summary>
    Script,

    /// <summary>Dashboard .schx (grille de cartes alimentees par des requetes parametrees).</summary>
    Dashboard,
}
