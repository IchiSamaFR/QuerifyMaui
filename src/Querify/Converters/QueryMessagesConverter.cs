using Querify.Core.Enums.Queries;
using Querify.Core.Models.Queries;
using System.Globalization;

namespace Querify.Converters;

/// <summary>
/// Messages d'execution en un seul texte formate : un seul Label permet de selectionner plusieurs lignes d'un coup.
/// Chaque origine (moteur, erreur, sortie du script) a son style ; une sortie Log.* suit son niveau.
/// </summary>
public sealed class QueryMessagesConverter : IValueConverter
{
    public Style? InfoStyle { get; set; }

    public Style? ErrorStyle { get; set; }

    public Style? OutputStyle { get; set; }

    public Style? WarningStyle { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = new FormattedString();
        if (value is not IEnumerable<QueryMessage> messages)
            return text;

        QueryMessage? previous = null;
        foreach (var message in messages)
        {
            // Les sorties du script se suivent comme dans un terminal ; le reste est aere comme avant.
            var separator = previous is null ? string.Empty
                : previous.Kind == QueryMessageKind.Output && message.Kind == QueryMessageKind.Output ? Environment.NewLine
                : Environment.NewLine + Environment.NewLine;

            text.Spans.Add(new Span { Text = separator + message.Text, Style = StyleOf(message) });
            previous = message;
        }

        return text;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private Style? StyleOf(QueryMessage message) => message.Kind switch
    {
        QueryMessageKind.Error => ErrorStyle,
        QueryMessageKind.Output => message.Level switch
        {
            QueryLogLevel.Error or QueryLogLevel.Critical => ErrorStyle,
            QueryLogLevel.Warning => WarningStyle,
            // Trace et Debug en retrait, comme les messages du moteur.
            QueryLogLevel.Trace or QueryLogLevel.Debug => InfoStyle,
            _ => OutputStyle,
        },
        _ => InfoStyle,
    };
}
