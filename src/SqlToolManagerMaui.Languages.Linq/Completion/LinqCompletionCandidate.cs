namespace SqlToolManagerMaui.Languages.Linq.Completion;

/// <summary>Suggestion C# brute, independante de l'interface.</summary>
/// <param name="Label">Texte affiche (ex. "Where&lt;&gt;").</param>
/// <param name="InsertText">Texte insere (ex. "Where").</param>
/// <param name="Kind">Nature Roslyn principale : Method, Property, Class, Keyword, Local...</param>
/// <param name="Detail">Precision courte fournie par Roslyn (espace de noms d'un type...).</param>
public sealed record LinqCompletionCandidate(string Label, string InsertText, string Kind, string? Detail);
