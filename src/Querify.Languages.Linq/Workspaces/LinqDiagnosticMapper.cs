using Microsoft.CodeAnalysis;
using Querify.Core.Enums.Editors.Analysis;
using Querify.Core.Models.Editors.Analysis;
using System.Globalization;

namespace Querify.Languages.Linq.Workspaces;

/// <summary>Diagnostics Roslyn ramenes au texte de l'onglet : memes positions pour l'editeur et l'execution.</summary>
internal static class LinqDiagnosticMapper
{
    /// <summary>Au-dela, une erreur en amont fait cascader les suivantes : les premieres suffisent.</summary>
    private const int MaxDiagnostics = 100;

    /// <param name="syntax">Fautes de syntaxe du document : seules celles-la sont rattachees a la fin de la requete.</param>
    public static IReadOnlyList<CodeDiagnostic> ToQuery(IEnumerable<Diagnostic> diagnostics, IReadOnlySet<Diagnostic> syntax, LinqDocument linq, string query)
    {
        List<CodeDiagnostic> result = [];
        foreach (var diagnostic in diagnostics.OrderBy(d => d.Location.SourceSpan.Start))
        {
            if (diagnostic.Severity is not (DiagnosticSeverity.Error or DiagnosticSeverity.Warning) || diagnostic.IsSuppressed || !diagnostic.Location.IsInSource)
                continue;

            var span = diagnostic.Location.SourceSpan;
            var start = span.Start - linq.QueryStart;
            if (start < 0)
                continue;

            if (start > query.Length)
            {
                // ")" ou ";" attendu est signale sur le code genere qui suit la requete : on le rattache a sa fin.
                // Une erreur semantique au-dela de la requete concerne le code genere lui-meme.
                if (!syntax.Contains(diagnostic))
                    continue;

                start = query.Length;
            }

            var (visibleStart, visibleLength) = Widen(query, start, Math.Min(span.Length, query.Length - start));
            if (visibleLength == 0)
                continue;

            result.Add(new CodeDiagnostic(
                visibleStart,
                visibleLength,
                diagnostic.Severity == DiagnosticSeverity.Error ? CodeDiagnosticSeverity.Error : CodeDiagnosticSeverity.Warning,
                diagnostic.GetMessage(CultureInfo.CurrentUICulture),
                diagnostic.Id));

            if (result.Count == MaxDiagnostics)
                break;
        }

        return result;
    }

    /// <summary>Ex. "Ligne 2, colonne 6 : CS1061 ..." pour l'onglet Messages (lignes et colonnes en base 1).</summary>
    public static string Describe(CodeDiagnostic diagnostic, string query)
    {
        var before = query.AsSpan(0, Math.Clamp(diagnostic.Start, 0, query.Length));
        var line = before.Count('\n') + 1;
        var column = before.Length - before.LastIndexOf('\n');

        return $"Ligne {line}, colonne {column} : {diagnostic.Code} {diagnostic.Message}";
    }

    /// <summary>Une erreur sans longueur (symbole manquant) souligne le caractere visible le plus proche.</summary>
    private static (int Start, int Length) Widen(string query, int start, int length)
    {
        if (length > 0)
            return (start, length);

        if (start < query.Length && !char.IsWhiteSpace(query[start]))
            return (start, 1);

        var previous = start - 1;
        while (previous >= 0 && char.IsWhiteSpace(query[previous]))
            previous--;

        return previous >= 0 ? (previous, 1) : (start, 0);
    }
}
