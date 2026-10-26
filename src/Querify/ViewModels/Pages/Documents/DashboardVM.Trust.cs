using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Models.Dashboards;
using System.Runtime.CompilerServices;

namespace Querify.ViewModels.Pages.Documents;

/// <summary>
/// Un .schx execute ses requetes des l'ouverture (SQL, ou C# pour les cartes LINQ) : un fichier recu
/// d'ailleurs ne doit rien lancer sans l'accord de l'utilisateur. L'accord est retenu par chemin.
/// </summary>
public partial class DashboardVM
{
    private const int MaxTrustedDashboards = 200;

    /// <summary>Accord (ou demande en cours, partagee par les lectures concurrentes) par onglet ; libere avec l'onglet.</summary>
    private readonly ConditionalWeakTable<DashboardSession, Task<bool>> _approvals = new();

    private async Task<bool> EnsureApprovedAsync(DashboardSession session)
    {
        if (!_approvals.TryGetValue(session, out var approval))
        {
            approval = AskApprovalAsync(session);
            _approvals.AddOrUpdate(session, approval);
        }

        var approved = await approval;
        if (!approved)
        {
            // Redemande au prochain chargement (bouton Appliquer).
            _approvals.Remove(session);
            return false;
        }

        // Fichier approuve puis enregistre sous un autre nom : le nouveau chemin est retenu.
        if (session.Document.FilePath is { } path)
            Trust(path);

        return true;
    }

    private async Task<bool> AskApprovalAsync(DashboardSession session)
    {
        var document = session.Document;

        // Sans chemin : cree dans l'application (ou onglet sans titre restaure).
        if (document.FilePath is not { } path || IsTrusted(path))
            return true;

        var linqCards = session.Definition?.Pages.SelectMany(p => p.Cards).Count(c => c.Language == SyntaxLanguage.Linq) ?? 0;
        var linqWarning = linqCards > 0
            ? $" Il contient {linqCards} carte(s) LINQ : du code C# exécuté sur cet ordinateur."
            : string.Empty;

        return await _dialogs.ConfirmAsync(
            "Exécuter ce dashboard ?",
            $"« {document.FileName} » va exécuter ses requêtes sur la connexion sélectionnée.{linqWarning}"
                + $"{Environment.NewLine}{Environment.NewLine}N'autorisez que les fichiers que vous avez créés ou dont vous connaissez la provenance.",
            "Autoriser");
    }

    private bool IsTrusted(string path) => Preferences.TrustedDashboards.Any(p => IsSamePath(p, path));

    private void Trust(string path)
    {
        if (!IsTrusted(path))
            Preferences.TrustedDashboards = [path, .. Preferences.TrustedDashboards.Take(MaxTrustedDashboards - 1)];
    }

    private static bool IsSamePath(string left, string right)
        => string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
}
