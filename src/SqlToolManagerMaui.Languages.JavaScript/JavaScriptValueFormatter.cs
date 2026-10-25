using Jint;
using Jint.Native;
using Jint.Native.Json;
using Jint.Runtime;
using System.Globalization;

namespace SqlToolManagerMaui.Languages.JavaScript;

/// <summary>Valeurs JavaScript rendues en texte, pour la grille de resultats et la console.</summary>
internal static class JavaScriptValueFormatter
{
    /// <summary>Memes conventions d'affichage que l'execution SQL ; null = cellule NULL.</summary>
    public static string? ToCell(Engine engine, JsValue value)
    {
        if (value.IsNull() || value.IsUndefined())
            return null;

        if (value.IsString())
            return value.AsString();

        if (value.IsBoolean())
            return value.AsBoolean() ? "1" : "0";

        if (value.IsDate())
        {
            var date = value.AsDate();
            return double.IsNaN(date.DateValue)
                ? "Invalid Date"
                : date.ToDateTime().ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        }

        // Tableau imbrique : sa taille plutot que son contenu, comme une collection en LINQ.
        if (value.IsArray())
            return $"({value.AsArray().Length} élément(s))";

        return value.IsObject() ? ToJson(engine, value) : ToText(value);
    }

    /// <summary>Argument de console.log : texte brut, objets en JSON.</summary>
    public static string ToConsole(Engine engine, JsValue value)
    {
        if (value.IsUndefined())
            return "undefined";

        return value.IsObject() && !value.IsDate() ? ToJson(engine, value) : ToText(value);
    }

    /// <summary>Type affiche dans l'en-tete de colonne.</summary>
    public static string TypeName(JsValue value)
    {
        if (value.IsDate())
            return "Date";

        if (value.IsArray())
            return "array";

        return value.Type switch
        {
            Types.Null or Types.Undefined => "null",
            Types.Boolean => "boolean",
            Types.String => "string",
            Types.Number => "number",
            Types.BigInt => "bigint",
            Types.Symbol => "symbol",
            _ => "object",
        };
    }

    private static string ToText(JsValue value)
    {
        // Un symbole refuse la conversion implicite en texte (TypeError).
        try
        {
            return TypeConverter.ToString(value);
        }
        catch (JavaScriptException)
        {
            return value.ToString();
        }
    }

    private static string ToJson(Engine engine, JsValue value)
    {
        // Reference circulaire, fonction ou BigInt : JSON.stringify echoue ou ne rend rien.
        try
        {
            var json = new JsonSerializer(engine).Serialize(value);
            return json.IsString() ? json.AsString() : ToText(value);
        }
        catch (JavaScriptException)
        {
            return ToText(value);
        }
    }
}
