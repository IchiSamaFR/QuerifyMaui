using System.ComponentModel;
using SqlToolManagerMaui.Services.Editors.Highlighting;
using SqlToolManagerMaui.ViewModels.Pages;
using SqlToolManagerMaui.Views.Controls.Editors;

namespace SqlToolManagerMaui.Views.Pages;

/// <summary>
/// Editeur de texte / SQL multi-onglets, pose sur le controle CodeEditor de CodeEdit.Maui.
/// Le controle expose son etat par evenements (pas de bindings) : la page relaie donc
/// les editions et la position du curseur vers le <see cref="QueryVM"/>.
/// </summary>
public partial class QueryPage : AbstractPage
{
    public static readonly string PAGE_ROUTE = nameof(QueryPage);

    /// <summary>Nom du fichier .xshd charge pour l'editeur (Resources/Raw/SyntaxHighlights/Sql.xshd).</summary>
    private const string SyntaxLanguage = "Sql";
    protected override string PageRoute => PAGE_ROUTE;

    /// <summary>Acces type au VM, utilise par les bindings des DataTemplate (x:Reference Root).</summary>
    public QueryVM ViewModel { get; }

    /// <summary>
    /// Neutralise le marquage "modifie" pendant le changement d'onglet : remplacer
    /// Document peut declencher ContentChanged alors que l'utilisateur n'a rien tape.
    /// </summary>
    private bool _suppressContentChanged;

    /// <summary>Coloration syntaxique : CodeEdit.Maui n'en fournit aucune, on la publie nous-memes.</summary>
    private readonly SyntaxHighlightingController _highlighting;

    public QueryPage(QueryVM viewModel, ISyntaxDefinitionStore syntaxDefinitions, ISyntaxHighlighter syntaxHighlighter) : base(viewModel)
    {
        ViewModel = viewModel;

        InitializeComponent();

        _highlighting = new SyntaxHighlightingController(Editor, syntaxDefinitions, syntaxHighlighter);
        _highlighting.Start(SyntaxLanguage);

        // Le document ne notifie pas la vue : on marque l'onglet modifie a chaque edition.
        Editor.DocumentChanged += OnEditorDocumentChanged;
        Editor.ContentChanged += OnEditorContentChanged;
        Editor.SelectionChanged += OnEditorSelectionChanged;

        // Les options d'affichage atteignent le controle natif mais pas la mesure MAUI.
        ViewModel.EditorConfiguration.PropertyChanged += OnDocumentConfigurationChanged;
    }

    private void OnDocumentConfigurationChanged(object? sender, PropertyChangedEventArgs e)
        => RefreshEditorLayout();

    /// <summary>
    /// CodeEditor est un ContentView : quand WordWrap bascule, CodeEdit.Maui reconfigure le
    /// RichEditBox natif mais MAUI conserve sa mesure en cache, et l'editeur garde donc
    /// l'ancienne largeur. On force une remesure des deux cotes.
    /// </summary>
    private void RefreshEditorLayout()
    {
        ((IView)Editor).InvalidateMeasure();

#if WINDOWS
        if (Editor.Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement native)
        {
            native.InvalidateMeasure();
            native.UpdateLayout();
        }
#endif
    }

    private void OnEditorDocumentChanged(object? sender, EventArgs e)
    {
        _suppressContentChanged = true;
        Dispatcher.Dispatch(() => _suppressContentChanged = false);
    }

    private void OnEditorContentChanged(object? sender, EventArgs e)
    {
        if (_suppressContentChanged)
            return;

        ViewModel.MarkActiveDocumentDirty();
    }

    private void OnEditorSelectionChanged(object? sender, EventArgs e)
    {
        // CaretPosition est en base 1 (ligne / colonne UTF-16).
        var position = Editor.CaretPosition;
        ViewModel.UpdateCaret(position.Line, position.Column);
    }
}
