using Microsoft.CodeAnalysis;
using SqlToolManagerMaui.Languages.Linq.Modeling;
using System.Reflection;

namespace SqlToolManagerMaui.Languages.Linq.Compilation;

/// <summary>Modele compile et charge, reutilisable par toutes les requetes sur ce schema.</summary>
public sealed class LinqModelAssembly
{
    public required EfGeneratedModel Model { get; init; }

    public required Assembly Assembly { get; init; }

    public required Type ContextType { get; init; }

    /// <summary>Reference Roslyn vers l'image compilee, pour compiler les requetes contre elle.</summary>
    public required MetadataReference Reference { get; init; }
}
