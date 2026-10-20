using SqlToolManagerMaui.Core.Enums.Settings;
using SqlToolManagerMaui.Core.Models.Dashboards;
using SqlToolManagerMaui.Core.Models.Editors;
using SqlToolManagerMaui.Core.Models.Settings;

namespace SqlToolManagerMaui.ViewModels.Pages;

/// <summary>Partie "espace de travail" : onglets memorises a la fermeture et rouverts au demarrage.</summary>
public partial class MainVM
{
    /// <summary>Faux tant que la restauration n'a pas eu lieu : fermer avant ecraserait les onglets memorises.</summary>
    private bool _isWorkspaceRestored;

    /// <summary>Memorise les onglets ouverts ; synchrone car appele pendant la fermeture de la fenetre.</summary>
    public void SaveWorkspace()
    {
        if (!_isWorkspaceRestored)
            return;

        try
        {
            if (!Preferences.RestoreOpenTabs)
            {
                _workspace.Clear();
                return;
            }

            // Dernieres modifications du concepteur pas encore reecrites dans le texte.
            _dashboard.FlushPendingWrites();

            _workspace.Save(new WorkspaceState
            {
                Tabs = Sessions.Select(ToTab).ToList(),
                UntitledCount = _untitledCount,
                UntitledDashboardCount = _untitledDashboardCount,
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Application en cours de fermeture : plus personne a qui le signaler.
        }
    }

    private static WorkspaceTab ToTab(AbstractSession session)
    {
        var document = session.Document;

        // Un fichier enregistre et non modifie est relu au demarrage : inutile de dupliquer son texte.
        var keepText = !document.HasPath || document.IsDirty;

        return new WorkspaceTab
        {
            Kind = session is DashboardSession ? WorkspaceTabKind.Dashboard : WorkspaceTabKind.CodeEditor,
            FilePath = document.FilePath,
            FallbackName = document.FallbackName,
            Language = document.Language,
            FileText = keepText ? document.FileText : null,
            EditorText = keepText ? document.EditorText : null,
            SelectionAnchor = document.Selection.Anchor,
            SelectionActive = document.Selection.Active,
            DashboardMode = session is DashboardSession dashboard ? dashboard.Mode : default,
        };
    }

    /// <summary>Rouvre les onglets memorises (une seule fois) ; un fichier disparu sans modification en cours est ignore.</summary>
    private async Task RestoreWorkspaceAsync(CancellationToken token)
    {
        if (_isWorkspaceRestored)
            return;

        try
        {
            if (!Preferences.RestoreOpenTabs || await _workspace.LoadAsync(token) is not { } state)
                return;

            _untitledCount = Math.Max(_untitledCount, state.UntitledCount);
            _untitledDashboardCount = Math.Max(_untitledDashboardCount, state.UntitledDashboardCount);

            var skipped = 0;
            for (var i = 0; i < state.Tabs.Count; i++)
            {
                var session = await RestoreTabAsync(state.Tabs[i], token);
                if (session is null)
                {
                    skipped++;
                    continue;
                }

                Sessions.Add(session);
            }

            if (Sessions.Count > 0)
                SelectedSession = Sessions[0];

            if (skipped > 0)
                _status.Set($"{skipped} onglet(s) non restauré(s) : fichier introuvable ou illisible.", isError: true);
        }
        finally
        {
            _isWorkspaceRestored = true;
        }
    }

    private async Task<AbstractSession?> RestoreTabAsync(WorkspaceTab tab, CancellationToken token)
    {
        string fileText;
        string? editorText = tab.EditorText;

        if (editorText is not null)
        {
            fileText = tab.FileText ?? string.Empty;
        }
        else
        {
            if (string.IsNullOrEmpty(tab.FilePath))
                return null;

            try
            {
                var content = await _files.ReadAsync(tab.FilePath, token);
                if (content is null)
                    return null;
                fileText = content.Text;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }

        var document = new EditorDocument(tab.FallbackName, tab.FilePath, fileText, tab.Language);
        if (editorText is not null)
            document.EditorText = editorText;

        var length = document.EditorText.Length;
        document.Selection.Anchor = Math.Clamp(tab.SelectionAnchor, 0, length);
        document.Selection.Active = Math.Clamp(tab.SelectionActive, 0, length);

        return tab.Kind == WorkspaceTabKind.Dashboard
            ? new DashboardSession(document) { Mode = tab.DashboardMode }
            : new CodeEditorSession(document);
    }
}
