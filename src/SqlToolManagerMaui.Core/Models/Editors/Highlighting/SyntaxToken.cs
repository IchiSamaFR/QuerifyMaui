namespace SqlToolManagerMaui.Core.Models.Editors.Highlighting;

/// <summary>
/// Portion de texte coloree. Les bornes sont en unites UTF-16, comme les ranges de CodeEdit.
/// </summary>
public readonly record struct SyntaxToken(int Start, int Length, string Color);
