using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SqlToolManagerMaui.Core.Enums.Dashboards;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Dashboards;
using SqlToolManagerMaui.UI.ViewModels;

namespace SqlToolManagerMaui.ViewModels.Pages.Documents;

/// <summary>
/// Popup de declaration d'un parametre (nom, libelle, type). Elle travaille sur une copie :
/// un abandon ne modifie rien, l'appelant applique la definition renvoyee.
/// </summary>
[AutoRegister(Lifetime = ServiceLifetime.Transient)]
public partial class DashboardParameterEditorVM : AbstractVM
{
    private readonly IPopupService _popupService;
    private IReadOnlyCollection<string> _usedNames = [];

    public static DashboardParameterType[] Types { get; } = Enum.GetValues<DashboardParameterType>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    public partial bool IsEditing { get; private set; }

    public string Title => IsEditing ? "Modifier le paramètre" : "Nouveau paramètre";

    /// <summary>Nom utilise dans les requetes, sous la forme @Nom.</summary>
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    /// <summary>Libelle du champ de saisie ; vide = le nom.</summary>
    [ObservableProperty]
    public partial string Label { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DashboardParameterType Type { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? Error { get; private set; }

    public bool HasError => Error is not null;

    public DashboardParameterEditorVM(IPopupService popupService)
    {
        _popupService = popupService;
    }

    /// <summary>Prepare le formulaire avant l'ouverture de la popup.</summary>
    public void Initialize(DashboardParameterDefinition? parameter, IReadOnlyCollection<string> usedNames)
    {
        _usedNames = usedNames;
        IsEditing = parameter is not null;
        Name = parameter?.Name ?? string.Empty;
        Label = parameter?.Label ?? string.Empty;
        Type = parameter?.Type ?? DashboardParameterType.Text;
        Error = null;
    }

    [RelayCommand]
    private Task CancelAsync() => CloseAsync(null);

    [RelayCommand]
    private Task SaveAsync()
    {
        var name = Name.Trim().TrimStart('@');
        Error = Validate(name);
        if (Error is not null)
            return Task.CompletedTask;

        return CloseAsync(new DashboardParameterDefinition
        {
            Name = name,
            Label = string.IsNullOrWhiteSpace(Label) ? null : Label.Trim(),
            Type = Type,
        });
    }

    private string? Validate(string name)
    {
        if (name.Length == 0)
            return "Le nom est obligatoire.";

        // Le nom devient @Nom dans les requetes : un identifiant SQL simple.
        if (!(char.IsLetter(name[0]) || name[0] == '_') || !name.All(c => char.IsLetterOrDigit(c) || c == '_'))
            return "Le nom ne contient que des lettres, chiffres ou _, et ne commence pas par un chiffre.";

        if (_usedNames.Contains(name, StringComparer.OrdinalIgnoreCase))
            return $"Le paramètre @{name} existe déjà.";

        return null;
    }

    private async Task CloseAsync(DashboardParameterDefinition? result)
    {
        if (Shell.Current is { } shell)
            await _popupService.ClosePopupAsync(shell, result);
    }
}
