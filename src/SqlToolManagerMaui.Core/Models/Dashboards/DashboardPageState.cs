using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace SqlToolManagerMaui.Core.Models.Dashboards;

/// <summary>Page ouverte (une etape du fil d'Ariane) : ses cartes et les valeurs de parametres avec lesquelles elle a ete ouverte.</summary>
public sealed partial class DashboardPageState : ObservableObject
{
    public DashboardPageDefinition Definition { get; }

    /// <summary>Valeurs des parametres de cette etape, restaurees au retour.</summary>
    public Dictionary<string, string?> ParameterValues { get; }

    public ObservableCollection<DashboardCardState> Cards { get; } = [];

    /// <summary>Le concepteur a renomme la page.</summary>
    public event EventHandler? DefinitionChanged;

    public DashboardPageState(DashboardPageDefinition definition, IReadOnlyDictionary<string, string?> parameterValues)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(parameterValues);

        Definition = definition;
        ParameterValues = new Dictionary<string, string?>(parameterValues, StringComparer.OrdinalIgnoreCase);
        foreach (var card in definition.Cards)
            Cards.Add(new DashboardCardState(card));
    }

    public string Title
    {
        get => Definition.Title;
        set
        {
            if (string.Equals(Definition.Title, value, StringComparison.Ordinal))
                return;

            Definition.Title = value ?? string.Empty;
            OnPropertyChanged();
            DefinitionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Premiere ligne libre sous la derniere carte.</summary>
    public int BottomRow => Cards.Count == 0 ? 0 : Cards.Max(c => c.Row + c.RowSpan);
}
