namespace Querify.Core.Enums.Editors;

/// <summary>Nature d'un caractere du source : les crochets et les paires ne comptent que dans le code.</summary>
public enum SourceRegion : byte
{
    Code = 0,

    LineComment,

    BlockComment,

    /// <summary>Chaine ou caractere ('...', "...", @"...").</summary>
    String
}
