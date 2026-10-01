using CodeEdit.Maui;
using RichEdit.Maui;
using SqlToolManagerMaui.Models.Editors;
using SqlToolManagerMaui.Models.Editors.Highlighting;
using SqlToolManagerMaui.Services.Editors.Completion;
using SqlToolManagerMaui.Services.Editors.Highlighting;
using System.ComponentModel;
using NativeCodeEditor = CodeEdit.Maui.CodeEditor;

namespace SqlToolManagerMaui.Views.Controls.Editors;

/// <summary>
/// Editeur de code de l'application. CodeEdit.Maui.CodeEditor etant scelle, ce controle
/// l'encapsule et lui ajoute ce qui manque : un contenu en simple <see cref="Text"/>,
/// les options d'affichage en un seul objet, la selection (curseur + texte) bindable et la
/// coloration syntaxique.
/// </summary>
public partial class CodeEditor : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(CodeEditor), string.Empty, BindingMode.TwoWay,
        propertyChanged: (bindable, _, newValue) => ((CodeEditor)bindable).OnTextChanged(newValue as string));

    /// <summary>
    /// Texte edite. Seule porte d'entree du contenu : le document CodeEdit reste interne
    /// au controle et suit cette propriete dans les deux sens.
    /// </summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly BindableProperty EditorConfigurationProperty = BindableProperty.Create(
        nameof(EditorConfiguration), typeof(EditorConfiguration), typeof(CodeEditor), null,
        propertyChanged: (bindable, oldValue, newValue) => ((CodeEditor)bindable).OnEditorConfigurationChanged(oldValue as EditorConfiguration, newValue as EditorConfiguration));

    /// <summary>Options d'affichage (numeros de ligne, retour a la ligne, indentation).</summary>
    public EditorConfiguration? EditorConfiguration
    {
        get => (EditorConfiguration?)GetValue(EditorConfigurationProperty);
        set => SetValue(EditorConfigurationProperty, value);
    }

    public static readonly BindableProperty SyntaxConfigurationProperty = BindableProperty.Create(
        nameof(SyntaxConfiguration), typeof(SyntaxConfiguration), typeof(CodeEditor), null,
        propertyChanged: (bindable, _, _) => ((CodeEditor)bindable).OnSyntaxConfigurationChanged());

    /// <summary>Langage colore : prefixe de commentaire et regles de coloration.</summary>
    public SyntaxConfiguration? SyntaxConfiguration
    {
        get => (SyntaxConfiguration?)GetValue(SyntaxConfigurationProperty);
        set => SetValue(SyntaxConfigurationProperty, value);
    }

    public static readonly BindableProperty SelectionProperty = BindableProperty.Create(
        nameof(Selection), typeof(EditorSelection), typeof(CodeEditor), null,
        defaultValueCreator: _ => new EditorSelection(),
        propertyChanged: (bindable, _, _) => ((CodeEditor)bindable).ScheduleSelectionRestore());

    /// <summary>
    /// Curseur et texte selectionne. L'objet appartient a l'hote (ou au controle a defaut) :
    /// le controle met a jour ses proprietes, et restaure la selection qu'il porte quand
    /// l'hote en fournit une autre (changement d'onglet).
    /// </summary>
    public EditorSelection? Selection
    {
        get => (EditorSelection?)GetValue(SelectionProperty);
        set => SetValue(SelectionProperty, value);
    }

    private static readonly BindablePropertyKey CommandsPropertyKey = BindableProperty.CreateReadOnly(
        nameof(Commands), typeof(CodeEdit.Maui.CodeEditorCommands), typeof(CodeEditor), null);

    public static readonly BindableProperty CommandsProperty = CommandsPropertyKey.BindableProperty;

    /// <summary>
    /// Commandes d'edition de l'editeur natif (annuler, copier, coller...), exposees telles
    /// quelles pour le menu Edition.
    /// </summary>
    public CodeEdit.Maui.CodeEditorCommands? Commands
    {
        get => (CodeEdit.Maui.CodeEditorCommands?)GetValue(CommandsProperty);
        private set => SetValue(CommandsPropertyKey, value);
    }

    /// <summary>Editeur natif encapsule, pour les rares appels imperatifs (ScrollIntoView...).</summary>
    public NativeCodeEditor NativeEditor => Native;

    private CodeDocument _document = null!;
    private SyntaxHighlightingController? _highlighting;
    private CompletionController? _completion;

    /// <summary>Evite l'aller-retour Text -> document -> Text pendant une synchronisation.</summary>
    private bool _isSynchronizing;

    /// <summary>
    /// Vrai entre un changement de document / de Selection et la restauration : la selection
    /// native, remise a zero par le changement de document, ne doit ecraser aucun modele.
    /// </summary>
    private bool _isRestoringSelection;

    public CodeEditor()
    {
        InitializeComponent();

        Commands = Native.Commands;
        AttachDocument(CodeDocument.FromPlainText(Text));

        Native.SelectionChanged += OnNativeSelectionChanged;

        // La couche de decorations suit le cycle de vie natif de l'editeur : on la recree
        // a chaque chargement, sinon un retour sur la page laisserait le texte sans couleur.
        Loaded += (_, _) =>
        {
            StartHighlighting();
            StartCompletion();
        };
        Unloaded += (_, _) =>
        {
            StopHighlighting();
            StopCompletion();
        };

#if WINDOWS
        DisableWindowsHoverState();
#endif
    }

