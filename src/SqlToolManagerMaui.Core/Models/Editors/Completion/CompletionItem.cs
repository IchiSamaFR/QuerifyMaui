using MauiIcons.Fluent.Filled;
using SqlToolManagerMaui.Core.Enums.Editors.Completion;

namespace SqlToolManagerMaui.Core.Models.Editors.Completion;

/// <summary>Suggestion proposee par l'autocompletion.</summary>
public sealed class CompletionItem
{
    /// <summary>Texte affiche et filtre.</summary>
    public required string Label { get; init; }

    public required CompletionItemKind Kind { get; init; }

    /// <summary>Texte insere a la validation ; le libelle a defaut.</summary>
    public string? InsertText { get; init; }

    /// <summary>Precision affichee a droite (type d'une colonne, schema d'une table...).</summary>
    public string? Detail { get; init; }

    public string TextToInsert => InsertText ?? Label;

    public Enum Icon => Kind switch
    {
        CompletionItemKind.Column => FluentFilledIcons.Column20Filled,
        CompletionItemKind.Table => FluentFilledIcons.Table16Filled,
        CompletionItemKind.View => FluentFilledIcons.Eye16Filled,
        CompletionItemKind.Procedure => FluentFilledIcons.Code16Filled,
        CompletionItemKind.Schema => FluentFilledIcons.Folder16Filled,
        CompletionItemKind.Database => FluentFilledIcons.Database20Filled,
        CompletionItemKind.Function => FluentFilledIcons.MathFormula16Filled,
        CompletionItemKind.DataType => FluentFilledIcons.NumberSymbol16Filled,
        CompletionItemKind.Snippet or CompletionItemKind.Namespace => FluentFilledIcons.Braces16Filled,
        CompletionItemKind.Property or CompletionItemKind.Field => FluentFilledIcons.Textbox16Filled,
        CompletionItemKind.Method => FluentFilledIcons.Cube16Filled,
        CompletionItemKind.Variable => FluentFilledIcons.DocumentText16Filled,
        CompletionItemKind.Type => FluentFilledIcons.Shapes16Filled,
        CompletionItemKind.Text => FluentFilledIcons.TextT20Filled,
        _ => FluentFilledIcons.TextFont16Filled,
    };

    /// <summary>Memes teintes que la coloration syntaxique pour reconnaitre la nature d'un coup d'oeil.</summary>
    public Color IconColor => Kind switch
    {
        CompletionItemKind.Column or CompletionItemKind.Schema => Color.FromArgb("#9CDCFE"),
        CompletionItemKind.Table or CompletionItemKind.View or CompletionItemKind.DataType => Color.FromArgb("#4EC9B0"),
        CompletionItemKind.Database => Color.FromArgb("#4FC3F7"),
        CompletionItemKind.Function or CompletionItemKind.Procedure => Color.FromArgb("#DCDCAA"),
        CompletionItemKind.Text => Color.FromArgb("#9E9E9E"),
        CompletionItemKind.Property or CompletionItemKind.Field or CompletionItemKind.Variable => Color.FromArgb("#9CDCFE"),
        CompletionItemKind.Method => Color.FromArgb("#B180D7"),
        CompletionItemKind.Type => Color.FromArgb("#4EC9B0"),
        CompletionItemKind.Namespace => Color.FromArgb("#D4D4D4"),
        _ => Color.FromArgb("#569CD6"),
    };
}
