using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace SqlToolManagerMaui.UI.Inputs.Core;

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
#if ANDROID
            handler.PlatformView.Background = null;
            handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
            handler.PlatformView.SetPadding(0, 0, 0, 0);
#elif IOS || MACCATALYST
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
            handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
#elif WINDOWS
            ApplyWindowsCompactTextBox(handler.PlatformView);
#endif
        });

        EntryHandler.Mapper.AppendToMapping(nameof(BorderlessEntry.SelectionHighlightColor), (handler, view) =>
        {
            if (view is not BorderlessEntry entry || entry.SelectionHighlightColor is null)
                return;
#if ANDROID
            handler.PlatformView.SetHighlightColor(entry.SelectionHighlightColor.ToPlatform());
#elif IOS || MACCATALYST
            handler.PlatformView.TintColor = entry.SelectionHighlightColor.ToPlatform();
#endif
        });

        // Android : le curseur (caret) a sa propre drawable, distincte du surlignage de sélection.
        // iOS : pas d'API publique séparée pour le caret, TintColor pilote caret + poignées de sélection.
        EntryHandler.Mapper.AppendToMapping(nameof(BorderlessEntry.CursorColor), (handler, view) =>
        {
            if (view is not BorderlessEntry entry || entry.CursorColor is null)
                return;
#if ANDROID
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Q)
            {
                var density = handler.PlatformView.Context?.Resources?.DisplayMetrics?.Density ?? 1f;
                var drawable = new Android.Graphics.Drawables.GradientDrawable();
                drawable.SetColor(entry.CursorColor.ToPlatform());
                drawable.SetSize((int)(2 * density), handler.PlatformView.LineHeight);
                handler.PlatformView.TextCursorDrawable = drawable;
            }
#elif IOS || MACCATALYST
            handler.PlatformView.TintColor = entry.CursorColor.ToPlatform();
#endif
        });

        // Caret : CursorColor ne fait que teinter. Pour une Entry cachée (PinInput) il faut
        // supprimer le caret au niveau natif, sinon iOS/Android continuent de le dessiner.
        EntryHandler.Mapper.AppendToMapping(nameof(BorderlessEntry.IsCaretVisible), (handler, view) =>
        {
            if (view is not BorderlessEntry entry)
                return;
#if ANDROID
            handler.PlatformView.SetCursorVisible(entry.IsCaretVisible);
#elif IOS || MACCATALYST
            if (handler.PlatformView is SecureAwareTextField field)
            {
                field.CaretVisible = entry.IsCaretVisible;
                field.SetNeedsLayout();
            }
#endif
        });

        // Menu contextuel : la bulle "Coller" d'Android est une ActionMode flottante qui n'est pas
        // fermée par un Unfocus programmatique — d'où l'affichage résiduel. On l'empêche d'exister.
        EntryHandler.Mapper.AppendToMapping(nameof(BorderlessEntry.IsContextMenuEnabled), (handler, view) =>
        {
            if (view is not BorderlessEntry entry)
                return;
#if ANDROID
            var platform = handler.PlatformView;
            platform.LongClickable = entry.IsContextMenuEnabled;
            platform.CustomInsertionActionModeCallback = entry.IsContextMenuEnabled ? null : (Android.Views.ActionMode.ICallback)DisabledActionModeCallback.Instance;
            platform.CustomSelectionActionModeCallback = entry.IsContextMenuEnabled ? null : (Android.Views.ActionMode.ICallback)DisabledActionModeCallback.Instance;
#elif IOS || MACCATALYST
            if (handler.PlatformView is SecureAwareTextField field)
                field.ContextMenuEnabled = entry.IsContextMenuEnabled;
#endif
        });

        // Les insets natifs de l'Editor s'ajoutent au padding : sans les neutraliser,
        // la hauteur rendue ne correspond pas à la hauteur de ligne mesurée.
        EditorHandler.Mapper.AppendToMapping("RaveOn.Borderless", (handler, view) =>
        {
            if (view is not BorderlessEditor)
                return;
#if ANDROID
            handler.PlatformView.Background = null;
            handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
            handler.PlatformView.SetPadding(0, 0, 0, 0);
            handler.PlatformView.SetIncludeFontPadding(false);
#elif IOS || MACCATALYST
            handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
            handler.PlatformView.TextContainerInset = UIKit.UIEdgeInsets.Zero;
            handler.PlatformView.TextContainer.LineFragmentPadding = 0;
#endif
        });

        EditorHandler.Mapper.AppendToMapping(nameof(BorderlessEditor.CursorColor), (handler, view) =>
        {
            if (view is not BorderlessEditor editor || editor.CursorColor is null)
                return;
#if ANDROID
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Q)
            {
                var density = handler.PlatformView.Context?.Resources?.DisplayMetrics?.Density ?? 1f;
                var drawable = new Android.Graphics.Drawables.GradientDrawable();
                drawable.SetColor(editor.CursorColor.ToPlatform());
                drawable.SetSize((int)(2 * density), handler.PlatformView.LineHeight);
                handler.PlatformView.TextCursorDrawable = drawable;
            }
#elif IOS || MACCATALYST
            handler.PlatformView.TintColor = editor.CursorColor.ToPlatform();
#endif
        });

        DatePickerHandler.Mapper.AppendToMapping("RaveOn.Borderless", (handler, view) =>
        {
            if (view is not BorderlessDatePicker)
                return;
#if ANDROID
            handler.PlatformView.Background = null;
            handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
            handler.PlatformView.SetPadding(0, 0, 0, 0);
#elif IOS || MACCATALYST
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
            handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
#endif
        });

        TimePickerHandler.Mapper.AppendToMapping("RaveOn.Borderless", (handler, view) =>
        {
            if (view is not BorderlessTimePicker)
                return;
#if ANDROID
            handler.PlatformView.Background = null;
            handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
            handler.PlatformView.SetPadding(0, 0, 0, 0);
#elif IOS || MACCATALYST
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
            handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
#endif
        });

        PickerHandler.Mapper.AppendToMapping("RaveOn.Borderless", (handler, view) =>
        {
            if (view is not BorderlessPicker)
                return;
#if ANDROID
            handler.PlatformView.Background = null;
            handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
            handler.PlatformView.SetPadding(0, 0, 0, 0);
            if (handler.PlatformView.LayoutParameters is Android.Views.ViewGroup.MarginLayoutParams lp)
            {
                lp.SetMargins(0, 0, 0, 0);
                handler.PlatformView.LayoutParameters = lp;
            }
#elif IOS || MACCATALYST
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
            handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
            handler.PlatformView.LeftView = null;
            handler.PlatformView.RightView = null;
#elif WINDOWS
            handler.PlatformView.Padding = new Microsoft.UI.Xaml.Thickness(8, 0, 8, 0);
            handler.PlatformView.Margin = new Microsoft.UI.Xaml.Thickness(0);
            handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
            handler.PlatformView.MinHeight = 0;
            handler.PlatformView.Background = null;
#endif
        });

        // Windows : la bordure basse du TextBox en focus utilise l'accent systeme (bleu).
        // On la repeint avec l'AccentColor du champ.
        EntryHandler.Mapper.AppendToMapping(nameof(BorderlessEntry.AccentColor), (handler, view) =>
        {
            if (view is not BorderlessEntry entry || entry.AccentColor is null)
                return;
#if WINDOWS
            ApplyWindowsAccent(handler.PlatformView, entry.AccentColor);
#endif
        });

        EditorHandler.Mapper.AppendToMapping(nameof(BorderlessEditor.AccentColor), (handler, view) =>
        {
            if (view is not BorderlessEditor editor || editor.AccentColor is null)
                return;
#if WINDOWS
            ApplyWindowsAccent(handler.PlatformView, editor.AccentColor);
#endif
        });

        EditorHandler.Mapper.AppendToMapping("RemoveBlueCircle", (handler, view) =>
        {
#if IOS
            if (handler.PlatformView is UIKit.UITextView nativeEditor)
            {
                // Set InputAccessoryView to null or a custom UIView to remove the blue circle
                nativeEditor.InputAccessoryView = null;
            }
#endif
        });
    }

#if WINDOWS
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
#endif
}

#if ANDROID
/// <summary>
/// Refuse toute ActionMode d'édition : ni barre de sélection, ni bulle "Coller".
/// </summary>
internal sealed class DisabledActionModeCallback : Java.Lang.Object, Android.Views.ActionMode.ICallback
{
    public static readonly DisabledActionModeCallback Instance = new();

    public bool OnCreateActionMode(Android.Views.ActionMode? mode, Android.Views.IMenu? menu) => false;

    public bool OnPrepareActionMode(Android.Views.ActionMode? mode, Android.Views.IMenu? menu) => false;

    public bool OnActionItemClicked(Android.Views.ActionMode? mode, Android.Views.IMenuItem? item) => false;

    public void OnDestroyActionMode(Android.Views.ActionMode? mode) { }
}
#endif
