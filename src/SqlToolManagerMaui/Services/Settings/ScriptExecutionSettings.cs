using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Languages.Commons.Queries;

namespace SqlToolManagerMaui.Services.Settings;

/// <summary>Reglages des scripts lus dans les preferences a chaque execution : un changement de menu vaut tout de suite.</summary>
[AutoRegister(As = typeof(IScriptExecutionSettings))]
public sealed class ScriptExecutionSettings : IScriptExecutionSettings
{
    private readonly IUserPreferencesStore _preferences;

    public ScriptExecutionSettings(IUserPreferencesStore preferences)
    {
        _preferences = preferences;
    }

    public TimeSpan ScriptTimeout => TimeSpan.FromSeconds(Math.Max(0, _preferences.Preferences.ScriptTimeoutSeconds));
}
