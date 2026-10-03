using System.ComponentModel;
using System.Windows.Input;
using SqlToolManagerMaui.Models.Editors;

namespace SqlToolManagerMaui.Views.Controls.Editors;

/// <summary>
/// Un onglet de document : nom, pastille "modifie" qui laisse place au bouton fermer au survol,
/// et indicateur d'accent quand l'onglet est celui du <see cref="FileTabControl"/>.
/// </summary>
public partial class FileTabItem : ContentView
{
    public static readonly BindableProperty DocumentProperty = BindableProperty.Create(
        nameof(Document), typeof(EditorDocument), typeof(FileTabItem), null,
        propertyChanged: OnDocumentChanged);

    public static readonly BindableProperty SelectedDocumentProperty = BindableProperty.Create(
        nameof(SelectedDocument), typeof(EditorDocument), typeof(FileTabItem), null,
        propertyChanged: (b, _, _) => ((FileTabItem)b).Refresh());

    public static readonly BindableProperty CloseCommandProperty = BindableProperty.Create(
        nameof(CloseCommand), typeof(ICommand), typeof(FileTabItem), null);

    public static readonly BindableProperty IsSelectedProperty = BindableProperty.Create(
        nameof(IsSelected), typeof(bool), typeof(FileTabItem), false);

    public static readonly BindableProperty IsPointerOverProperty = BindableProperty.Create(
        nameof(IsPointerOver), typeof(bool), typeof(FileTabItem), false);

    public static readonly BindableProperty ShowDirtyDotProperty = BindableProperty.Create(
        nameof(ShowDirtyDot), typeof(bool), typeof(FileTabItem), false);

    public static readonly BindableProperty ShowCloseButtonProperty = BindableProperty.Create(
        nameof(ShowCloseButton), typeof(bool), typeof(FileTabItem), false);

    public EditorDocument? Document
    {
        get => (EditorDocument?)GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    /// <summary>Document actif du controle parent : l'onglet est selectionne quand il s'agit du sien.</summary>
    public EditorDocument? SelectedDocument
    {
        get => (EditorDocument?)GetValue(SelectedDocumentProperty);
        set => SetValue(SelectedDocumentProperty, value);
    }

    public ICommand? CloseCommand
    {
        get => (ICommand?)GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        private set => SetValue(IsSelectedProperty, value);
    }

    public bool IsPointerOver
    {
        get => (bool)GetValue(IsPointerOverProperty);
        private set => SetValue(IsPointerOverProperty, value);
    }

    public bool ShowDirtyDot
    {
        get => (bool)GetValue(ShowDirtyDotProperty);
        private set => SetValue(ShowDirtyDotProperty, value);
    }

    public bool ShowCloseButton
    {
        get => (bool)GetValue(ShowCloseButtonProperty);
        private set => SetValue(ShowCloseButtonProperty, value);
    }

    /// <summary>Leve quand l'utilisateur clique sur l'onglet (hors bouton fermer).</summary>
    public event EventHandler? Selected;

    public FileTabItem()
    {
        InitializeComponent();
    }

    private static void OnDocumentChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var item = (FileTabItem)bindable;

        if (oldValue is EditorDocument previous)
            previous.PropertyChanged -= item.OnDocumentPropertyChanged;
        if (newValue is EditorDocument current)
            current.PropertyChanged += item.OnDocumentPropertyChanged;

        item.Refresh();
    }

    private void OnDocumentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditorDocument.IsDirty))
            Refresh();
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        IsPointerOver = true;
        Refresh();
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        IsPointerOver = false;
        Refresh();
    }

    private void OnTapped(object? sender, TappedEventArgs e) => Selected?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Pastille "modifie" tant que le pointeur n'est pas sur l'onglet ; bouton fermer au survol
    /// et sur l'onglet actif.
    /// </summary>
    private void Refresh()
    {
        var selected = Document is not null && ReferenceEquals(Document, SelectedDocument);
        var dirty = Document?.IsDirty == true;

        IsSelected = selected;
        ShowDirtyDot = dirty && !IsPointerOver;
        ShowCloseButton = !ShowDirtyDot && (IsPointerOver || selected);
    }
}
