using MauiIcons.Fluent;
using MauiIcons.Fluent.Filled;
using SqlToolManagerMaui.Core.Enums.Editors.Completion;
using SqlToolManagerMaui.UI.Tools;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace SqlToolManagerMaui.Converters;

/// <summary>
/// Glyphe (police Fluent Filled) d'une suggestion d'autocompletion, d'apres son <see cref="CompletionItemKind"/>.
/// Rendu par un simple Label plutot qu'un MauiIcon : les lignes du popup sont recyclees et
/// MauiIcon ne se redessine pas de facon fiable quand son icone change apres coup.
/// </summary>
public sealed class CompletionItemIconConverter : IValueConverter
{
    /// <summary>Alias sous lequel MauiIcons enregistre la police Fluent Filled.</summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not CompletionItemKind kind)
            return null;

        return GlyphIconHelper.Glyph(kind switch
        {
            CompletionItemKind.Column => FluentFilledIcons.Column20Filled,
            CompletionItemKind.Table => FluentFilledIcons.Table16Filled,
            CompletionItemKind.View => FluentFilledIcons.Eye16Filled,
            CompletionItemKind.Procedure => FluentFilledIcons.Code16Filled,
            CompletionItemKind.Schema => FluentFilledIcons.Folder16Filled,
            CompletionItemKind.Database => FluentFilledIcons.Database20Filled,
            CompletionItemKind.Function => FluentFilledIcons.MathFormula16Filled,
            CompletionItemKind.DataType => FluentFilledIcons.NumberSymbol16Filled,
            CompletionItemKind.Snippet or CompletionItemKind.Namespace => FluentFilledIcons.Braces16Filled,
            CompletionItemKind.Property or CompletionItemKind.Field => FluentFilledIcons.Textbox16Filled,
            CompletionItemKind.Method => FluentFilledIcons.Cube16Filled,
            CompletionItemKind.Variable => FluentFilledIcons.DocumentText16Filled,
            CompletionItemKind.Type => FluentFilledIcons.Shapes16Filled,
            CompletionItemKind.Text => FluentFilledIcons.TextT20Filled,
            _ => FluentFilledIcons.TextFont16Filled,
        });
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

}