#if WINDOWS
    // Brosses mutables : les ressources du template se figent au chargement, on recale leur couleur ensuite.
    private readonly Microsoft.UI.Xaml.Media.SolidColorBrush _hoverBackground = new(Microsoft.UI.Colors.Transparent);
    private readonly Microsoft.UI.Xaml.Media.SolidColorBrush _hoverBorder = new(Microsoft.UI.Colors.Transparent);

    // Le handler de CodeEdit n'est qu'un conteneur : le RichEditBox natif est celui du RichEditor interne.
    private void DisableWindowsHoverState()
    {
        var editor = Native.GetVisualTreeDescendants().OfType<RichEditor>().FirstOrDefault();
        if (editor is null)
            return;

        editor.HandlerChanged += (_, _) => AttachHoverResources(editor);
        AttachHoverResources(editor);
    }

    private void AttachHoverResources(RichEditor editor)
    {
        if (editor.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Control box)
            return;

        // Ressources posees avant l'application du template, sinon ignorees.
        box.Resources["TextControlBackgroundPointerOver"] = _hoverBackground;
        box.Resources["TextControlBorderBrushPointerOver"] = _hoverBorder;
        box.Resources["TextControlBorderThemeThickness"] = new Microsoft.UI.Xaml.Thickness(1,0,0,0);
        box.Resources["TextControlBorderThemeThicknessFocused"] = new Microsoft.UI.Xaml.Thickness(1,0,0,0);
        box.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);

        box.Loaded += (_, _) => SyncHoverBrushes(box);
        box.ActualThemeChanged += (_, _) => SyncHoverBrushes(box);
    }

    private void SyncHoverBrushes(Microsoft.UI.Xaml.Controls.Control box)
    {
        _hoverBackground.Color = (box.Background as Microsoft.UI.Xaml.Media.SolidColorBrush)?.Color ?? Microsoft.UI.Colors.Transparent;
        _hoverBorder.Color = (box.BorderBrush as Microsoft.UI.Xaml.Media.SolidColorBrush)?.Color ?? Microsoft.UI.Colors.Transparent;
    }
