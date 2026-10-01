namespace SqlToolManagerMaui.Models.Editors.Completion;

/// <summary>Nature d'une suggestion ; l'ordre sert aussi de priorite d'affichage a score egal.</summary>
public enum CompletionItemKind
{
    Column,
    Table,
    View,
    Schema,
    Database,
    Function,
    Keyword,
    DataType,
    Snippet
}
