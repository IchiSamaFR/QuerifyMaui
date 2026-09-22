using System.Text.RegularExpressions;
using System.Xml.Linq;
using SqlToolManagerMaui.Models.Editors.Highlighting;

namespace SqlToolManagerMaui.Services.Editors.Highlighting;

/// <summary>
/// Lit les fichiers .xshd (format AvalonEdit) embarques dans les assets et les traduit
/// en regles exploitables par le tokenizer. Le sous-ensemble supporte est celui utilise
/// par nos fichiers : Color, RuleSet, Span, Keywords, Rule.
/// </summary>
public sealed class XshdSyntaxDefinitionStore : ISyntaxDefinitionStore
{
    private const string AssetFolder = "SyntaxHighlights";

    private static readonly XNamespace Xshd = "http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008";

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, SyntaxDefinition?> _cache = [];

    public async Task<SyntaxDefinition?> GetAsync(string language, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_cache.TryGetValue(language, out var cached))
                return cached;

            var definition = await LoadAsync(language, cancellationToken);
            _cache[language] = definition;
            return definition;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static async Task<SyntaxDefinition?> LoadAsync(string language, CancellationToken cancellationToken)
    {
        var assetPath = $"{AssetFolder}/{language}.xshd";

        if (!await FileSystem.AppPackageFileExistsAsync(assetPath))
            return null;

        await using var stream = await FileSystem.OpenAppPackageFileAsync(assetPath);
        var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);

        return Parse(document, language);
    }

    private static SyntaxDefinition? Parse(XDocument document, string language)
    {
        var root = document.Root;
        var ruleSet = root?.Element(Xshd + "RuleSet");
        if (root is null || ruleSet is null)
            return null;

        var colors = ReadColors(root);

        var ignoreCase = (bool?)ruleSet.Attribute("ignoreCase") ?? false;
        var options = RegexOptions.CultureInvariant | (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);

        List<SyntaxRule> rules = [];
        foreach (var element in ruleSet.Elements())
        {
            if (ResolveColor(colors, element) is not { } color)
                continue;

            var rule = element.Name.LocalName switch
            {
                "Span" => CreateSpan(element, color, options),
                "Keywords" => CreateKeywords(element, color, options),
                "Rule" => CreatePattern(element.Value, color, options),
                _ => null
            };

            if (rule is not null)
                rules.Add(rule);
        }

        return new SyntaxDefinition
        {
            Name = (string?)root.Attribute("name") ?? language,
            Extensions = ((string?)root.Attribute("extensions"))?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [],
            Rules = rules
        };
    }

    private static Dictionary<string, Color> ReadColors(XElement root)
    {
        Dictionary<string, Color> colors = new(StringComparer.OrdinalIgnoreCase);

        foreach (var element in root.Elements(Xshd + "Color"))
        {
            var name = (string?)element.Attribute("name");
            var foreground = (string?)element.Attribute("foreground");

            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(foreground))
                colors[name] = Color.FromArgb(foreground);
        }

        return colors;
    }

    private static Color? ResolveColor(Dictionary<string, Color> colors, XElement element)
    {
        var name = (string?)element.Attribute("color");

        return !string.IsNullOrEmpty(name) && colors.TryGetValue(name, out var color) ? color : null;
    }

    private static SyntaxRule? CreateSpan(XElement element, Color color, RegexOptions options)
    {
        var begin = element.Element(Xshd + "Begin")?.Value.Trim();
        if (string.IsNullOrEmpty(begin))
            return null;

        var end = element.Element(Xshd + "End")?.Value.Trim();
        var isMultiline = (bool?)element.Attribute("multiline") ?? false;

        return new SyntaxSpanRule(Anchored(begin, options), string.IsNullOrEmpty(end) ? null : new Regex(end, options), isMultiline)
        {
            Color = color
        };
    }

    private static SyntaxRule? CreateKeywords(XElement element, Color color, RegexOptions options)
    {
        var words = element.Elements(Xshd + "Word")
                           .Select(word => word.Value.Trim())
                           .Where(word => word.Length > 0)
                           .Select(Regex.Escape)
                           .ToArray();

        if (words.Length == 0)
            return null;

        // Les \b evitent de colorer un mot-cle noye dans un identifiant (SELECTED).
        return new SyntaxPatternRule(new Regex($@"\G\b(?:{string.Join('|', words)})\b", options))
        {
            Color = color
        };
    }

    private static SyntaxRule? CreatePattern(string pattern, Color color, RegexOptions options)
    {
        pattern = pattern.Trim();

        return string.IsNullOrEmpty(pattern) ? null : new SyntaxPatternRule(Anchored(pattern, options)) { Color = color };
    }

    /// <summary>Ancre le motif sur la position testee : sans \G, Regex.Match chercherait plus loin.</summary>
    private static Regex Anchored(string pattern, RegexOptions options) => new($@"\G(?:{pattern})", options);
}
