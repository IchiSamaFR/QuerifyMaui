using CodeEdit.Maui;
using RichEdit.Maui;
using SqlToolManagerMaui.Models.Editors;
using SqlToolManagerMaui.Models.Editors.Highlighting;
using SqlToolManagerMaui.Services.Editors.Highlighting;
using System.ComponentModel;
using NativeCodeEditor = CodeEdit.Maui.CodeEditor;

namespace SqlToolManagerMaui.Views.Controls.Editors;

/// <summary>
/// Editeur de code de l'application. CodeEdit.Maui.CodeEditor etant scelle, ce controle
/// l'encapsule et lui ajoute ce qui manque : un contenu en simple <see cref="Text"/>,
/// les options d'affichage en un seul objet, la position du curseur bindable et la
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

    public static readonly BindableProperty CaretLineProperty = BindableProperty.Create(
        nameof(CaretLine), typeof(int), typeof(CodeEditor), 1, BindingMode.OneWayToSource);

    /// <summary>Ligne du curseur (base 1), remontee vers l'hote pour la barre d'etat.</summary>
    public int CaretLine
    {
        get => (int)GetValue(CaretLineProperty);
        private set => SetValue(CaretLineProperty, value);
    }

    public static readonly BindableProperty CaretColumnProperty = BindableProperty.Create(
        nameof(CaretColumn), typeof(int), typeof(CodeEditor), 1, BindingMode.OneWayToSource);

    /// <summary>Colonne du curseur (base 1, unites UTF-16).</summary>
    public int CaretColumn
    {
        get => (int)GetValue(CaretColumnProperty);
        private set => SetValue(CaretColumnProperty, value);
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

    /// <summary>Evite l'aller-retour Text -> document -> Text pendant une synchronisation.</summary>
    private bool _isSynchronizing;

    public CodeEditor()
    {
        InitializeComponent();

        Commands = Native.Commands;
        AttachDocument(CodeDocument.FromPlainText(Text));

        Native.SelectionChanged += OnNativeSelectionChanged;

        // La couche de decorations suit le cycle de vie natif de l'editeur : on la recree
        // a chaque chargement, sinon un retour sur la page laisserait le texte sans couleur.
        Loaded += (_, _) => StartHighlighting();
        Unloaded += (_, _) => StopHighlighting();
    }

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

        _isSynchronizing = true;
        AttachDocument(text);
        _isSynchronizing = false;
    }

    // ---------- Coloration syntaxique ----------

    private void OnSyntaxConfigurationChanged()
    {
        StopHighlighting();

        if (IsLoaded)
            StartHighlighting();
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

    // ---------- Curseur ----------

    private void OnNativeSelectionChanged(object? sender, RichTextSelectionChangedEventArgs e)
    {
        // CaretPosition est en base 1 (ligne / colonne UTF-16).
        var position = Native.CaretPosition;

        CaretLine = position.Line;
        CaretColumn = position.Column;
    }
}
