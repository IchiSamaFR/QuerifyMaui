using CommunityToolkit.Mvvm.ComponentModel;
using SqlToolManagerMaui.Core.Enums.Dashboards;
using SqlToolManagerMaui.Databases.Enums;
using SqlToolManagerMaui.Databases.Models.Queries;
using System.Runtime.CompilerServices;

namespace SqlToolManagerMaui.Core.Models.Dashboards;

/// <summary>
/// Parametre du dashboard : sa valeur courante (consultation) et sa definition (concepteur,
/// reecrite directement dans le fichier).
/// </summary>
public sealed partial class DashboardParameterValue : ObservableObject
{
    public DashboardParameterDefinition Definition { get; }

    /// <summary>Le concepteur a modifie la definition.</summary>
    public event EventHandler? DefinitionChanged;

    /// <summary>Saisie courante (texte) ; vide = NULL.</summary>
    [ObservableProperty]
    public partial string Value { get; set; }

    /// <summary>Raison du refus de la saisie, null si elle est valide.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? Error { get; set; }

    public bool HasError => Error is not null;

    public string Name
    {
        get => Definition.Name;
        set => Update(Definition.Name, value.Trim().TrimStart('@'), v => Definition.Name = v);
    }

    public DashboardParameterType Type
    {
        get => Definition.Type;
        set => Update(Definition.Type, value, v => Definition.Type = v);
    }

    /// <summary>Libelle du champ de saisie.</summary>
    public string DisplayName => string.IsNullOrWhiteSpace(Definition.Label) ? Definition.Name : Definition.Label;

    public string Hint => Definition.Type switch
    {
        DashboardParameterType.Integer => "Entier",
        DashboardParameterType.Decimal => "Nombre",
        DashboardParameterType.Date => "aaaa-mm-jj",
        DashboardParameterType.DateTime => "aaaa-mm-jj hh:mm",
        DashboardParameterType.Boolean => "0 / 1",
        DashboardParameterType.Guid => "GUID",
        _ => "Texte",
    };

    public DashboardParameterValue(DashboardParameterDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        Definition = definition;
        Value = definition.Default ?? string.Empty;
    }

    /// <summary>Valeur typee pour la requete ; faux (et <see cref="Error"/> renseigne) si la saisie est invalide.</summary>
    public bool TryGetQueryParameter(out QueryParameter parameter)
    {
        var isValid = Definition.TryConvert(Value, out var value, out var error);
        Error = error;
        parameter = new QueryParameter(Definition.Name, ToQueryType(Definition.Type), value);
        return isValid;
    }

    private static QueryParameterType ToQueryType(DashboardParameterType type) => type switch
    {
        DashboardParameterType.Integer => QueryParameterType.Integer,
        DashboardParameterType.Decimal => QueryParameterType.Decimal,
        DashboardParameterType.Date => QueryParameterType.Date,
        DashboardParameterType.DateTime => QueryParameterType.DateTime,
        DashboardParameterType.Boolean => QueryParameterType.Boolean,
        DashboardParameterType.Guid => QueryParameterType.Guid,
        _ => QueryParameterType.Text,
    };

    private void Update<T>(T current, T value, Action<T> apply, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(current, value))
            return;

        apply(value);
        OnPropertyChanged(propertyName);
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(Hint));
        DefinitionChanged?.Invoke(this, EventArgs.Empty);
    }
}
