using Querify.Core.Enums.Editors.Completion;
using System.Globalization;

namespace Querify.Converters;

/// <summary>Memes teintes que la coloration syntaxique pour reconnaitre la nature d'un coup d'oeil.</summary>
public sealed class CompletionItemIconColorConverter : IValueConverter
{
    // Couleurs parsees une fois : le convertisseur est rejoue a chaque ligne affichee.
    private static readonly Color Identifier = Color.FromArgb("#9CDCFE");
    private static readonly Color TypeName = Color.FromArgb("#4EC9B0");
    private static readonly Color DatabaseName = Color.FromArgb("#4FC3F7");
    private static readonly Color Callable = Color.FromArgb("#DCDCAA");
    private static readonly Color PlainText = Color.FromArgb("#9E9E9E");
    private static readonly Color MethodName = Color.FromArgb("#B180D7");
    private static readonly Color NamespaceName = Color.FromArgb("#D4D4D4");
    private static readonly Color Keyword = Color.FromArgb("#569CD6");

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not CompletionItemKind kind)
            return null;

        return kind switch
        {
            CompletionItemKind.Column or CompletionItemKind.Schema => Identifier,
            CompletionItemKind.Table or CompletionItemKind.View or CompletionItemKind.DataType => TypeName,
            CompletionItemKind.Database => DatabaseName,
            CompletionItemKind.Function or CompletionItemKind.Procedure => Callable,
            CompletionItemKind.Text => PlainText,
            CompletionItemKind.Property or CompletionItemKind.Field or CompletionItemKind.Variable => Identifier,
            CompletionItemKind.Method => MethodName,
            CompletionItemKind.Type => TypeName,
            CompletionItemKind.Namespace => NamespaceName,
            _ => Keyword,
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
