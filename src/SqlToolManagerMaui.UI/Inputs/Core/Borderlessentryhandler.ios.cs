#if IOS
using CoreGraphics;
using Foundation;
using Microsoft.Maui.Platform;
using ObjCRuntime;
using UIKit;

namespace SqlToolManagerMaui.UI.Inputs.Core;

/// <summary>
/// iOS vide un UITextField en SecureTextEntry quand il redevient first responder :
/// la valeur est réinjectée juste après pour la préserver.
///
/// Porte aussi la suppression du caret et du menu contextuel : sur iOS il n'existe pas
/// de propriété pour ça, il faut surcharger le UITextField.
/// </summary>
internal class SecureAwareTextField : MauiTextField
{
    /// <summary>Quand false, le caret n'est plus dessiné (rect vide) et la sélection n'est plus surlignée.</summary>
    public bool CaretVisible { get; set; } = true;

    /// <summary>Quand false, aucune action d'édition n'est proposée : pas de bulle "Coller".</summary>
    public bool ContextMenuEnabled { get; set; } = true;

    public override bool BecomeFirstResponder()
    {
        var became = base.BecomeFirstResponder();

        if (!became || !SecureTextEntry || string.IsNullOrEmpty(Text))
            return became;

        var text = Text;
        Text = string.Empty;
        // Le caractère en trop puis DeleteBackward évite qu'iOS révèle le dernier caractère.
        InsertText(text + " ");
        DeleteBackward();

        return became;
    }

    public override CGRect GetCaretRectForPosition(UITextPosition position)
        => CaretVisible ? base.GetCaretRectForPosition(position) : CGRect.Empty;

    public override bool CanPerform(Selector action, NSObject? withSender)
        => ContextMenuEnabled && base.CanPerform(action, withSender);
}

public partial class BorderlessEntryHandler
{
    protected override MauiTextField CreatePlatformView() => new SecureAwareTextField
    {
        BorderStyle = UITextBorderStyle.None,
        ClipsToBounds = true,
    };
}
#endif
