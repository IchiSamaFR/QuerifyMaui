namespace SqlToolManagerMaui.Core.Models.Editors.Completion;

/// <summary>Origine de l'ouverture de l'autocompletion.</summary>
public enum CompletionTrigger
{
    /// <summary>Frappe d'un caractere d'identifiant.</summary>
    Typing,

    /// <summary>Ctrl+Espace.</summary>
    Explicit,

    /// <summary>Frappe d'un point (alias.colonne, schema.table).</summary>
    MemberAccess
}
