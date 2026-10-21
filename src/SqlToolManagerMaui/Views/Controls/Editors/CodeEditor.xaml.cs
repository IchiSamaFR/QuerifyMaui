using CodeEdit.Maui;
using CommunityToolkit.Maui;
using RichEdit.Maui;
using SqlToolManagerMaui.Core.Models.Editors;
using SqlToolManagerMaui.Core.Models.Editors.Highlighting;
using SqlToolManagerMaui.Core.Models.Settings;
using SqlToolManagerMaui.Databases.Models.Connections;
using SqlToolManagerMaui.Databases.Models.Queries;
using SqlToolManagerMaui.Services.Editors.Analysis;
using SqlToolManagerMaui.Services.Editors.Completion;
using SqlToolManagerMaui.Services.Editors.Highlighting;
using SqlToolManagerMaui.UI.Extensions;
using SqlToolManagerMaui.Views.Controls.Editors.Analysis;
using SqlToolManagerMaui.Views.Controls.Editors.Completion;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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
    /// Identite du document affiche (EditorDocument d'un onglet, carte d'un dashboard). Le controle etant partage,
    /// il garde par cle son document CodeEdit (historique Annuler / Retablir) et sa selection, retrouves au retour.
    /// Null : chaque texte pousse repart d'un document neuf.
    /// </summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnDocumentKeyPropertyChanged))]
    public partial object? DocumentKey { get; set; }

    /// <summary>
    /// Chemin du fichier edite.
    /// </summary>
    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
    public partial string FilePath { get; set; } = string.Empty;

    /// <summary>Options d'affichage (numeros de ligne, retour a la ligne, indentation).</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnEditorPreferencesPropertyChanged))]
    public partial EditorPreferences? EditorPreferences { get; set; }

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

    /// <summary>Base interrogee par le texte (null = celle de la connexion) : son schema alimente l'autocompletion.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnAnalysisContextPropertyChanged))]
    public partial string? Database { get; set; }

    /// <summary>Parametres fournis a l'execution (dashboard) : proposes par l'autocompletion avant d'etre ecrits.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnAnalysisContextPropertyChanged))]
    public partial IReadOnlyList<QueryParameter>? QueryParameters { get; set; }

    /// <summary>Soulignement des erreurs ; faux pour un texte qui n'est pas un script (ex. "Param = Colonne").</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnIsDiagnosticsEnabledPropertyChanged))]
    public partial bool IsDiagnosticsEnabled { get; set; } = true;

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
    private DiagnosticsController? _diagnostics;
    private HoverController? _hover;
    private SignatureHelpController? _signatureHelp;

    /// <summary>Evite l'aller-retour Text -> document -> Text pendant une synchronisation.</summary>
    private bool _isSynchronizing;

    /// <summary>
    /// Vrai pendant la synchronisation : la selection native, remise a zero par le changement
    /// de document, ne doit ecraser aucun modele.
    /// </summary>
    private bool _isRestoringSelection;

    /// <summary>
    /// Vrai entre un changement de Text / DocumentKey / Selection et la synchronisation : les trois changent
    /// au changement d'onglet, dans un ordre non garanti, et le document affiche est encore l'ancien.
    /// </summary>
    private bool _isSyncPending;

    /// <summary>L'hote a fourni un autre objet Selection : sa position est a appliquer.</summary>
    private bool _isSelectionModelChanged;

    /// <summary>Cle du document actuellement affiche.</summary>
    private object? _attachedKey;

    /// <summary>Etat des documents quittes ; cles faibles : un onglet ferme libere son document.</summary>
    private readonly ConditionalWeakTable<object, DocumentState> _states = new();

    private static readonly RichTextEditOptions ExternalEditOptions = new(RichTextUndoBehavior.CreateUnit, "Modification externe", null);

    public CodeEditor()
    {
        InitializeComponent();

        Commands = Native.Commands;
        AttachDocument(CodeDocument.FromPlainText(Text));

        Native.SelectionChanged += OnNativeSelectionChanged;
        Native.KeyDown += OnNativeKeyDown;

        // La couche de decorations suit le cycle de vie natif de l'editeur : on la recree
        // a chaque chargement, sinon un retour sur la page laisserait le texte sans couleur.
        Loaded += (_, _) =>
        {
            StartHighlighting();
            StartCompletion();
            StartAnalysis();
        };
        Unloaded += (_, _) =>
        {
            StopHighlighting();
            StopCompletion();
            StopAnalysis();
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
        editor._diagnostics?.Refresh();
    }

    private void OnConnectionStateChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(DatabaseConnectionInfo.IsConnected))
            return;

        _completion?.WarmUp();

        // Connexion ouverte : le DbContext ou le schema donnent un sens aux noms, les erreurs sont recalculees.
        _diagnostics?.Refresh();
    }

    private static void OnIsDiagnosticsEnabledPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var editor = (CodeEditor)bindable;
        if (!editor.IsLoaded)
            return;

        editor.StopAnalysis();
        editor.StartAnalysis();
    }

    /// <summary>Autre base ou autres parametres : les noms valides ne sont plus les memes.</summary>
    private static void OnAnalysisContextPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((CodeEditor)bindable)._diagnostics?.Refresh();

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

        ScheduleLineSeparatorFix();
    }

    private static void OnTextPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((CodeEditor)bindable).OnTextChanged(newValue as string);

    /// <summary>Texte pousse par l'hote (changement d'onglet, ouverture, reecriture par le concepteur).</summary>
    private void OnTextChanged(string? value)
    {
        if (_isSynchronizing)
            return;

        if (_document.Text == (value ?? string.Empty) && ReferenceEquals(DocumentKey, _attachedKey))
            return;

        ScheduleSync();
    }

    private static void OnDocumentKeyPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((CodeEditor)bindable).ScheduleSync();

    /// <summary>Differee apres toutes les mises a jour de binding du changement d'onglet.</summary>
    private void ScheduleSync()
    {
        if (_isSyncPending)
            return;

        _isSyncPending = true;
        Dispatcher.Dispatch(Sync);
    }

    /// <summary>
    /// Affiche le document de <see cref="DocumentKey"/> : celui laisse au depart (historique et selection
    /// intacts) s'il existe, sinon un neuf. Un texte change entre-temps est applique comme une edition,
    /// annulable, plutot qu'en remplacant le document.
    /// </summary>
    private void Sync()
    {
        _isSyncPending = false;

        var text = Text ?? string.Empty;
        var key = DocumentKey;
        RichTextSelectionState? selection = null;

        _isRestoringSelection = true;
        _isSynchronizing = true;
        try
        {
            if (!ReferenceEquals(key, _attachedKey))
            {
                if (_attachedKey is not null)
                    _states.AddOrUpdate(_attachedKey, new DocumentState(_document, Native.SelectionState));

                if (key is not null && _states.TryGetValue(key, out var state))
                {
                    AttachDocument(state.Document);
                    selection = state.Selection;

                    // Modifie pendant l'absence (rechargement, concepteur du dashboard).
                    if (state.Document.Text != text)
                        ReplaceText(text);
                }
                else
                {
                    AttachDocument(text);
                    selection = SelectionFromModel();
                }

                _attachedKey = key;
            }
            else if (_document.Text != text)
            {
                if (key is null)
                {
                    AttachDocument(text);
                    selection = SelectionFromModel();
                }
                else
                {
                    ReplaceText(text);
                }
            }
            else if (_isSelectionModelChanged)
            {
                selection = SelectionFromModel();
            }

            _isSelectionModelChanged = false;

            if (selection is { } restored)
            {
                var length = _document.Text.Length;
                Native.SelectionState = new RichTextSelectionState(
                    Math.Clamp(restored.Anchor, 0, length),
                    Math.Clamp(restored.Active, 0, length));

                // Apres la mise en page du document : le curseur retrouve redevient visible.
                var caret = Native.SelectionState.Active;
                Dispatcher.Dispatch(() => Native.ScrollIntoView(new RichTextRange(Math.Min(caret, _document.Text.Length), 0)));
            }
        }
        finally
        {
            _isSynchronizing = false;
            _isRestoringSelection = false;
        }

        // Le modele de selection suit l'etat reellement applique (offsets bornes).
        UpdateSelection();
    }

    private RichTextSelectionState? SelectionFromModel()
        => Selection is { } model ? new RichTextSelectionState(model.Anchor, model.Active) : null;

    /// <summary>Remplace seulement la partie qui differe : l'edition reste annulable et le curseur bouge peu.</summary>
    private void ReplaceText(string text)
    {
        var current = _document.Text;

        var prefix = 0;
        var maxPrefix = Math.Min(current.Length, text.Length);
        while (prefix < maxPrefix && current[prefix] == text[prefix])
            prefix++;

        var suffix = 0;
        var maxSuffix = maxPrefix - prefix;
        while (suffix < maxSuffix && current[^(suffix + 1)] == text[^(suffix + 1)])
            suffix++;

        var removed = new RichTextRange(prefix, current.Length - prefix - suffix);
        var inserted = text[prefix..(text.Length - suffix)];

        var selection = Native.SelectionState;
        var caret = Math.Clamp(selection.Active, 0, text.Length);
        var applied = Native.TryApplyEdits(
            _document.CurrentSnapshot.Revision,
            [new RichTextEdit(removed, inserted)],
            new RichTextSelectionState(Math.Clamp(selection.Anchor, 0, text.Length), caret),
            ExternalEditOptions);

        if (!applied)
            AttachDocument(text);
    }

    /// <summary>Document CodeEdit (et son historique) et selection d'un document quitte.</summary>
    private sealed record DocumentState(CodeDocument Document, RichTextSelectionState Selection);

    // ---------- Coloration syntaxique ----------

    private static void OnSyntaxConfigurationPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((CodeEditor)bindable).OnSyntaxConfigurationChanged();

    private void OnSyntaxConfigurationChanged()
    {
        StopHighlighting();
        StopCompletion();
        StopAnalysis();

        if (IsLoaded)
        {
            StartHighlighting();
            StartCompletion();
            StartAnalysis();
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

        _completion = new CompletionController(Native, CompletionList, this, Ioc.GetRequiredService<ICompletionService>(), SyntaxConfiguration.Language,
            () => Connection, () => Database, () => QueryParameters);
    }

    private void StopCompletion()
    {
        _completion?.Dispose();
        _completion = null;
    }

    // ---------- Erreurs, infobulle et signature ----------

    private void StartAnalysis()
    {
        if (_hover is not null || SyntaxConfiguration is null)
            return;

        var service = Ioc.GetRequiredService<ICodeAnalysisService>();
        var language = SyntaxConfiguration.Language;

        if (IsDiagnosticsEnabled)
            _diagnostics = new DiagnosticsController(Native, service, language, () => Connection, () => Database, () => QueryParameters);

        _hover = new HoverController(Native, Tooltip, this, service, language, () => Connection, () => Database, () => QueryParameters,
            _diagnostics, () => CompletionList.IsVisible);
        _signatureHelp = new SignatureHelpController(Native, SignatureHelp, this, service, language, () => Connection, () => Database, () => QueryParameters,
            () => CompletionList.IsVisible);
    }

    private void StopAnalysis()
    {
        _signatureHelp?.Dispose();
        _signatureHelp = null;
        _hover?.Dispose();
        _hover = null;
        _diagnostics?.Dispose();
        _diagnostics = null;
    }

    // ---------- Options d'affichage ----------

    private static void OnEditorPreferencesPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((CodeEditor)bindable).OnEditorPreferencesChanged(oldValue as EditorPreferences, newValue as EditorPreferences);

    private void OnEditorPreferencesChanged(EditorPreferences? oldConfiguration, EditorPreferences? newConfiguration)
    {
        if (oldConfiguration is not null)
            oldConfiguration.PropertyChanged -= OnEditorPreferencesPropertyChanged;

        if (newConfiguration is not null)
            newConfiguration.PropertyChanged += OnEditorPreferencesPropertyChanged;
    }

    private void OnEditorPreferencesPropertyChanged(object? sender, PropertyChangedEventArgs e)
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

    // ---------- Couper la ligne ----------

    private static readonly RichTextEditOptions CutLineOptions = new(RichTextUndoBehavior.CreateUnit, "Couper la ligne", null);

    /// <summary>Coupe en cours : l'editeur peut relayer deux fois la meme touche, la seconde couperait la ligne suivante.</summary>
    private bool _isCuttingLine;

    /// <summary>Ctrl+X sans selection coupe la ligne entiere, comme Visual Studio.</summary>
    private void OnNativeKeyDown(object? sender, EditorKeyEventArgs e)
    {
        if (e.Handled || e.Key != EditorKey.X || e.Modifiers != EditorKeyModifiers.Control || !Native.SelectionState.IsEmpty)
            return;

        e.Handled = true;
        if (_isCuttingLine)
            return;

        CutLineAsync().FireAndForget("Impossible de couper la ligne.");
    }

    private async Task CutLineAsync()
    {
        _isCuttingLine = true;
        try
        {
            await CutCurrentLineAsync();
        }
        finally
        {
            _isCuttingLine = false;
        }
    }

    private async Task CutCurrentLineAsync()
    {
        var snapshot = _document.CurrentSnapshot;
        var text = snapshot.Text;
        if (text.Length == 0)
            return;

        // \n, \r\n, \r, et les separateurs Unicode que l'editeur affiche aussi comme des retours a la ligne.
        var caret = Math.Clamp(Native.SelectionState.Active, 0, text.Length);
        var lineStart = caret;
        while (lineStart > 0 && !IsLineBreak(text[lineStart - 1]))
            lineStart--;

        var lineEnd = caret;
        while (lineEnd < text.Length && !IsLineBreak(text[lineEnd]))
            lineEnd++;

        // Le presse-papiers recoit toujours la ligne avec son retour : collee, elle reste une ligne a part.
        // Derniere ligne : c'est le retour qui la precede qui part, pour ne pas laisser de ligne vide.
        int removeStart, removeEnd;
        if (lineEnd < text.Length)
        {
            removeStart = lineStart;
            removeEnd = lineEnd + (text[lineEnd] == '\r' && lineEnd + 1 < text.Length && text[lineEnd + 1] == '\n' ? 2 : 1);
        }
        else
        {
            removeEnd = lineEnd;
            removeStart = lineStart == 0 ? 0 : lineStart - (lineStart >= 2 && text[lineStart - 1] == '\n' && text[lineStart - 2] == '\r' ? 2 : 1);
        }

        // Presse-papiers d'abord : s'il echoue, la ligne n'est pas perdue.
        await Clipboard.Default.SetTextAsync(text[lineStart..lineEnd] + Environment.NewLine);

        // Texte modifie pendant l'ecriture du presse-papiers : la plage calculee ne designe plus la ligne
        // (TryApplyEdits la decalerait sur la suivante).
        if (_document != Native.Document || _document.Revision != snapshot.Revision)
            return;

        Native.TryApplyEdits(
            snapshot.Revision,
            [new RichTextEdit(new RichTextRange(removeStart, removeEnd - removeStart), string.Empty)],
            new RichTextSelectionState(removeStart, removeStart),
            CutLineOptions);
    }

    private static bool IsLineBreak(char c) => c is '\n' or '\r' or LineSeparator or ParagraphSeparator;

    // ---------- Separateurs de ligne Unicode ----------

    /// <summary>
    /// U+2028 / U+2029 (Maj+Entree, collage depuis Word ou un navigateur) : affiches comme un retour a la ligne,
    /// mais une seule ligne pour le texte (Ctrl+X, numeros de ligne, erreurs). Remplaces par un vrai retour.
    /// </summary>
    private const char LineSeparator = '\u2028';
    private const char ParagraphSeparator = '\u2029';

    private static readonly RichTextEditOptions LineSeparatorOptions = new(RichTextUndoBehavior.MergeWithPrevious, "Retour à la ligne", null);

    private bool _isLineSeparatorFixScheduled;

    /// <summary>Differe : on ne modifie pas le document pendant qu'il notifie sa propre modification.</summary>
    private void ScheduleLineSeparatorFix()
    {
        if (_isLineSeparatorFixScheduled || _document.Text.AsSpan().IndexOfAny(LineSeparator, ParagraphSeparator) < 0)
            return;

        _isLineSeparatorFixScheduled = true;
        Dispatcher.Dispatch(FixLineSeparators);
    }

    /// <summary>Meme longueur : la selection ne bouge pas, et Ctrl+Z annule la frappe et le remplacement d'un coup.</summary>
    private void FixLineSeparators()
    {
        _isLineSeparatorFixScheduled = false;

        var snapshot = _document.CurrentSnapshot;
        var text = snapshot.Text;

        List<RichTextEdit> edits = [];
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] is LineSeparator or ParagraphSeparator)
                edits.Add(new RichTextEdit(new RichTextRange(i, 1), "\n"));
        }

        if (edits.Count > 0)
            Native.TryApplyEdits(snapshot.Revision, edits, Native.SelectionState, LineSeparatorOptions);
    }

    // ---------- Selection ----------

    private static void OnSelectionPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var editor = (CodeEditor)bindable;
        editor._isSelectionModelChanged = true;
        editor.ScheduleSync();
    }

    /// <summary>Synchronisation en attente : le modele est peut-etre deja celui du document suivant.</summary>
    private void OnNativeSelectionChanged(object? sender, RichTextSelectionChangedEventArgs e)
    {
        if (!_isRestoringSelection && !_isSyncPending)
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
