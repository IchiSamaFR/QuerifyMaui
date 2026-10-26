using CommunityToolkit.Maui;
using Querify.Core.Models.Editors;
using Querify.ViewModels.Pages.Documents;

namespace Querify.Views.Pages.Documents;

/// <summary>
/// Onglet de script : en-tete d'execution, editeur et resultats. Le contenu est lie au
/// <see cref="CodeEditorVM"/> singleton ; <see cref="Session"/> lui transmet l'onglet affiche.
/// </summary>
public partial class CodeEditorView : ContentView
{
    private readonly CodeEditorVM _viewModel;

    /// <summary>Session de l'onglet affiche (null quand la vue est masquee).</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnSessionPropertyChanged))]
    public partial CodeEditorSession? Session { get; set; }

    public CodeEditorView()
    {
        InitializeComponent();

        // Vue creee par le DataTemplate de MainPage : pas d'injection par constructeur possible.
        _viewModel = Ioc.GetRequiredService<CodeEditorVM>();
        Body.BindingContext = _viewModel;
    }

    private static void OnSessionPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (CodeEditorView)bindable;
        view._viewModel.Session = newValue as CodeEditorSession;
    }
}
