namespace SqlToolManagerMaui.Core.Enums.Dashboards;

/// <summary>Affichage d'un onglet .schx.</summary>
public enum DashboardMode
{
    /// <summary>Consultation : parametres, cartes, navigation entre pages.</summary>
    View,

    /// <summary>Concepteur : placer, dimensionner et configurer les cartes.</summary>
    Design,

    /// <summary>JSON du fichier dans l'editeur de texte.</summary>
    Source,
}
