using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SqlToolManagerMaui.UI.ViewModels;

public abstract partial class AbstractVM : ObservableObject
{
    public virtual bool IsAlive => IsShellRoot || IsInNavigationStack();
    public virtual bool IsShellRoot => false;

    [ObservableProperty]
    public partial bool IsFocused { get; set; } = false;

    [ObservableProperty]
    public partial bool IsLoadingDatas { get; set; } = false;

    [ObservableProperty]
    public partial bool IsLoadingDatasRequested { get; set; } = true;

    private bool IsInNavigationStack()
    {
        var navigationStack = Shell.Current.Navigation?.NavigationStack;
        if (navigationStack != null && navigationStack.Any(p => p?.BindingContext == this))
        {
            return true;
        }

        var modalStack = Shell.Current.Navigation?.ModalStack;
        if (modalStack != null && modalStack.Any(p => p?.BindingContext == this))
        {
            return true;
        }

        if (Shell.Current.CurrentPage?.BindingContext == this)
        {
            return true;
        }

        return false;
    }

    protected void LoadDatas()
    {
        if (!IsLoadingDatasRequested)
            return;
        IsLoadingDatasRequested = false;

        StartAsBusy(LoadDatasBusyAsync);
    }

    /// <summary>
    /// Chargement des données de la page. Appelé UNIQUEMENT si
    /// <see cref="IsLoadingDatasRequested"/> est vrai (toute navigation en avant le fixe à true).
    /// </summary>
    protected virtual Task LoadDatasBusyAsync() => Task.CompletedTask;

    /// <summary>
    /// Point d'entrée mutualisé de la navigation entrante (Push ET Pop).
    /// Appelé par AbstractPage à chaque apparition de la page.
    /// Exécute le hook UI <see cref="OnEntered"/> puis, uniquement si
    /// <see cref="IsLoadingDatasRequested"/> est vrai, le chargement <see cref="LoadDatasBusyAsync"/>.
    /// </summary>
    public void NavigatedTo(bool isPush)
    {
        OnEntered();

        if (isPush)
            OnEnteredForward();
        else
            OnEnteredBackward();

        LoadDatas();
    }

    /// <summary>
    /// Point de sortie mutualisé de la navigation (Push ET Pop).
    /// Appelé par AbstractPage à chaque disparition de la page.
    /// </summary>
    public void NavigatedFrom(bool isPush)
    {
        OnExited();

        if (isPush)
            OnExitedForward();
        else
            OnExitedBackward();
    }

    /// <summary>
    /// Hook exécuté à CHAQUE navigation entrante (Push ou Pop).
    /// Réservé à l'état/UI non lié au chargement distant : focus des inputs, libellés, etc.
    /// </summary>
    protected virtual void OnEntered()
    {
    }

    protected virtual void OnEnteredForward()
    {
    }

    protected virtual void OnEnteredBackward()
    {

    }

    /// <summary>
    /// Hook exécuté à CHAQUE navigation sortante (Push ou Pop).
    /// </summary>
    protected virtual void OnExited()
    {
    }

    /// <summary>
    /// Hook exécuté quand on quitte la page via une navigation "avant" (Push vers une autre page).
    /// </summary>
    protected virtual void OnExitedForward()
    {
    }

    /// <summary>
    /// Hook exécuté quand on quitte la page via un retour en arrière (Pop).
    /// </summary>
    protected virtual void OnExitedBackward()
    {
    }

    [RelayCommand]
    protected async virtual Task Return()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    protected async virtual Task Action()
    {
        await Task.CompletedTask;
    }
}