#endif

    // ---------- Contenu ----------

    /// <summary>
    /// Le document CodeEdit est remplace plutot que modifie : un texte pousse de l'exterieur
    /// (changement d'onglet, ouverture de fichier) repart d'un historique d'annulation propre.
    /// </summary>
    private void AttachDocument(string text) => AttachDocument(CodeDocument.FromPlainText(text));

    private void AttachDocument(CodeDocument document)
    {
        if (_document is not null)
            _document.Changed -= OnDocumentChanged;

        _document = document;
        _document.Changed += OnDocumentChanged;

        Native.Document = document;
    }

    /// <summary>Frappe de l'utilisateur : le document fait foi, on remonte vers Text.</summary>
    private void OnDocumentChanged(object? sender, RichTextDocumentChangedEventArgs e)
    {
        if (_isSynchronizing)
            return;

        _isSynchronizing = true;
        Text = _document.Text;
        _isSynchronizing = false;
    }

    /// <summary>Texte pousse par l'hote : on recharge le document.</summary>
    private void OnTextChanged(string? value)
    {
        if (_isSynchronizing)
            return;

        var text = value ?? string.Empty;
        if (_document.Text == text)
            return;

        // Le nouveau document remet le curseur a 0 : on reapplique ensuite la selection de l'hote.
        ScheduleSelectionRestore();

        _isSynchronizing = true;
        AttachDocument(text);
        _isSynchronizing = false;
    }

    // ---------- Coloration syntaxique ----------

    private void OnSyntaxConfigurationChanged()
    {
        StopHighlighting();
        StopCompletion();

        if (IsLoaded)
        {
            StartHighlighting();
            StartCompletion();
        }
    }

    private void StartHighlighting()
    {
        if (_highlighting is not null || SyntaxConfiguration is null)
            return;

        // Un controle ne recoit pas ses dependances par constructeur : c'est le cas prevu pour Ioc.
        _highlighting = new SyntaxHighlightingController(Native, Ioc.GetRequiredService<ISyntaxHighlighter>());
        _highlighting.Start(SyntaxConfiguration.Definition);
    }

    private void StopHighlighting()
    {
        _highlighting?.Dispose();
        _highlighting = null;
    }

    // ---------- Autocompletion ----------

    private void StartCompletion()
    {
        if (_completion is not null || SyntaxConfiguration is null)
            return;

        _completion = new CompletionController(Native, CompletionList, this, Ioc.GetRequiredService<ICompletionService>(), SyntaxConfiguration.Language);
    }

    private void StopCompletion()
    {
        _completion?.Dispose();
        _completion = null;
    }

    // ---------- Options d'affichage ----------

    private void OnEditorConfigurationChanged(EditorConfiguration? oldConfiguration, EditorConfiguration? newConfiguration)
    {
        if (oldConfiguration is not null)
            oldConfiguration.PropertyChanged -= OnEditorConfigurationPropertyChanged;

        if (newConfiguration is not null)
            newConfiguration.PropertyChanged += OnEditorConfigurationPropertyChanged;
    }

    private void OnEditorConfigurationPropertyChanged(object? sender, PropertyChangedEventArgs e)
        => RefreshLayout();

    /// <summary>
    /// Quand WordWrap bascule, CodeEdit.Maui reconfigure le RichEditBox natif mais MAUI
    /// conserve sa mesure en cache : l'editeur garderait l'ancienne largeur.
    /// </summary>
    private void RefreshLayout()
    {
        ((IView)Native).InvalidateMeasure();

#if WINDOWS
        if (Native.Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement native)
        {
            native.InvalidateMeasure();
            native.UpdateLayout();
        }
#endif
    }

    // ---------- Selection ----------

    private void OnNativeSelectionChanged(object? sender, RichTextSelectionChangedEventArgs e)
    {
        if (!_isRestoringSelection)
            UpdateSelection();
    }

    /// <summary>
    /// Text et Selection changent tous deux au changement d'onglet, dans un ordre non garanti :
    /// la restauration est differee apres les deux mises a jour de binding.
    /// </summary>
    private void ScheduleSelectionRestore()
    {
        if (_isRestoringSelection)
            return;

        _isRestoringSelection = true;
        Dispatcher.Dispatch(RestoreSelection);
    }

    private void RestoreSelection()
    {
        try
        {
            if (Selection is { } selection)
            {
                var length = _document.Text.Length;
                Native.SelectionState = new RichTextSelectionState(
                    Math.Clamp(selection.Anchor, 0, length),
                    Math.Clamp(selection.Active, 0, length));
            }
        }
        finally
        {
            _isRestoringSelection = false;
        }

        // Recale ligne / colonne / texte sur l'etat reellement applique (offsets bornes).
        UpdateSelection();
    }

    private void UpdateSelection()
    {
        if (Selection is not { } selection)
            return;

        var state = Native.SelectionState;
        // CaretPosition est en base 1 (ligne / colonne UTF-16).
        var position = Native.CaretPosition;

        selection.Anchor = state.Anchor;
        selection.Active = state.Active;
        selection.CaretLine = position.Line;
        selection.CaretColumn = position.Column;
        selection.SelectedText = Native.Selection.Text ?? string.Empty;
    }
}
