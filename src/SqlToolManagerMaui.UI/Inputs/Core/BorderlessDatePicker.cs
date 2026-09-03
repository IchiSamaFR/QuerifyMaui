namespace SqlToolManagerMaui.UI.Inputs.Core;

/// <summary>
/// Versions "nues" des inputs natifs (sans underline Android / bordure iOS),
/// </summary>
public class BorderlessDatePicker : DatePicker
{
    static BorderlessDatePicker() => BorderlessHandlers.Init();
}