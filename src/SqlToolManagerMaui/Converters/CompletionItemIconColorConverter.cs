using SqlToolManagerMaui.Core.Enums.Editors.Completion;
using System.Globalization;

namespace SqlToolManagerMaui.Converters;

/// <summary>Memes teintes que la coloration syntaxique pour reconnaitre la nature d'un coup d'oeil.</summary>
public sealed class CompletionItemIconColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not CompletionItemKind kind)
            return null;

        return kind switch
        {
            CompletionItemKind.Column or CompletionItemKind.Schema => Color.FromArgb("#9CDCFE"),
            CompletionItemKind.Table or CompletionItemKind.View or CompletionItemKind.DataType => Color.FromArgb("#4EC9B0"),
            CompletionItemKind.Database => Color.FromArgb("#4FC3F7"),
            CompletionItemKind.Function or CompletionItemKind.Procedure => Color.FromArgb("#DCDCAA"),
            CompletionItemKind.Text => Color.FromArgb("#9E9E9E"),
            CompletionItemKind.Property or CompletionItemKind.Field or CompletionItemKind.Variable => Color.FromArgb("#9CDCFE"),
            CompletionItemKind.Method => Color.FromArgb("#B180D7"),
            CompletionItemKind.Type => Color.FromArgb("#4EC9B0"),
            CompletionItemKind.Namespace => Color.FromArgb("#D4D4D4"),
            _ => Color.FromArgb("#569CD6"),
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
