namespace SqlToolManagerMaui.UI.Models.Keyboards;

public class KeyboardShortcut
{
    public KeyboardAcceleratorModifiers Modifiers { get; }
    public string Key { get; }

    public KeyboardShortcut(string key)
    {
        Key = key;
    }

    public KeyboardShortcut(KeyboardAcceleratorModifiers modifiers, string key)
        : this(key)
    {
        Modifiers = modifiers;
    }
}
