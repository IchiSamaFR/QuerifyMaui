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

/// <summary>Partie concepteur : cartes, pages et parametres.</summary>
public partial class DashboardVM
{
    // ---------- Concepteur : cartes ----------

    [RelayCommand]
    private void SelectCard(DashboardCardState? card)
    {
        if (Session is { IsDesigning: true } session)
            session.SelectedCard = card;
    }

    /// <summary>Nouvelle carte de 4 × 2 cases (reduite au bord droit de la grille).</summary>
    [RelayCommand]
    private void AddCard(DashboardCell? cell)
    {
        if (Session is not { IsDesigning: true, Current: not null } session || cell is null)
            return;

        session.AddCard(new DashboardCardDefinition
        {
            Title = "Nouvelle carte",
            Column = cell.Column,
            Row = cell.Row,
            ColumnSpan = Math.Min(4, session.ColumnCount - cell.Column),
            RowSpan = 2,
        });
    }

    [RelayCommand]
    private void StartDrag(DashboardCardState? card)
    {
        if (Session is { IsDesigning: true } session && card is not null)
        {
            session.DraggedCard = card;
            session.SelectedCard = card;
        }
    }

    [RelayCommand]
    private void DragOverCell(DashboardCell? cell)
    {
        if (Session is { IsDesigning: true, IsDraggingCard: true } session)
            session.DropCell = cell;
    }

    /// <summary>Le survol d'une case peut commencer avant la sortie de la precedente : on ne vide que la sienne.</summary>
    [RelayCommand]
    private void DragLeaveCell(DashboardCell? cell)
    {
        if (Session is { } session && session.DropCell == cell)
            session.DropCell = null;
    }

    /// <summary>Fin du glisser, depose ou abandonne hors de la grille.</summary>
    [RelayCommand]
    private void EndDrag()
    {
        if (Session is not { } session)
            return;

        session.DraggedCard = null;
        session.DropCell = null;
    }

    [RelayCommand]
    private void DropCard(DashboardCell? cell)
    {
        if (Session is not { IsDesigning: true, DraggedCard: { } card } session || cell is null)
            return;

        var placement = session.GetDropPlacement(card, cell);
        session.DraggedCard = null;
        session.DropCell = null;
        card.Column = placement.Column;
        card.Row = placement.Row;
    }

    [RelayCommand]
    private void DeleteCard(DashboardCardState? card)
    {
        if (card is not null)
            Session?.RemoveCard(card);
    }

    [RelayCommand]
    private Task RunCardAsync(DashboardCardState? card)
        => Session is { } session && card is not null ? LoadCardsAsync(session, [card]) : Task.CompletedTask;

    // ---------- Concepteur : pages et parametres ----------

    [RelayCommand]
    private void OpenDesignerPage(DashboardPageDefinition? page)
    {
        // Clic sur l'onglet deja ouvert : pas de relecture des cartes.
        if (Session is { } session && page is not null && session.Current?.Definition != page)
            OpenPage(session, page, asRoot: true);
    }

    [RelayCommand]
    private void AddPage()
    {
        if (Session is not { Definition: { } definition } session)
            return;

        var number = definition.Pages.Count + 1;
        while (definition.FindPage($"page{number.ToString(CultureInfo.InvariantCulture)}") is not null)
            number++;

        var page = new DashboardPageDefinition { Id = $"page{number.ToString(CultureInfo.InvariantCulture)}", Title = "Nouvelle page" };
        session.AddPage(page);
        OpenPage(session, page, asRoot: true);
    }

    /// <summary>Supprime une page apres confirmation (la derniere page restante est conservee).</summary>
    [RelayCommand]
    private async Task DeletePageAsync(DashboardPageDefinition? page)
    {
        if (page is null || Session is not { Definition: { } definition } session)
            return;

        if (definition.Pages.Count <= 1)
        {
            Status.Set("Un dashboard garde au moins une page.", isError: true);
            return;
        }

        var name = string.IsNullOrWhiteSpace(page.Title) ? page.Id : page.Title;
        if (!await _dialogs.ConfirmAsync("Supprimer la page", $"Supprimer la page « {name} » et ses cartes ?", "Supprimer"))
            return;

        var wasCurrent = session.Current?.Definition == page;
        session.RemovePage(page);
        if (wasCurrent)
            OpenPage(session, definition.FindPage(definition.StartPage) ?? definition.Pages[0], asRoot: true);
    }

    [RelayCommand]
    private async Task AddParameterAsync()
    {
        if (Session is not { IsDesigning: true, Definition: not null } session)
            return;

        if (await _parameterDialogs.EditAsync(null, GetParameterNames(session, except: null)) is { } parameter)
            session.AddParameter(parameter);
    }

    [RelayCommand]
    private async Task EditParameterAsync(DashboardParameterValue? parameter)
    {
        if (Session is not { IsDesigning: true } session || parameter is null)
            return;

        if (await _parameterDialogs.EditAsync(parameter.Definition, GetParameterNames(session, except: parameter)) is not { } edited)
            return;

        parameter.Name = edited.Name;
        parameter.Label = edited.Label ?? string.Empty;
        parameter.Type = edited.Type;
    }

    [RelayCommand]
    private void DeleteParameter(DashboardParameterValue? parameter)
    {
        if (parameter is not null)
            Session?.RemoveParameter(parameter);
    }

    private static List<string> GetParameterNames(DashboardSession session, DashboardParameterValue? except)
        => session.Parameters.Where(p => p != except).Select(p => p.Name).ToList();

    [RelayCommand]
    private void ShowSettings()
    {
        Preferences.Panels.IsDashboardParamsOpen = !Preferences.Panels.IsDashboardParamsOpen;
    }
}
