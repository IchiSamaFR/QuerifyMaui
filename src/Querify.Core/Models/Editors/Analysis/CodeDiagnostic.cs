using Querify.Core.Enums.Editors.Analysis;

namespace Querify.Core.Models.Editors.Analysis;

/// <summary>Erreur ou avertissement souligne dans l'editeur.</summary>
/// <param name="Code">Identifiant du compilateur ou du parseur (CS0103, 102...), null si aucun.</param>
public sealed record CodeDiagnostic(int Start, int Length, CodeDiagnosticSeverity Severity, string Message, string? Code = null)
{
    public int End => Start + Length;

    public bool IsError => Severity == CodeDiagnosticSeverity.Error;
}
