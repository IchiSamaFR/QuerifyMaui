namespace SqlToolManagerMaui.UI.Inputs.Core;

/// <summary>
/// Versions "nues" des inputs natifs (sans underline Android / bordure iOS),
/// </summary>
public class BorderlessTimePicker : TimePicker
{
    static BorderlessTimePicker() => BorderlessHandlers.Init();
}