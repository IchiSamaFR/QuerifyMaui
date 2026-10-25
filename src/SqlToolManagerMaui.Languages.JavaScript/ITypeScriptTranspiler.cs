namespace SqlToolManagerMaui.Languages.JavaScript;

/// <summary>Retrait des types TypeScript, sans verification : les numeros de ligne du script sont conserves.</summary>
public interface ITypeScriptTranspiler
{
    /// <summary>JavaScript equivalent ; leve <see cref="ScriptSyntaxException"/> si le TypeScript ne s'analyse pas.</summary>
    string Transpile(string script);
}
