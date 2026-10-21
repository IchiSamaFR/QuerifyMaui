using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SqlToolManagerMaui.Core.Enums.Dashboards;
using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Dashboards;
using SqlToolManagerMaui.Core.Models.Editors.Highlighting;
using SqlToolManagerMaui.Core.Serialization;
using SqlToolManagerMaui.Databases.Models.Connections;
using SqlToolManagerMaui.Databases.Models.Queries;
using SqlToolManagerMaui.Databases.Queries;
using SqlToolManagerMaui.Services.Databases;
using SqlToolManagerMaui.Services.Editors.Highlighting;
using SqlToolManagerMaui.Services.Settings;
using SqlToolManagerMaui.UI.Extensions;
using SqlToolManagerMaui.ViewModels.Pages.Shared;
using System.Globalization;

namespace SqlToolManagerMaui.ViewModels.Pages.Documents;

/// <summary>Partie consultation : navigation entre pages et lecture des cartes.</summary>
public partial class DashboardVM
{
    // ---------- Consultation ----------

    protected override void RefreshCanExecute()
    {
        ApplyParametersCommand.NotifyCanExecuteChanged();
        CancelLoadingCommand.NotifyCanExecuteChanged();
    }

    private bool CanApplyParameters() => Connection.IsConnected && Session?.Current is not null;

    private bool CanCancelLoading() => Connection.IsConnected && Session?.IsLoading == true;

    [RelayCommand(CanExecute = nameof(CanApplyParameters))]
    private Task ApplyParametersAsync()
    {
        if (Session is not { Current: { } page } session)
            return Task.CompletedTask;

        session.SnapshotValues();
        session.CommitParameterValues();
        return LoadCardsAsync(session, page.Cards);
    }

    /// <summary>Interrompt la lecture des cartes en cours : elles gardent leur dernier resultat.</summary>
    [RelayCommand(CanExecute = nameof(CanCancelLoading))]
    private void CancelLoading() => Session?.CancelLoad();

    [RelayCommand]
    private void Back() => Session?.Back();

    /// <summary>Les etapes du fil d'Ariane gardent leurs resultats : rien n'est relu.</summary>
    [RelayCommand]
    private void GoToStep(DashboardPageState? step)
    {
        if (step is not null)
            Session?.GoTo(step);
    }

    /// <summary>Bouton, ou ligne cliquee : les parametres prennent les valeurs prevues et la page cible s'ouvre.</summary>
    private void OnNavigationRequested(object? sender, DashboardNavigationRequest request)
    {
        // Pas de navigation dans le concepteur : un clic y sert a selectionner.
        if (sender is not DashboardSession { IsDesigning: false, Definition: { } definition } session
            || request.Card.Definition.Navigation is not { } navigation)
        {
            return;
        }

        if (definition.FindPage(navigation.Page) is not { } page)
        {
            Status.Set($"La page « {navigation.Page} » n'existe pas dans ce dashboard.", isError: true);
            return;
        }

        session.SnapshotValues();
        session.ApplyValues(navigation.Resolve(session.CurrentValues(), request.Card.Result, request.Row));
        OpenPage(session, page, asRoot: false);
    }

    private void OpenPage(DashboardSession session, DashboardPageDefinition page, bool asRoot)
    {
        var state = session.Open(page, asRoot);
        LoadCardsAsync(session, state.Cards).FireAndForget("Lecture des cartes du dashboard en échec.");
    }

    // ---------- Lecture des cartes ----------

    /// <summary>Cartes lues en parallele, chacune affichee des qu'elle arrive ; une nouvelle lecture annule la precedente.</summary>
    private async Task LoadCardsAsync(DashboardSession session, IEnumerable<DashboardCardState> cards)
    {
        var queried = cards.Where(c => c.UsesQuery).ToList();
        if (queried.Count == 0)
            return;

        var token = session.BeginLoad();
        try
        {
            await LoadQueriedCardsAsync(session, queried, token);
        }
        finally
        {
            session.EndLoad();
        }
    }

    private async Task LoadQueriedCardsAsync(DashboardSession session, List<DashboardCardState> queried, CancellationToken token)
    {
        List<QueryParameter> parameters = [];
        var isValid = true;
        foreach (var parameter in session.Parameters)
        {
            isValid &= parameter.TryGetQueryParameter(out var value);
            parameters.Add(value);
        }

        if (!isValid)
        {
            Status.Set("Paramètres invalides : corrigez la saisie puis appliquez.", isError: true);
            return;
        }

        DatabaseConnectionInfo connection;
        try
        {
            connection = await OpenConnectionAsync(token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (InvalidOperationException ex)
        {
            foreach (var card in queried)
                card.Error = ex.Message;
            Status.Set(ex.Message, isError: true);
            return;
        }

        var database = session.Definition?.Database;
        await Task.WhenAll(queried.Select(card => LoadCardAsync(connection, database, card, parameters, token)));
    }

    private async Task LoadCardAsync(DatabaseConnectionInfo connection, string? database, DashboardCardState card, IReadOnlyList<QueryParameter> parameters, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(card.Definition.Query))
        {
            card.Result = null;
            card.Error = card.IsChart || card.IsValue || card.IsGrid ? "Aucune requête : renseignez-la dans le concepteur." : null;
            return;
        }

        card.IsLoading = true;
        card.Error = null;
        try
        {
            card.Result = await _executors.Resolve(card.Language)
                .ReadAsync(connection, database, card.Definition.Query, parameters, CARD_MAX_ROWS, token);
        }
        catch (OperationCanceledException)
        {
            // Remplace par une lecture plus recente, ou onglet ferme.
        }
        catch (Exception ex)
        {
            card.Result = null;
            card.Error = ex.Message;
        }
        finally
        {
            card.IsLoading = false;
        }
    }

    private async Task<DatabaseConnectionInfo> OpenConnectionAsync(CancellationToken token)
    {
        var connection = Connection.ActiveConnection
            ?? throw new InvalidOperationException("Choisissez une connexion pour alimenter les cartes.");

        if (!await _connectionStore.OpenConnectionAsync(connection, token))
            throw new InvalidOperationException($"La chaîne de connexion de {connection.Name} est introuvable : réenregistrez-la depuis la page Connexions.");

        return connection;
    }
}
