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
        CompletionItemKind.Schema => FluentIcons.Folder16,
        CompletionItemKind.Database => FluentIcons.Database20,
        CompletionItemKind.Function => FluentIcons.MathFormula16,
        CompletionItemKind.DataType => FluentIcons.NumberSymbol16,
        CompletionItemKind.Snippet => FluentIcons.Braces16,
        _ => FluentIcons.TextFont16,
    };

    /// <summary>Memes teintes que la coloration syntaxique pour reconnaitre la nature d'un coup d'oeil.</summary>
    public Color IconColor => Kind switch
    {
        CompletionItemKind.Column or CompletionItemKind.Schema => Color.FromArgb("#9CDCFE"),
        CompletionItemKind.Table or CompletionItemKind.View or CompletionItemKind.DataType => Color.FromArgb("#4EC9B0"),
        CompletionItemKind.Database => Color.FromArgb("#4FC3F7"),
        CompletionItemKind.Function => Color.FromArgb("#DCDCAA"),
        _ => Color.FromArgb("#569CD6"),
    };
}
