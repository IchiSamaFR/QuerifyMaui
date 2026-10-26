namespace SqlToolManagerMaui.Languages.Linq.Queries;

/// <summary>Annulation demandee alors que le code de l'utilisateur tourne encore : il termine en arriere-plan.</summary>
public sealed class LinqRunAbandonedException : OperationCanceledException
{
    public LinqRunAbandonedException(CancellationToken cancellationToken)
        : base("Le code LINQ ne peut pas être interrompu : il se termine en arrière-plan et garde sa connexion ouverte jusque-là.", null, cancellationToken)
    {
    }
}
