using CodeEdit.Maui;
using CommunityToolkit.Maui;
using RichEdit.Maui;
using SqlToolManagerMaui.Core.Models.Databases.Connections;
using SqlToolManagerMaui.Core.Models.Editors;
using SqlToolManagerMaui.Core.Models.Editors.Highlighting;
using SqlToolManagerMaui.Services.Editors.Completion;
using SqlToolManagerMaui.Services.Editors.Highlighting;
using SqlToolManagerMaui.UI.Extensions;
using SqlToolManagerMaui.Views.Controls.Editors.Completion;
using System.ComponentModel;
using System.Windows.Input;
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
    /// <summary>
    /// Texte edite. Seule porte d'entree du contenu : le document CodeEdit reste interne
    /// au controle et suit cette propriete dans les deux sens.
    /// </summary>
    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay, PropertyChangedMethodName = nameof(OnTextPropertyChanged))]
    public partial string Text { get; set; } = string.Empty;

    /// <summary>
    /// Chemin du fichier edite.
    /// </summary>
    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
    public partial string FilePath { get; set; } = string.Empty;

    /// <summary>Options d'affichage (numeros de ligne, retour a la ligne, indentation).</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnEditorConfigurationPropertyChanged))]
    public partial EditorConfiguration? EditorConfiguration { get; set; }

    /// <summary>Langage colore : prefixe de commentaire et regles de coloration.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnSyntaxConfigurationPropertyChanged))]
    public partial SyntaxConfiguration? SyntaxConfiguration { get; set; }

    /// <summary>
    /// Curseur et texte selectionne. L'objet appartient a l'hote (ou au controle a defaut) :
    /// le controle met a jour ses proprietes, et restaure la selection qu'il porte quand
    /// l'hote en fournit une autre (changement d'onglet).
    /// </summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnSelectionPropertyChanged))]
    public partial EditorSelection? Selection { get; set; } = new EditorSelection();

    /// <summary>Connexion dont le schema alimente l'autocompletion ; null = mots-cles seulement.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnConnectionPropertyChanged))]
    public partial DatabaseConnectionInfo? Connection { get; set; }

    /// <summary>
    /// Commandes d'edition de l'editeur natif (annuler, copier, coller...), exposees telles
    /// quelles pour le menu Edition.
    /// </summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnCommandsPropertyChanged))]
    public partial CodeEdit.Maui.CodeEditorCommands? Commands { get; private set; }

    /// <summary>Commande Annuler de l'editeur natif.</summary>
    [BindableProperty]
    public partial ICommand? UndoCommand { get; private set; }

    /// <summary>Commande Retablir de l'editeur natif.</summary>
    [BindableProperty]
    public partial ICommand? RedoCommand { get; private set; }

    /// <summary>Commande Tout selectionner de l'editeur natif.</summary>
    [BindableProperty]
    public partial ICommand? SelectAllCommand { get; private set; }

    /// <summary>Commande Copier de l'editeur natif.</summary>
    [BindableProperty]
    public partial ICommand? CopyCommand { get; private set; }

    /// <summary>Commande Couper de l'editeur natif.</summary>
    [BindableProperty]
    public partial ICommand? CutCommand { get; private set; }

    /// <summary>Commande Coller de l'editeur natif.</summary>
    [BindableProperty]
    public partial ICommand? PasteCommand { get; private set; }

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

    /// <summary>Nouvelle connexion, ou connexion qui vient de s'ouvrir : son catalogue est precharge pour l'autocompletion.</summary>
    private static void OnConnectionPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var editor = (CodeEditor)bindable;

        if (oldValue is DatabaseConnectionInfo oldConnection)
            oldConnection.PropertyChanged -= editor.OnConnectionStateChanged;
        if (newValue is DatabaseConnectionInfo newConnection)
            newConnection.PropertyChanged += editor.OnConnectionStateChanged;

        editor._completion?.WarmUp();
    }

    private void OnConnectionStateChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DatabaseConnectionInfo.IsConnected))
            _completion?.WarmUp();
    }

    /// <summary>Repercute les commandes natives sur les proprietes bindables dediees.</summary>
    private static void OnCommandsPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var editor = (CodeEditor)bindable;
        var commands = newValue as CodeEdit.Maui.CodeEditorCommands;

        editor.UndoCommand = commands?.Undo;
        editor.RedoCommand = commands?.Redo;
        editor.SelectAllCommand = commands?.SelectAll;
        editor.CopyCommand = commands?.Copy;
        editor.CutCommand = commands?.Cut;
        editor.PasteCommand = commands?.Paste;
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
        box.Resources["TextControlBorderThemeThickness"] = new Microsoft.UI.Xaml.Thickness(1, 0, 0, 0);
        box.Resources["TextControlBorderThemeThicknessFocused"] = new Microsoft.UI.Xaml.Thickness(1, 0, 0, 0);
        box.ApplyTransparentResources();
        box.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);

        // Le flyout natif s'ouvre meme vide (flash au clic droit) : on le supprime a la source.
        if (box is Microsoft.UI.Xaml.Controls.RichEditBox richBox)
        {
            richBox.ContextFlyout = null;
            richBox.SelectionFlyout = null;
            richBox.ContextRequested += (_, e) => e.Handled = true;
        }

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

    private static void OnTextPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((CodeEditor)bindable).OnTextChanged(newValue as string);

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

    private static void OnSyntaxConfigurationPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((CodeEditor)bindable).OnSyntaxConfigurationChanged();

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

        _completion = new CompletionController(Native, CompletionList, this, Ioc.GetRequiredService<ICompletionService>(), SyntaxConfiguration.Language, () => Connection);
    }

    private void StopCompletion()
    {
        _completion?.Dispose();
        _completion = null;
    }

    // ---------- Options d'affichage ----------

    private static void OnEditorConfigurationPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((CodeEditor)bindable).OnEditorConfigurationChanged(oldValue as EditorConfiguration, newValue as EditorConfiguration);

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

    private static void OnSelectionPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((CodeEditor)bindable).ScheduleSelectionRestore();

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
