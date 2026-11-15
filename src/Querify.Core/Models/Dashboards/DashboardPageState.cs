using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Querify.Core.Models.Dashboards;

/// <summary>Page ouverte (une etape du fil d'Ariane) : ses cartes et les valeurs de parametres avec lesquelles elle a ete ouverte.</summary>
public sealed partial class DashboardPageState : ObservableObject
{
    public DashboardPageDefinition Definition { get; }

    /// <summary>Valeurs des parametres de cette etape, restaurees au retour.</summary>
    public Dictionary<string, string?> ParameterValues { get; }

    public ObservableCollection<DashboardCardState> Cards { get; } = [];

    public DashboardPageState(DashboardPageDefinition definition, IReadOnlyDictionary<string, string?> parameterValues)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(parameterValues);

        Definition = definition;
        ParameterValues = new Dictionary<string, string?>(parameterValues, StringComparer.OrdinalIgnoreCase);
        foreach (var card in definition.Cards)
            Cards.Add(new DashboardCardState(card));
    }

    public string Title => Definition.Title;

    /// <summary>La page a ete renommee (depuis son onglet du concepteur).</summary>
    public void NotifyTitleChanged() => OnPropertyChanged(nameof(Title));

    /// <summary>Resultats liberes (etape ancienne du fil d'Ariane) : les cartes sont a relire au retour sur la page.</summary>
    public bool NeedsReload { get; private set; }

    public void ReleaseResults()
    {
        if (NeedsReload)
            return;

        foreach (var card in Cards)
            card.Result = null;

        NeedsReload = true;
    }

    /// <summary>La relecture des cartes est lancee.</summary>
    public void MarkReloaded() => NeedsReload = false;

    /// <summary>Premiere ligne libre sous la derniere carte.</summary>
    public int BottomRow => Cards.Count == 0 ? 0 : Cards.Max(c => c.Row + c.RowSpan);
}
