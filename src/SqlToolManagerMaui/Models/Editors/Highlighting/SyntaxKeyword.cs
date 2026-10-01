namespace SqlToolManagerMaui.Models.Editors.Highlighting;

/// <summary>Mot d'une liste &lt;Keywords&gt; du .xshd, reutilise par l'autocompletion.</summary>
/// <param name="Word">Mot tel qu'ecrit dans le fichier.</param>
/// <param name="Category">Nom de la couleur de la liste (Keyword, DataType, Function...).</param>
public sealed record SyntaxKeyword(string Word, string Category);
