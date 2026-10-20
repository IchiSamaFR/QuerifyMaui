using SqlToolManagerMaui.Core.Enums.Dashboards;
using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Enums.Settings;

namespace SqlToolManagerMaui.Core.Models.Settings;

/// <summary>
/// Un onglet memorise. Les textes ne sont conserves que pour un document sans fichier ou modifie ;
/// sinon le fichier est relu sur le disque a la restauration.
/// </summary>
public sealed class WorkspaceTab
{
    public WorkspaceTabKind Kind { get; set; }

    public string? FilePath { get; set; }

    public string FallbackName { get; set; } = string.Empty;

    public SyntaxLanguage Language { get; set; }

    /// <summary>Derniere version enregistree (sert a retrouver l'etat modifie).</summary>
    public string? FileText { get; set; }

    /// <summary>Texte en cours d'edition ; null quand le fichier est a relire.</summary>
    public string? EditorText { get; set; }

    public int SelectionAnchor { get; set; }

    public int SelectionActive { get; set; }

    /// <summary>Vue d'un dashboard (consultation, concepteur ou JSON).</summary>
    public DashboardMode DashboardMode { get; set; }
}
