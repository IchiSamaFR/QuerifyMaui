using MauiIcons.Fluent;
using MauiIcons.Fluent.Filled;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace SqlToolManagerMaui.UI.Tools;

public static class GlyphIconHelper
{
    private static readonly Dictionary<Type, ConcurrentDictionary<Enum, string>> GlyphCache = new();

    public const string FontFamily = nameof(FluentIcons);
    public const string FilledFontFamily = nameof(FluentFilledIcons);

    /// <summary>MauiIcons porte le caractere de chaque icone dans un [Description] sur la valeur d'enum.</summary>
    public static string Glyph<T>(T icon) where T : Enum
    {
        var type = typeof(T);
        if (!GlyphCache.TryGetValue(type, out var cache))
        {
            cache = new ConcurrentDictionary<Enum, string>();
            GlyphCache[type] = cache;
        }

        return cache.GetOrAdd(icon, static i =>
            typeof(T).GetField(i.ToString(), BindingFlags.Public | BindingFlags.Static)?
                .GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty);
    }
}
