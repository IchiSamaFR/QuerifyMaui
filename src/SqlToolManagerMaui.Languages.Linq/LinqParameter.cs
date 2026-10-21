namespace SqlToolManagerMaui.Languages.Linq;

/// <summary>
/// Variable typee declaree en tete d'une requete (parametre du dashboard) : utilisable sous son nom ou en @Nom.
/// Capturee par la requete, EF l'envoie comme parametre SQL, jamais concatenee au texte.
/// </summary>
/// <param name="Type">Type C# de la variable (int?, string, DateTime?...) ; <paramref name="Value"/> doit lui etre affectable.</param>
public sealed record LinqParameter(string Name, Type Type, object? Value);
