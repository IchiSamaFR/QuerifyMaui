using SqlToolManagerMaui.Core.Enums.Editors;
using SqlToolManagerMaui.Core.Ioc;

namespace SqlToolManagerMaui.Services.Dialogs;

/// <inheritdoc cref="IDialogService"/>
[AutoRegister(As = typeof(IDialogService))]
public sealed class DialogService : IDialogService
{
    private const string SaveLabel = "Enregistrer";
    private const string DiscardLabel = "Ne pas enregistrer";
    private const string CancelLabel = "Annuler";

    public async Task<SaveChangesChoice> AskSaveChangesAsync(string documentName)
    {
        // Sans page pour poser la question, on ne ferme rien : rien n'est perdu.
        if (Shell.Current?.CurrentPage is not { } page)
            return SaveChangesChoice.Cancel;

        var answer = await page.DisplayActionSheetAsync(
            $"Enregistrer les modifications de « {documentName} » ?",
            CancelLabel,
            DiscardLabel,
            SaveLabel);

        return answer switch
        {
            SaveLabel => SaveChangesChoice.Save,
            DiscardLabel => SaveChangesChoice.Discard,
            _ => SaveChangesChoice.Cancel,
        };
    }
}
