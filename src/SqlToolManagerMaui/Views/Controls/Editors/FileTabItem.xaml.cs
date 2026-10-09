using CommunityToolkit.Maui;
using SqlToolManagerMaui.Core.Models.Editors;
using System.ComponentModel;
using System.Windows.Input;

namespace SqlToolManagerMaui.Views.Controls.Editors;

/// <summary>
/// Un onglet de document : nom, pastille "modifie" qui laisse place au bouton fermer au survol,
/// et indicateur d'accent quand l'onglet est celui du <see cref="FileTabControl"/>.
/// </summary>
public partial class FileTabItem : ContentView
{
    [BindableProperty(PropertyChangedMethodName = nameof(OnDocumentChanged))]
    public partial EditorDocument? Document { get; set; }

    /// <summary>Document actif du controle parent : l'onglet est selectionne quand il s'agit du sien.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnSelectedDocumentChanged))]
    public partial EditorDocument? SelectedDocument { get; set; }

    [BindableProperty]
    public partial ICommand? CloseCommand { get; set; }

    [BindableProperty]
    public partial bool IsSelected { get; private set; }

    [BindableProperty]
    public partial bool IsPointerOver { get; private set; }

    [BindableProperty]
    public partial bool ShowDirtyDot { get; private set; }

    [BindableProperty]
    public partial bool ShowCloseButton { get; private set; }

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

    private static void OnSelectedDocumentChanged(BindableObject bindable, object oldValue, object newValue)
        => ((FileTabItem)bindable).Refresh();

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
