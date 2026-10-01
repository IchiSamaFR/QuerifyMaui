using System.Text.RegularExpressions;
using System.Xml.Linq;
using SqlToolManagerMaui.Models.Editors.Highlighting;

namespace SqlToolManagerMaui.Services.Editors.Highlighting;

/// <summary>
/// Lit les fichiers .xshd (format AvalonEdit) embarques dans les assets et les traduit
/// en configuration exploitable par l'editeur. Le sous-ensemble supporte est celui utilise
/// par nos fichiers : Property, Color, RuleSet, Span, Keywords, Rule.
/// </summary>
[AutoRegister(As = typeof(ISyntaxConfigurationStore))]
public sealed class XshdSyntaxConfigurationStore : ISyntaxConfigurationStore
{
    private const string AssetFolder = "SyntaxHighlights";

    /// <summary>Nom de la propriete .xshd portant le prefixe de commentaire de ligne.</summary>
    private const string LineCommentProperty = "LineComment";

    private static readonly XNamespace Xshd = "http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008";

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<SyntaxLanguage, SyntaxConfiguration?> _cache = [];

    public async Task<SyntaxConfiguration?> GetAsync(SyntaxLanguage language, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_cache.TryGetValue(language, out var cached))
                return cached;

            var configuration = await LoadAsync(language, cancellationToken);
            _cache[language] = configuration;
            return configuration;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<SyntaxLanguage?> FindLanguageAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(filePath);
        if (string.IsNullOrEmpty(extension))
            return null;

        foreach (var language in Enum.GetValues<SyntaxLanguage>().Where(l => l != SyntaxLanguage.None))
        {
            var configuration = await GetAsync(language, cancellationToken);
            if (configuration?.Definition.Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase) == true)
                return language;
        }

        return null;
    }

    private static async Task<SyntaxConfiguration?> LoadAsync(SyntaxLanguage language, CancellationToken cancellationToken)
    {
        var assetPath = $"{AssetFolder}/{language}.xshd";

        if (language == SyntaxLanguage.None || !await FileSystem.AppPackageFileExistsAsync(assetPath))
            return null;

        await using var stream = await FileSystem.OpenAppPackageFileAsync(assetPath);
        var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);

        return Parse(document, language);
    }

    private static SyntaxConfiguration? Parse(XDocument document, SyntaxLanguage language)
    {
        var root = document.Root;
        var ruleSet = root?.Element(Xshd + "RuleSet");
        if (root is null || ruleSet is null)
            return null;

        return new SyntaxConfiguration
        {
            Language = language,
            Comments = ReadLineComment(root),
            Definition = ParseDefinition(root, ruleSet, language)
        };
    }

    /// <summary>Lit &lt;Property name="LineComment" value="--" /&gt;, avec le SQL comme valeur par defaut.</summary>
    private static string ReadLineComment(XElement root)
    {
        var property = root.Elements(Xshd + "Property")
                           .FirstOrDefault(e => string.Equals((string?)e.Attribute("name"), LineCommentProperty, StringComparison.OrdinalIgnoreCase));

        var value = (string?)property?.Attribute("value");

        return string.IsNullOrEmpty(value) ? "--" : value;
    }

    private static SyntaxDefinition ParseDefinition(XElement root, XElement ruleSet, SyntaxLanguage language)
    {
        var colors = ReadColors(root);

        var ignoreCase = (bool?)ruleSet.Attribute("ignoreCase") ?? false;
        var options = RegexOptions.CultureInvariant | (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);

        List<SyntaxRule> rules = [];
        List<SyntaxKeyword> keywords = [];
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

            if (element.Name.LocalName == "Keywords")
                keywords.AddRange(ReadKeywords(element));
        }

        return new SyntaxDefinition
        {
            Name = (string?)root.Attribute("name") ?? language.ToString(),
            Extensions = ((string?)root.Attribute("extensions"))?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [],
            Rules = rules,
            Keywords = keywords
        };
    }

    /// <summary>La categorie est le nom de couleur de la liste : c'est lui qui distingue mots-cles, types et fonctions.</summary>
    private static IEnumerable<SyntaxKeyword> ReadKeywords(XElement element)
    {
        var category = (string?)element.Attribute("color") ?? string.Empty;

        return element.Elements(Xshd + "Word")
                      .Select(word => word.Value.Trim())
                      .Where(word => word.Length > 0)
                      .Select(word => new SyntaxKeyword(word, category));
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
