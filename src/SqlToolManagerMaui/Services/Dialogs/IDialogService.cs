using SqlToolManagerMaui.Core.Enums.Editors;

namespace SqlToolManagerMaui.Services.Dialogs;

/// <summary>Questions posees a l'utilisateur depuis un ViewModel, sans qu'il connaisse la page affichee.</summary>
public interface IDialogService
{
    /// <summary>Demande quoi faire des modifications non enregistrees d'un document ; <see cref="SaveChangesChoice.Cancel"/> si aucune page n'est affichee.</summary>
    Task<SaveChangesChoice> AskSaveChangesAsync(string documentName);

    /// <summary>Confirmation d'une action ; faux si l'utilisateur refuse ou si aucune page n'est affichee.</summary>
    Task<bool> ConfirmAsync(string title, string message, string accept);
}
