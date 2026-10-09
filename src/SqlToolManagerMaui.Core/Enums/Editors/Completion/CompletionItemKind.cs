namespace SqlToolManagerMaui.Core.Enums.Editors.Completion;

/// <summary>Nature d'une suggestion ; l'ordre sert aussi de priorite d'affichage a score egal.</summary>
public enum CompletionItemKind
{
    Column,
    Table,
    View,
    Procedure,
    Schema,
    Database,

    // Membres C# des requetes LINQ
    Property,
    Field,
    Method,
    Variable,
    Type,
    Namespace,

    Function,
    Keyword,
    DataType,
    Snippet,

    /// <summary>Mot deja present dans le document.</summary>
    Text
}
