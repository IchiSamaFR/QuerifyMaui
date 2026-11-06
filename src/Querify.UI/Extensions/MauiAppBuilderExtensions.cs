using Querify.UI.Inputs.Core;

namespace Querify.UI.Extensions;

/// <summary>Configuration de Querify.UI au démarrage de l'application.</summary>
public static class MauiAppBuilderExtensions
{
    /// <summary>
    /// Enregistre les mappings des contrôles natifs (Picker compact…) avant la création de tout handler :
    /// sans cela, ils ne s'appliquent qu'après l'instanciation d'un premier champ Querify.UI.
    /// </summary>
    public static MauiAppBuilder UseQuerifyInputs(this MauiAppBuilder builder)
    {
        BorderlessHandlers.Init();
        return builder;
    }
}
