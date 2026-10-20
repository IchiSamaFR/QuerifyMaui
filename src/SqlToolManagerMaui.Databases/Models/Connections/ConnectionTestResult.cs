namespace SqlToolManagerMaui.Databases.Models.Connections;

/// <summary>Resultat d'un test d'ouverture de connexion.</summary>
/// <param name="Success">Vrai si la connexion a pu etre ouverte.</param>
/// <param name="Message">Message affichable (succes ou erreur du driver).</param>
/// <param name="Elapsed">Duree de la tentative.</param>
public readonly record struct ConnectionTestResult(bool Success, string Message, TimeSpan Elapsed)
{
    public static ConnectionTestResult Ok(TimeSpan elapsed, string? detail = null)
        => new(true, detail ?? "Connexion etablie.", elapsed);

    public static ConnectionTestResult Fail(string message, TimeSpan elapsed = default)
        => new(false, message, elapsed);
}
