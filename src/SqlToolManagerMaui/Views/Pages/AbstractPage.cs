
using SqlToolManagerMaui.ViewModels;

namespace SqlToolManagerMaui.Views.Pages;

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

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        IsPageFocused = true;
        var isPush = args?.NavigationType == NavigationType.Push;
        HandlePageEntered(isPush);
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);

        IsPageFocused = false;
        var isPush = args?.NavigationType == NavigationType.Push;
        HandlePageExited(isPush);
    }

    /// <summary>
    /// Hook pour les pages filles : appelé quand la page devient active.
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