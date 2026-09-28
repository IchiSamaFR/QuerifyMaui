namespace SqlToolManagerMaui.Models.Queries;

/// <summary>Message produit par l'execution : PRINT, lignes affectees ou erreur.</summary>
public sealed record QueryMessage(string Text, bool IsError = false);
