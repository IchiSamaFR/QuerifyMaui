namespace SqlToolManagerMaui.Languages.JavaScript;

/// <summary>Script qui ne s'analyse pas (JavaScript ou TypeScript), avec sa position dans le texte de l'onglet.</summary>
public sealed class ScriptSyntaxException : Exception
{
    public ScriptSyntaxException()
    {
    }

    public ScriptSyntaxException(string message)
        : base(message)
    {
    }

    public ScriptSyntaxException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ScriptSyntaxException(string message, int line, int column)
        : base(message)
    {
        Line = line;
        Column = column;
    }

    /// <summary>Base 1 ; 0 si inconnue.</summary>
    public int Line { get; }

    /// <summary>Base 1 ; 0 si inconnue.</summary>
    public int Column { get; }
}
