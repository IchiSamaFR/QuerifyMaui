using SqlToolManagerMaui.UI.ViewModels;

namespace SqlToolManagerMaui.UI.Views;

public abstract partial class AbstractContentView : ContentView
{
    public AbstractVM ContextViewModel { get; }

    protected AbstractContentView(AbstractVM dataContext)
    {
        ContextViewModel = dataContext;
        BindingContext = dataContext;
        InitBindings();

        Loaded += OnContentViewLoaded;
        Unloaded += OnContentViewUnloaded;
    }

    private void OnContentViewLoaded(object? sender, EventArgs e)
    {
        IsViewFocused = true;
        HandleViewEntered();
    }

    private void OnContentViewUnloaded(object? sender, EventArgs e)
    {
        IsViewFocused = false;
        HandleViewExited();
    }

    /// <summary>
    /// Hook pour les vues filles : appelé quand la ContentView devient active (Loaded).
    /// </summary>
    protected virtual void HandleViewEntered()
    {
        ContextViewModel.NavigatedTo(true);
    }

    /// <summary>
    /// Hook pour les vues filles : appelé quand la ContentView n'est plus active (Unloaded).
    /// </summary>
    protected virtual void HandleViewExited()
    {
        ContextViewModel.NavigatedFrom(true);
    }
}