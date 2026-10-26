using Querify.Core.Enums.Editors.Completion;

namespace Querify.Core.Models.Editors.Completion;

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
}
