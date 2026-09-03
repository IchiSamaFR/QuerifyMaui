#if ANDROID
using Android.Content;
using Microsoft.Maui.Platform;
using Plainer.Maui.Controls;
using SqlToolManagerMaui.UI.Extensions;
using AApp = Android.App;
using AColor = Android.Graphics.Color;
using ADraw = Android.Graphics.Drawables;
using AView = Android.Views.View;

namespace SqlToolManagerMaui.UI.Inputs.Core;

public partial class ThemedPickerHandler
{
    protected override void ConnectHandler(MauiPicker platformView)
    {
        base.ConnectHandler(platformView);
        // Remplace le listener de clic de MAUI pour piloter nous-mêmes le dialog.
        platformView.SetOnClickListener(new PickerClickListener(this));
    }

    private void ShowDialog()
    {
        if (VirtualView is null)
            return;

        var count = VirtualView.GetCount();
        var items = new string[count];
        for (var i = 0; i < count; i++)
            items[i] = VirtualView.GetItem(i) ?? string.Empty;

        // Contexte thémé sombre => texte clair lisible, indépendant du thème applicatif / d'UraniumUI.
        var context = new Android.Views.ContextThemeWrapper(Context, Android.Resource.Style.ThemeMaterialDialogAlert);
        var builder = new AApp.AlertDialog.Builder(context);

        var pickerField = (VirtualView as Element)?.FindParent<PickerField>();
        if (pickerField != null && !string.IsNullOrEmpty(pickerField.Title))
            builder.SetTitle(pickerField.Title);
        else
            builder.SetTitle(VirtualView.Title ?? string.Empty);
        builder.SetSingleChoiceItems(items, VirtualView.SelectedIndex, (sender, args) =>
        {
            VirtualView.SelectedIndex = args.Which;
            (sender as AApp.AlertDialog)?.Dismiss();
        });
        builder.SetNegativeButton(Android.Resource.String.Cancel, (sender, args) => { });

        var dialog = builder.Create();
        dialog.Show();

        // Bouton "Annuler" aligné sur la couleur Primary de l'app plutôt que l'accent du thème système.
        dialog.GetButton((int)Android.Content.DialogButtonType.Negative)?.SetTextColor(AColor.ParseColor(PrimaryHex));

        // Fond forcé (arrondi + marges) : insensible au thème.
        var density = Context.Resources?.DisplayMetrics?.Density ?? 1f;
        var shape = new ADraw.GradientDrawable();
        shape.SetColor(AColor.ParseColor(DialogBackgroundHex));
        shape.SetCornerRadius(16 * density);
        dialog.Window?.SetBackgroundDrawable(new ADraw.InsetDrawable(shape, (int)(24 * density)));
    }

    private sealed class PickerClickListener : Java.Lang.Object, AView.IOnClickListener
    {
        private readonly ThemedPickerHandler _handler;
        public PickerClickListener(ThemedPickerHandler handler) => _handler = handler;
        public void OnClick(AView? v) => _handler.ShowDialog();
    }
}
#endif