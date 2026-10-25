using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace SqlToolManagerMaui.Languages.Linq.Workspaces;

/// <summary>Requete dans son source genere : les positions Roslyn se ramenent au texte de l'onglet par <see cref="QueryStart"/>.</summary>
/// <param name="HasModel">Faux sans DbContext : la variable de la base n'existe pas dans le source.</param>
public sealed record LinqDocument(Document Document, int QueryStart, int QueryLength, bool HasModel)
{
    public int QueryEnd => QueryStart + QueryLength;

    /// <summary>Vrai si <paramref name="span"/> est entierement dans le texte de l'utilisateur.</summary>
    public bool Contains(TextSpan span) => span.Start >= QueryStart && span.End <= QueryEnd;
}
