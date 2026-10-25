namespace SqlToolManagerMaui.Languages.Linq.Compilation;

/// <param name="Line">Ligne dans le texte de la requete (base 1).</param>
/// <param name="Column">Colonne dans le texte de la requete (base 1).</param>
public sealed record LinqDiagnostic(int Line, int Column, string Id, string Message)
{
    public override string ToString() => $"Ligne {Line}, colonne {Column} : {Id} {Message}";
}
