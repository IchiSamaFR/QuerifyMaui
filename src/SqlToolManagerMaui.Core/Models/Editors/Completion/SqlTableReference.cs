namespace SqlToolManagerMaui.Core.Models.Editors.Completion;

/// <summary>Table ou vue citee dans le lot courant (FROM, JOIN, UPDATE...), avec son alias eventuel.</summary>
/// <param name="Offset">Position du nom dans le texte, pour privilegier la reference la plus proche du curseur.</param>
public sealed record SqlTableReference(string? Database, string? Schema, string Name, string? Alias, int Offset)
{
    /// <summary>Nom sous lequel la requete designe la table : l'alias s'il existe.</summary>
    public string ExposedName => Alias ?? Name;
}
