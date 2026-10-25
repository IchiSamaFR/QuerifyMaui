using Microsoft.CodeAnalysis;
using SqlToolManagerMaui.Core.Models.Editors.Analysis;

namespace SqlToolManagerMaui.Languages.Linq.Analysis;

/// <summary>Textes des signatures et precisions du survol : types sans espace de noms, mots-cles C# (string, int), comme Visual Studio.</summary>
internal static class LinqSymbolFormatter
{
    /// <summary>Format de Visual Studio (noms courts, mots cles C#), nullabilite comprise.</summary>
    private static readonly SymbolDisplayFormat Format = SymbolDisplayFormat.MinimallyQualifiedFormat
        .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    /// <summary>
    /// Description du modele genere (table, colonne SQL) portee par le symbole, son type ou l'element
    /// d'une collection (DbSet&lt;Student&gt;, IQueryable&lt;Student&gt;) ; pour une methode, le type qui la declare.
    /// </summary>
    public static string? Details(ISymbol symbol)
    {
        if (symbol is IMethodSymbol method)
            return DeclaredBy(method);

        var type = symbol switch
        {
            ILocalSymbol local => local.Type,
            IParameterSymbol parameter => parameter.Type,
            IPropertySymbol property => property.Type,
            IFieldSymbol field => field.Type,
            _ => null,
        };

        return DescriptionOf(symbol)
            ?? (type is null ? null : DescriptionOf(type))
            ?? (type is INamedTypeSymbol { IsGenericType: true, TypeArguments: [var element, ..] } ? DescriptionOf(element) : null);
    }

    /// <summary>Surcharge d'une methode : prefixe, parametres un par un, suffixe.</summary>
    public static SignatureInfo ToSignature(IMethodSymbol method)
    {
        var typeArguments = method.IsGenericMethod
            ? $"<{string.Join(", ", method.TypeArguments.Select(t => t.ToDisplayString(Format)))}>"
            : string.Empty;

        var prefix = method.MethodKind == MethodKind.Constructor
            ? $"{method.ContainingType.ToDisplayString(Format)}("
            : $"{method.ReturnType.ToDisplayString(Format)} {method.Name}{typeArguments}(";

        return new SignatureInfo(
            prefix,
            method.Parameters.Select(p => p.ToDisplayString(Format)).ToArray(),
            ")",
            DeclaredBy(method));
    }

    private static string? DeclaredBy(IMethodSymbol method)
    {
        var container = (method.ReducedFrom ?? method).ContainingType;
        return container is null ? null : $"Déclarée par {container.ToDisplayString(Format)}";
    }

    /// <summary>[Description] pose par le generateur du DbContext sur les entites et leurs proprietes.</summary>
    private static string? DescriptionOf(ISymbol symbol)
        => symbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass is { Name: "DescriptionAttribute" } && a.ConstructorArguments.Length == 1)
            ?.ConstructorArguments[0].Value as string;
}
