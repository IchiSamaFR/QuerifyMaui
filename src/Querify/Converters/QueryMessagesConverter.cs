using Quill.Code.Scripting;
using System.Globalization;

namespace Querify.Converters;

/// <summary>
/// Messages d'execution en un seul texte formate : un seul Label permet de selectionner plusieurs lignes d'un coup.
/// Chaque niveau a son style : sortie du code (Output : PRINT, Log.Output), avertissement, erreur ; le reste en retrait.
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
        if (value is not IEnumerable<LogEntry> messages)
            return text;

        LogEntry? previous = null;
        foreach (var message in messages)
        {
            // Les sorties du code se suivent comme dans un terminal ; le reste est aere.
            var separator = previous is null ? string.Empty
                : previous.Level == LogLevel.Information && message.Level == LogLevel.Information ? Environment.NewLine
                : Environment.NewLine + Environment.NewLine;

            text.Spans.Add(new Span { Text = separator + message.Text, Style = StyleOf(message) });
            previous = message;
        }

        return text;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private Style? StyleOf(LogEntry message) => message.Level switch
    {
        LogLevel.Error or LogLevel.Critical => ErrorStyle,
        LogLevel.Warning => WarningStyle,
        LogLevel.Information => OutputStyle,
        // Information, Trace et Debug en retrait : messages de l'application et du moteur.
        _ => InfoStyle,
    };
}
