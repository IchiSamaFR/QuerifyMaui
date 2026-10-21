using Microsoft.CodeAnalysis;
using SqlToolManagerMaui.Core.Models.Editors.Analysis;

namespace SqlToolManagerMaui.Languages.Linq;

/// <summary>Textes du survol et des signatures : types sans espace de noms, mots-cles C# (string, int), comme Visual Studio.</summary>
internal static class LinqSymbolFormatter
{
    private const SymbolDisplayMiscellaneousOptions Miscellaneous =
        SymbolDisplayMiscellaneousOptions.UseSpecialTypes
        | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
        | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier;

    private const SymbolDisplayParameterOptions ParameterOptions =
        SymbolDisplayParameterOptions.IncludeType
        | SymbolDisplayParameterOptions.IncludeName
        | SymbolDisplayParameterOptions.IncludeParamsRefOut
        | SymbolDisplayParameterOptions.IncludeDefaultValue;

    private static readonly SymbolDisplayFormat SymbolFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeType
            | SymbolDisplayMemberOptions.IncludeContainingType
            | SymbolDisplayMemberOptions.IncludeParameters
            | SymbolDisplayMemberOptions.IncludeRef,
        delegateStyle: SymbolDisplayDelegateStyle.NameAndSignature,
        extensionMethodStyle: SymbolDisplayExtensionMethodStyle.InstanceMethod,
        parameterOptions: ParameterOptions,
        propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
        localOptions: SymbolDisplayLocalOptions.IncludeType,
        miscellaneousOptions: Miscellaneous);

    private static readonly SymbolDisplayFormat TypeFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: Miscellaneous);

    private static readonly SymbolDisplayFormat ParameterFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        parameterOptions: ParameterOptions,
        miscellaneousOptions: Miscellaneous);

    /// <summary>Ex. "(propriete) string Student.Name", "(variable locale) IQueryable&lt;Student&gt; students".</summary>
    public static string Describe(ISymbol symbol)
        => $"({KindLabel(symbol)}) {symbol.ToDisplayString(SymbolFormat)}";

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
            ? $"<{string.Join(", ", method.TypeArguments.Select(t => t.ToDisplayString(TypeFormat)))}>"
            : string.Empty;

        var prefix = method.MethodKind == MethodKind.Constructor
            ? $"{method.ContainingType.ToDisplayString(TypeFormat)}("
            : $"{method.ReturnType.ToDisplayString(TypeFormat)} {method.Name}{typeArguments}(";

        return new SignatureInfo(
            prefix,
            method.Parameters.Select(p => p.ToDisplayString(ParameterFormat)).ToArray(),
            ")",
            DeclaredBy(method));
    }

    private static string KindLabel(ISymbol symbol) => symbol switch
    {
        IPropertySymbol => "propriété",
        IFieldSymbol { ContainingType.TypeKind: TypeKind.Enum } => "valeur d'énumération",
        IFieldSymbol { IsConst: true } => "constante",
        IFieldSymbol => "champ",
        ILocalSymbol => "variable locale",
        IParameterSymbol { ContainingSymbol: IMethodSymbol { MethodKind: MethodKind.AnonymousFunction } } => "paramètre de lambda",
        IParameterSymbol => "paramètre",
        IRangeVariableSymbol => "variable de requête",
        IMethodSymbol { MethodKind: MethodKind.Constructor } => "constructeur",
        IMethodSymbol { IsExtensionMethod: true } or IMethodSymbol { ReducedFrom: not null } => "extension",
        IMethodSymbol => "méthode",
        INamedTypeSymbol { TypeKind: TypeKind.Class } => "classe",
        INamedTypeSymbol { TypeKind: TypeKind.Struct } => "structure",
        INamedTypeSymbol { TypeKind: TypeKind.Interface } => "interface",
        INamedTypeSymbol { TypeKind: TypeKind.Enum } => "énumération",
        INamedTypeSymbol { TypeKind: TypeKind.Delegate } => "délégué",
        ITypeSymbol => "type",
        INamespaceSymbol => "espace de noms",
        IEventSymbol => "événement",
        _ => symbol.Kind.ToString(),
    };

    private static string? DeclaredBy(IMethodSymbol method)
    {
        var container = (method.ReducedFrom ?? method).ContainingType;
        return container is null ? null : $"Déclarée par {container.ToDisplayString(TypeFormat)}";
    }

    /// <summary>[Description] pose par le generateur du DbContext sur les entites et leurs proprietes.</summary>
    private static string? DescriptionOf(ISymbol symbol)
        => symbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass is { Name: "DescriptionAttribute" } && a.ConstructorArguments.Length == 1)
            ?.ConstructorArguments[0].Value as string;
}
