namespace SqlToolManagerMaui.Core.Models.Editors.Completion;

/// <summary>Ce que la grammaire attend a la position du curseur.</summary>
public enum CompletionContextKind
{
    /// <summary>Commentaire, chaine, variable, alias en cours de declaration : rien a proposer.</summary>
    None,

    /// <summary>Debut d'instruction ou position indeterminee.</summary>
    General,

    /// <summary>Expression : liste SELECT, WHERE, ON, SET, ORDER BY...</summary>
    Column,

    /// <summary>Apres FROM, JOIN, UPDATE, INTO...</summary>
    TableSource,

    /// <summary>Juste apres le nom d'une table : alias, ou suite de la requete (WHERE, JOIN...).</summary>
    AfterTableName,

    /// <summary>Apres EXEC.</summary>
    Procedure,

    /// <summary>Apres USE.</summary>
    Database,

    /// <summary>DECLARE @x |, CAST(x AS |).</summary>
    DataType,

    /// <summary>Apres un qualificatif : alias.|, schema.|</summary>
    MemberAccess
}
