using CommunityToolkit.Maui;
using SqlToolManagerMaui.Core.Models.Editors.Analysis;

namespace SqlToolManagerMaui.Views.Controls.Editors.Analysis;

/// <summary>Infobulle superposee a l'editeur : erreurs a l'endroit survole, puis nature et type du nom.</summary>
public partial class CodeTooltipPopup : ContentView
{
    public const double MaxPopupWidth = 560;

    /// <summary>Ligne principale (ex. "(propriete) string Student.Name").</summary>
    [BindableProperty]
    public partial string? Signature { get; set; }

    [BindableProperty]
    public partial string? Description { get; set; }

    [BindableProperty]
    public partial IReadOnlyList<CodeDiagnostic>? Diagnostics { get; set; }

    public CodeTooltipPopup()
    {
        InitializeComponent();
    }
}
