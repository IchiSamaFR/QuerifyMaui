
using Querify.UI.ViewModels;

namespace Querify.UI.Views;

public abstract partial class AbstractPage : ContentPage
{
    protected abstract string PageRoute { get; }

    public AbstractVM ContextViewModel { get; }

    protected AbstractPage(AbstractVM dataContext)
    {
        ContextViewModel = dataContext;
        BindingContext = dataContext;
        InitBindings();
    }

    /// <summary>
    /// Vrai apres le premier <see cref="OnAppearing"/>. Au premier affichage, OnNavigatedTo arrive avant que la page
    /// soit dans l'arbre visuel (ressources dynamiques, etats VSM non resolus) : l'entree est faite par OnAppearing.
    /// </summary>
    private bool _isInitialized;

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_isInitialized)
            return;
        _isInitialized = true;

        IsPageFocused = true;
        HandlePageEntered(isPush: true);
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        if (!_isInitialized)
            return;

        IsPageFocused = true;
        var isPush = args?.NavigationType == NavigationType.Push;
        HandlePageEntered(isPush);
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);

        if (!_isInitialized)
            return;

        IsPageFocused = false;
        var isPush = args?.NavigationType == NavigationType.Push;
        HandlePageExited(isPush);
    }

    /// <summary>
    /// Hook pour les pages filles : appelé quand la page devient active (premier OnAppearing, puis chaque OnNavigatedTo).
    /// isPush = true si on arrive via une navigation "avant" (Push), false si on revient en arrière (Pop).
    /// </summary>
    protected virtual void HandlePageEntered(bool isPush)
    {
        ContextViewModel.NavigatedTo(isPush);
    }

    /// <summary>
    /// Hook pour les pages filles : appelé quand la page n'est plus active.
    /// isPush = true si on quitte via une navigation "avant" (Push vers une autre page), false si on revient en arrière (Pop).
    /// </summary>
    protected virtual void HandlePageExited(bool isPush)
    {
        ContextViewModel.NavigatedFrom(isPush);
    }
}
