using System.Text;

namespace SqlToolManagerMaui.Helpers.Tools;

/// <summary>
/// Detection et conservation de l'encodage d'un fichier texte : un script ANSI (Windows-1252) lu en
/// UTF-8 perdrait ses accents, et le reenregistrer autrement modifierait le fichier de l'utilisateur.
/// </summary>
public static class TextFileEncoding
{
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>Encodage des nouveaux fichiers : avec BOM, sinon Notepad devine et lit les accents en ANSI.</summary>
    public static Encoding Default { get; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    public static Encoding Utf8WithoutBom { get; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    /// <summary>Windows-1252 ; un caractere non representable leve une exception plutot que d'ecrire « ? ».</summary>
    public static Encoding Ansi { get; } = CreateAnsi();

    /// <summary>BOM d'abord, puis UTF-8 valide sans BOM ; a defaut, ANSI.</summary>
    public static Encoding Detect(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty || bytes.StartsWith(Default.Preamble))
            return Default;

        if (bytes.StartsWith(Encoding.Unicode.Preamble))
            return Encoding.Unicode;

        if (bytes.StartsWith(Encoding.BigEndianUnicode.Preamble))
            return Encoding.BigEndianUnicode;

        try
        {
            StrictUtf8.GetCharCount(bytes);
            return Utf8WithoutBom;
        }
        catch (DecoderFallbackException)
        {
            return Ansi;
        }
    }

    /// <summary>Texte du fichier, sans son preambule (BOM).</summary>
    public static string Decode(byte[] bytes, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentNullException.ThrowIfNull(encoding);

        var preamble = encoding.Preamble;
        var start = bytes.AsSpan().StartsWith(preamble) ? preamble.Length : 0;
        return encoding.GetString(bytes, start, bytes.Length - start);
    }

    /// <summary>Octets a ecrire, preambule compris ; un texte non representable en ANSI passe en <see cref="Default"/>.</summary>
    public static byte[] Encode(string text, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(encoding);

        try
        {
            return [.. encoding.Preamble, .. encoding.GetBytes(text)];
        }
        catch (EncoderFallbackException)
        {
            return [.. Default.Preamble, .. Default.GetBytes(text)];
        }
    }

    private static Encoding CreateAnsi()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(1252, EncoderFallback.ExceptionFallback, DecoderFallback.ReplacementFallback);
    }
}
