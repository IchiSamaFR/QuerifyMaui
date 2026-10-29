using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Databases.Common;
using Querify.Languages.Commons.Editors;

namespace Querify.Languages.Editors;

/// <inheritdoc cref="IEditorLanguageResolver"/>
[AutoRegister(As = typeof(IEditorLanguageResolver))]
public sealed class EditorLanguageResolver : IEditorLanguageResolver
{
    private readonly KeyedServiceMap<SyntaxLanguage, IEditorLanguage> _languages;

    public EditorLanguageResolver(IEnumerable<IEditorLanguage> languages)
    {
        _languages = new(languages, l => l.Language, "langage d'éditeur");
    }

    public IEditorLanguage? Find(SyntaxLanguage language) => _languages.TryResolve(language, out var found) ? found : null;
}
