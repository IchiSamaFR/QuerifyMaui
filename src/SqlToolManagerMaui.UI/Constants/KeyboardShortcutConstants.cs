using SqlToolManagerMaui.UI.Models.Keyboards;
using System;
using System.Collections.Generic;
using System.Text;

namespace SqlToolManagerMaui.UI.Constants;

public static class KeyboardShortcutConstants
{
    // ----- Fichier -----
    public static readonly KeyboardShortcut NewFile = new(KeyboardAcceleratorModifiers.Ctrl, "N");
    public static readonly KeyboardShortcut NewDashboardFile = new(KeyboardAcceleratorModifiers.Ctrl, "D");
    public static readonly KeyboardShortcut OpenFile = new(KeyboardAcceleratorModifiers.Ctrl, "O");
    public static readonly KeyboardShortcut SaveFile = new(KeyboardAcceleratorModifiers.Ctrl, "S");
    public static readonly KeyboardShortcut SaveAsFile = new(KeyboardAcceleratorModifiers.Ctrl | KeyboardAcceleratorModifiers.Shift, "S");
    public static readonly KeyboardShortcut CloseFile = new(KeyboardAcceleratorModifiers.Ctrl, "W");

    // ----- Requêtes -----
    public static readonly KeyboardShortcut ExecuteQuery = new("F5");
}
