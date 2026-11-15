namespace Querify.Core.Models.Dashboards;

/// <summary>Page proposee comme cible d'un bouton dans le concepteur : choisie par son libelle, enregistree par son Id.</summary>
public sealed record DashboardPageChoice(string Id, string Title);
