using Querify.Core.Enums.Editors;

namespace Querify.Services.Dialogs;

/// <summary>Questions posees a l'utilisateur depuis un ViewModel, sans qu'il connaisse la page affichee.</summary>
public interface IDialogService
{
    /// <summary>Demande quoi faire des modifications non enregistrees d'un document ; <see cref="SaveChangesChoice.Cancel"/> si aucune page n'est affichee.</summary>
    Task<SaveChangesChoice> AskSaveChangesAsync(string documentName);

    /// <summary>Confirmation d'une action ; faux si l'utilisateur refuse ou si aucune page n'est affichee.</summary>
    Task<bool> ConfirmAsync(string title, string message, string accept);

    /// <summary>Saisie d'un texte ; null si annulee ou si aucune page n'est affichee.</summary>
    /// <param name="validate">Message d'erreur pour une valeur refusee (la popup reste ouverte), null si elle convient.</param>
    Task<string?> PromptAsync(string title, string message, string initialValue, string accept, Func<string, string?>? validate = null);
}
