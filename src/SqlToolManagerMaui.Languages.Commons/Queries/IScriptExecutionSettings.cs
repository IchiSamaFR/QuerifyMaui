namespace SqlToolManagerMaui.Languages.Commons.Queries;

/// <summary>Reglages d'execution des langages de script, fournis par l'application (preferences).</summary>
public interface IScriptExecutionSettings
{
    /// <summary>Duree maximale d'un script, appels a la base compris ; <see cref="TimeSpan.Zero"/> = sans limite.</summary>
    TimeSpan ScriptTimeout { get; }
}
