using Quill.Code.Scripting;
using System.Globalization;

namespace Querify.Converters;

/// <summary>
/// Messages d'execution en un seul texte formate : un seul Label permet de selectionner plusieurs lignes d'un coup.
/// Chaque message a son style : erreur, avertissement, sortie du code (PRINT, Log.*) ; les messages de l'hote en retrait.
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
                : IsOutput(previous) && IsOutput(message) ? Environment.NewLine
                : Environment.NewLine + Environment.NewLine;

            text.Spans.Add(new Span { Text = separator + message.Text, Style = StyleOf(message) });
            previous = message;
        }

        return text;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private Style? StyleOf(LogEntry message) => message switch
    {
        { IsError: true } => ErrorStyle,
        { Level: LogLevel.Warning } => WarningStyle,
        _ when IsOutput(message) => OutputStyle,
        _ => InfoStyle,
    };

    private static bool IsOutput(LogEntry message) => message.Source == LogSource.Code && message.Level < LogLevel.Warning;
}
