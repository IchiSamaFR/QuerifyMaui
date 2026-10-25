namespace SqlToolManagerMaui.Languages.JavaScript;

/// <summary>Garde-fous de l'interpreteur, en plus du delai choisi dans les preferences.</summary>
public static class JavaScriptLimits
{
    /// <summary>Filet contre une boucle infinie quand aucun delai n'est fixe.</summary>
    public const int MaxStatements = 100_000_000;

    /// <summary>Allocations cumulees du fil du script : Jint ne mesure pas la memoire vive.</summary>
    public const long MaxAllocatedBytes = 1L << 30;

    /// <summary>Appels imbriques : au-dela, RangeError plutot qu'un debordement de pile qui fermerait l'application.</summary>
    public const int MaxCallDepth = 1_000;
}
