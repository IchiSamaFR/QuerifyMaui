using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Querify.UI.Extensions;

namespace Querify.UI.Inputs.Core;

/// <summary>
/// Versions "nues" des inputs natifs (sans underline Android / bordure iOS),
/// </summary>
internal static class BorderlessHandlers
{
    private static bool _initialized;

    public static void Init()
    {
        if (_initialized)
            return;
        _initialized = true;

        EntryHandler.Mapper.AppendToMapping("RaveOn.Borderless", (handler, view) =>
        {
            if (view is not BorderlessEntry)
                return;

            ApplyWindowsCompactTextBox(handler.PlatformView);
        });

        EntryHandler.Mapper.AppendToMapping(nameof(BorderlessEntry.SelectionHighlightColor), (handler, view) =>
        {
            if (view is not BorderlessEntry entry || entry.SelectionHighlightColor is null)
                return;
        });

        // Android : le curseur (caret) a sa propre drawable, distincte du surlignage de sélection.
        // iOS : pas d'API publique séparée pour le caret, TintColor pilote caret + poignées de sélection.
        EntryHandler.Mapper.AppendToMapping(nameof(BorderlessEntry.CursorColor), (handler, view) =>
        {
            if (view is not BorderlessEntry entry || entry.CursorColor is null)
                return;
        });

        // Caret : CursorColor ne fait que teinter. Pour une Entry cachée il faut
        // supprimer le caret au niveau natif, sinon iOS/Android continuent de le dessiner.
        EntryHandler.Mapper.AppendToMapping(nameof(BorderlessEntry.IsCaretVisible), (handler, view) =>
        {
            if (view is not BorderlessEntry entry)
                return;
        });

        // Menu contextuel : la bulle "Coller" d'Android est une ActionMode flottante qui n'est pas
        // fermée par un Unfocus programmatique — d'où l'affichage résiduel. On l'empêche d'exister.
        EntryHandler.Mapper.AppendToMapping(nameof(BorderlessEntry.IsContextMenuEnabled), (handler, view) =>
        {
            if (view is not BorderlessEntry entry)
                return;
        });

        // Les insets natifs de l'Editor s'ajoutent au padding : sans les neutraliser,
        // la hauteur rendue ne correspond pas à la hauteur de ligne mesurée.
        EditorHandler.Mapper.AppendToMapping("RaveOn.Borderless", (handler, view) =>
        {
            if (view is not BorderlessEditor)
                return;
        });

        EditorHandler.Mapper.AppendToMapping(nameof(BorderlessEditor.CursorColor), (handler, view) =>
        {
            if (view is not BorderlessEditor editor || editor.CursorColor is null)
                return;
        });

        PickerHandler.Mapper.AppendToMapping("RaveOn.Borderless", (handler, view) =>
        {
            if (view is not BorderlessPicker)
                return;

            handler.PlatformView.Margin = new Microsoft.UI.Xaml.Thickness(0);
            handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
            handler.PlatformView.MinHeight = 0;
            handler.PlatformView.Background = null;
        });

        // SelectInput / TextInput : compacts et sans fond ni bordure, le cadre est dessiné par FormField.
        PickerHandler.Mapper.AppendToMapping("Querify.Input", (handler, view) =>
        {
            if (view is not SelectInput)
                return;

            ApplyWindowsCompactComboBox(handler.PlatformView);
            handler.PlatformView.ApplyTransparentResources();
        });

        EntryHandler.Mapper.AppendToMapping("Querify.Input", (handler, view) =>
        {
            if (view is not TextInput)
                return;

            ApplyWindowsCompactTextBox(handler.PlatformView);
            handler.PlatformView.ApplyTransparentResources();
        });

        EntryHandler.Mapper.AppendToMapping(nameof(TextInput.Padding), (handler, view) =>
        {
            if (view is TextInput input)
                handler.PlatformView.Padding = ToWindowsThickness(input.Padding);
        });

        // Le padding est appliqué au ComboBox natif (et non en Margin MAUI) : seul le texte est décalé.
        PickerHandler.Mapper.AppendToMapping(nameof(BorderlessPicker.Padding), (handler, view) =>
        {
            if (view is BorderlessPicker picker)
                handler.PlatformView.Padding = ToWindowsThickness(picker.Padding);
            else if (view is SelectInput input)
                handler.PlatformView.Padding = ToWindowsThickness(input.Padding);
        });

        // Windows : la bordure basse du TextBox en focus utilise l'accent systeme (bleu).
        // On la repeint avec l'AccentColor du champ.
        EntryHandler.Mapper.AppendToMapping(nameof(BorderlessEntry.AccentColor), (handler, view) =>
        {
            if (view is not BorderlessEntry entry || entry.AccentColor is null)
                return;

            ApplyWindowsAccent(handler.PlatformView, entry.AccentColor);
        });

        EditorHandler.Mapper.AppendToMapping(nameof(BorderlessEditor.AccentColor), (handler, view) =>
        {
            if (view is not BorderlessEditor editor || editor.AccentColor is null)
                return;

            ApplyWindowsAccent(handler.PlatformView, editor.AccentColor);
        });

        // Windows : chaque element est enveloppe dans un ListViewItem au style Fluent (fond, pastille, coins arrondis).
        CollectionViewHandler.Mapper.AppendToMapping("RaveOn.Borderless", (handler, view) =>
        {
            if (view is BorderlessCollectionView)
                ApplyWindowsBorderlessItems(handler.PlatformView);
        });
    }

