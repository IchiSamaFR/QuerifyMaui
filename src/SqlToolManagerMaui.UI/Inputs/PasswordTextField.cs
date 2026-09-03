using SqlToolManagerMaui.UI.Attachments;

namespace SqlToolManagerMaui.UI.Inputs;

public class PasswordTextField : TextField
{
    private readonly TextFieldPasswordShowHideAttachment _showHideAttachment = new();

    public PasswordTextField()
        : base()
    {
        IsPassword = true;
        Keyboard = Keyboard.Plain;

        Attachments.Add(_showHideAttachment);
        BindAttachment();
    }

    private void BindAttachment()
    {
        _showHideAttachment.SetBinding(TextFieldPasswordShowHideAttachment.IconColorProperty, new Binding(nameof(TextColor), BindingMode.TwoWay, source: this));
    }
}
