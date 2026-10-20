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
    private void DropCard(DashboardCell? cell)
    {
        if (Session is not { IsDesigning: true, DraggedCard: { } card } session || cell is null)
            return;

        session.DraggedCard = null;
        card.Column = Math.Clamp(cell.Column, 0, session.ColumnCount - card.ColumnSpan);
        card.Row = cell.Row;
    }

    [RelayCommand]
    private void MoveCard(string? direction)
    {
        if (Session is not { SelectedCard: { } card } session)
            return;

        switch (direction)
        {
            case "Left":
                card.Column = Math.Max(0, card.Column - 1);
                break;
            case "Right":
                card.Column = Math.Min(session.ColumnCount - card.ColumnSpan, card.Column + 1);
                break;
            case "Up":
                card.Row = Math.Max(0, card.Row - 1);
                break;
            case "Down":
                card.Row++;
                break;
        }
    }

    [RelayCommand]
    private void ResizeCard(string? change)
    {
        if (Session is not { SelectedCard: { } card } session)
            return;

        switch (change)
        {
            case "Wider":
                card.ColumnSpan = Math.Min(session.ColumnCount - card.Column, card.ColumnSpan + 1);
                break;
            case "Narrower":
                card.ColumnSpan = Math.Max(1, card.ColumnSpan - 1);
                break;
            case "Taller":
                card.RowSpan++;
                break;
            case "Shorter":
                card.RowSpan = Math.Max(1, card.RowSpan - 1);
                break;
        }
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
        if (Session is { } session && page is not null)
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

    /// <summary>Supprime la page affichee (la derniere page restante est conservee).</summary>
    [RelayCommand]
    private void DeletePage()
    {
        if (Session is not { Definition: { } definition, Current: { } current } session)
            return;

        if (definition.Pages.Count <= 1)
        {
            Status.Set("Un dashboard garde au moins une page.", isError: true);
            return;
        }

        session.RemovePage(current.Definition);
        OpenPage(session, definition.FindPage(definition.StartPage) ?? definition.Pages[0], asRoot: true);
    }

    [RelayCommand]
    private void AddParameter()
    {
        if (Session is not { Definition: { } definition } session)
            return;

        var number = definition.Parameters.Count + 1;
        while (definition.Parameters.Any(p => string.Equals(p.Name, $"Param{number.ToString(CultureInfo.InvariantCulture)}", StringComparison.OrdinalIgnoreCase)))
            number++;

        session.AddParameter(new DashboardParameterDefinition { Name = $"Param{number.ToString(CultureInfo.InvariantCulture)}" });
    }

    [RelayCommand]
    private void DeleteParameter(DashboardParameterValue? parameter)
    {
        if (parameter is not null)
            Session?.RemoveParameter(parameter);
    }

    [RelayCommand]
    private void ShowSettings()
    {
        Preferences.Panels.IsDashboardParamsOpen = !Preferences.Panels.IsDashboardParamsOpen;
    }
}
