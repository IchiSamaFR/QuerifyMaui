using CommunityToolkit.Maui;
using SqlToolManagerMaui.Core.Enums.Dashboards;
using SqlToolManagerMaui.Core.Models.Dashboards;
using SqlToolManagerMaui.Core.Models.Editors;
using SqlToolManagerMaui.Core.Models.Editors.Highlighting;
using SqlToolManagerMaui.Core.Models.Settings;
using SqlToolManagerMaui.Databases.Models.Connections;
using System.Windows.Input;

namespace SqlToolManagerMaui.Views.Controls.Dashboards;

/// <summary>
/// Panneau du concepteur : proprietes de la carte selectionnee et page editee.
/// Les champs ecrivent directement dans la definition, que l'hote reecrit dans le fichier.
/// </summary>
public partial class DashboardDesignerPanel : ContentView
{
    /// <summary>Rendus proposes pour une carte.</summary>
    public static DashboardCardKind[] CardKinds { get; } = Enum.GetValues<DashboardCardKind>();

    [BindableProperty]
    public partial DashboardSession? Session { get; set; }

    /// <summary>Coloration de l'editeur de requete (SQL).</summary>
    [BindableProperty]
    public partial SyntaxConfiguration? SyntaxConfiguration { get; set; }

    /// <summary>Options d'affichage partagees avec l'editeur principal.</summary>
    [BindableProperty]
    public partial EditorPreferences? EditorPreferences { get; set; }

    /// <summary>Connexion active : alimente l'autocompletion des tables et colonnes.</summary>
    [BindableProperty]
    public partial DatabaseConnectionInfo? Connection { get; set; }

    /// <summary>Relit la carte (parametre : <see cref="DashboardCardState"/>).</summary>
    [BindableProperty]
    public partial ICommand? RunCardCommand { get; set; }

    [BindableProperty]
    public partial ICommand? DeleteCardCommand { get; set; }

    /// <summary>Ouvre une page dans le concepteur (parametre : <see cref="DashboardPageDefinition"/>).</summary>
    [BindableProperty]
    public partial ICommand? OpenPageCommand { get; set; }

    [BindableProperty]
    public partial ICommand? AddPageCommand { get; set; }

    [BindableProperty]
    public partial ICommand? DeletePageCommand { get; set; }

    public DashboardDesignerPanel()
    {
        InitializeComponent();
    }
}
