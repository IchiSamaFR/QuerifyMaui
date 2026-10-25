using SqlToolManagerMaui.Core.Enums.Queries;
using SqlToolManagerMaui.Core.Models.Queries;
using System.Globalization;

namespace SqlToolManagerMaui.Converters;

/// <summary>
/// Messages d'execution en un seul texte formate : un seul Label permet de selectionner plusieurs lignes d'un coup.
/// Chaque origine (moteur, erreur, sortie du script) a son style.
/// </summary>
public sealed class QueryMessagesConverter : IValueConverter
{
    public Style? InfoStyle { get; set; }

    public Style? ErrorStyle { get; set; }

    public Style? OutputStyle { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = new FormattedString();
        if (value is not IEnumerable<QueryMessage> messages)
            return text;

        QueryMessage? previous = null;
        foreach (var message in messages)
        {
            // Les lignes de console se suivent comme dans un terminal ; le reste est aere comme avant.
            var separator = previous is null ? string.Empty
                : previous.Kind == QueryMessageKind.Output && message.Kind == QueryMessageKind.Output ? Environment.NewLine
                : Environment.NewLine + Environment.NewLine;

            text.Spans.Add(new Span { Text = separator + message.Text, Style = StyleOf(message.Kind) });
            previous = message;
        }

        return text;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private Style? StyleOf(QueryMessageKind kind) => kind switch
    {
        QueryMessageKind.Error => ErrorStyle,
        QueryMessageKind.Output => OutputStyle,
        _ => InfoStyle,
    };
}
