using MauiIcons.Fluent;

namespace SqlToolManagerMaui.Models.Editors.Completion;

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

    public FluentIcons Icon => Kind switch
    {
        CompletionItemKind.Column => FluentIcons.Column20,
        CompletionItemKind.Table => FluentIcons.Table16,
        CompletionItemKind.View => FluentIcons.Eye16,
        CompletionItemKind.Procedure => FluentIcons.Code16,
        CompletionItemKind.Schema => FluentIcons.Folder16,
        CompletionItemKind.Database => FluentIcons.Database20,
        CompletionItemKind.Function => FluentIcons.MathFormula16,
        CompletionItemKind.DataType => FluentIcons.NumberSymbol16,
        CompletionItemKind.Snippet or CompletionItemKind.Namespace => FluentIcons.Braces16,
        CompletionItemKind.Property or CompletionItemKind.Field => FluentIcons.Textbox16,
        CompletionItemKind.Method => FluentIcons.Cube16,
        CompletionItemKind.Variable => FluentIcons.DocumentText16,
        CompletionItemKind.Type => FluentIcons.Shapes16,
        CompletionItemKind.Text => FluentIcons.TextT20,
        _ => FluentIcons.TextFont16,
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