    /// <summary>
    /// Le TextBox WinUI impose une hauteur min. (32) et un padding vertical : sous 32 de haut,
    /// le texte est rogne/decale. On les annule pour laisser le champ piloter la hauteur.
    /// </summary>
    private static void ApplyWindowsCompactTextBox(Microsoft.UI.Xaml.Controls.TextBox textBox)
    {
        textBox.MinHeight = 0;
        textBox.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
        textBox.Padding = new Microsoft.UI.Xaml.Thickness(8, 0, 8, 0);
        textBox.VerticalContentAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center;
        textBox.Resources["TextControlThemeMinHeight"] = 0d;
        textBox.Resources["TextControlThemePadding"] = new Microsoft.UI.Xaml.Thickness(0);
    }

    private static Microsoft.UI.Xaml.Thickness ToWindowsThickness(Thickness t) => new(t.Left, t.Top, t.Right, t.Bottom);

    /// <summary>
    /// Le ComboBox WinUI impose une hauteur min. (32) et un padding de template : sous 32 de haut,
    /// le texte est rogné. On les annule pour que la hauteur du cadre (FormField) s'applique.
    /// </summary>
    private static void ApplyWindowsCompactComboBox(Microsoft.UI.Xaml.Controls.ComboBox comboBox)
    {
        comboBox.MinHeight = 0;
        comboBox.Margin = new Microsoft.UI.Xaml.Thickness(0);
        comboBox.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
        comboBox.VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch;
        comboBox.VerticalContentAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center;
        comboBox.Resources["ComboBoxMinHeight"] = 0d;
        comboBox.Resources["ComboBoxPadding"] = new Microsoft.UI.Xaml.Thickness(0);

        comboBox.Loaded -= OnCompactComboBoxLoaded;
        comboBox.Loaded += OnCompactComboBoxLoaded;
    }

    /// <summary>Le ContentPresenter du template garde une marge verticale propre : retirée une fois le template appliqué.</summary>
    private static void OnCompactComboBoxLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var comboBox = (Microsoft.UI.Xaml.Controls.ComboBox)sender;
        comboBox.Loaded -= OnCompactComboBoxLoaded;

        if (ThemedPickerHandler.FindByName(comboBox, "ContentPresenter") is Microsoft.UI.Xaml.FrameworkElement presenter)
        {
            presenter.Margin = new Microsoft.UI.Xaml.Thickness(8,0,8,0);
            presenter.VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center;
        }
    }

    /// <summary>
    /// Neutralise les ressources du style des ListViewItem. Posees sur la liste : seuls ses elements sont concernes,
    /// et elles sont resolues a la creation des conteneurs (le mapping passe avant).
    /// </summary>
    private static void ApplyWindowsBorderlessItems(Microsoft.UI.Xaml.Controls.ListViewBase list)
    {
        var transparent = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
        string[] backgrounds =
        [
            "ListViewItemBackground",
            "ListViewItemBackgroundPointerOver",
            "ListViewItemBackgroundPressed",
            "ListViewItemBackgroundSelected",
            "ListViewItemBackgroundSelectedPointerOver",
            "ListViewItemBackgroundSelectedPressed",
        ];
        foreach (var key in backgrounds)
            list.Resources[key] = transparent;

        list.Resources["ListViewItemSelectionIndicatorVisualEnabled"] = false;
        list.Resources["ListViewItemCornerRadius"] = new Microsoft.UI.Xaml.CornerRadius(0);

        // Supprime le padding par défaut des conteneurs (ListViewItem).
        var style = new Microsoft.UI.Xaml.Style(typeof(Microsoft.UI.Xaml.Controls.ListViewItem));
        if (Microsoft.UI.Xaml.Application.Current.Resources.TryGetValue("DefaultListViewItemStyle", out var baseStyle)
            && baseStyle is Microsoft.UI.Xaml.Style defaultStyle)
        {
            style.BasedOn = defaultStyle;
        }
        style.Setters.Add(new Microsoft.UI.Xaml.Setter(Microsoft.UI.Xaml.Controls.Control.PaddingProperty, new Microsoft.UI.Xaml.Thickness(0)));
        style.Setters.Add(new Microsoft.UI.Xaml.Setter(Microsoft.UI.Xaml.FrameworkElement.MinHeightProperty, 0d));
        list.ItemContainerStyle = style;
    }

    /// <summary>
    /// Surcharge la ressource WinUI qui pilote la bordure du TextBox en focus (accent systeme par
    /// defaut). Les ressources sont posees sur le controle lui-meme : elles priment sur celles de
    /// l'app sans affecter les autres TextBox.
    /// </summary>
    private static void ApplyWindowsAccent(Microsoft.UI.Xaml.Controls.TextBox textBox, Color accent)
    {
        var brush = new Microsoft.UI.Xaml.Media.SolidColorBrush(accent.ToWindowsColor());
        textBox.Resources["TextControlBorderBrushFocused"] = brush;
        textBox.Resources["TextControlBorderBrushFocusedPointerOver"] = brush;
    }
}
