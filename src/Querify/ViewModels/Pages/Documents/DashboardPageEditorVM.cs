using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Querify.Core.Ioc;
using Querify.Core.Models.Dashboards;
using Querify.UI.ViewModels;

namespace Querify.ViewModels.Pages.Documents;

/// <summary>
/// Popup « Options de la page » (cle et libelle). Elle travaille sur une copie :
/// un abandon ne modifie rien, l'appelant applique la saisie renvoyee.
/// </summary>
[AutoRegister(Lifetime = ServiceLifetime.Transient)]
public partial class DashboardPageEditorVM : AbstractVM
{
    private readonly IPopupService _popupService;
    private IReadOnlyCollection<string> _usedIds = [];

    /// <summary>Cle visee par les boutons de navigation et la page de depart.</summary>
    [ObservableProperty]
    public partial string Id { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? Error { get; private set; }

    public bool HasError => Error is not null;

    public DashboardPageEditorVM(IPopupService popupService)
    {
        _popupService = popupService;
    }

    /// <summary>Prepare le formulaire avant l'ouverture de la popup.</summary>
    public void Initialize(DashboardPageDefinition page, IReadOnlyCollection<string> usedIds)
    {
        ArgumentNullException.ThrowIfNull(page);
        _usedIds = usedIds;
        Id = page.Id;
        Title = page.Title;
        Error = null;
    }

    [RelayCommand]
    private Task CancelAsync() => CloseAsync(null);

    [RelayCommand]
    private Task SaveAsync()
    {
        var id = Id.Trim();
        Error = Validate(id);
        if (Error is not null)
            return Task.CompletedTask;

        var title = string.IsNullOrWhiteSpace(Title) ? id : Title.Trim();
        return CloseAsync(new DashboardPageEdit(id, title));
    }

    private string? Validate(string id)
    {
        if (id.Length == 0)
            return "La clé est obligatoire.";

        if (!id.All(c => char.IsLetterOrDigit(c) || c is '_' or '-'))
            return "La clé ne contient que des lettres, chiffres, _ ou -.";

        // FindPage compare sans tenir compte de la casse.
        if (_usedIds.Contains(id, StringComparer.OrdinalIgnoreCase))
            return $"La page {id} existe déjà.";

        return null;
    }

    private async Task CloseAsync(DashboardPageEdit? result)
    {
        if (Shell.Current is { } shell)
            await _popupService.ClosePopupAsync(shell, result);
    }
}
