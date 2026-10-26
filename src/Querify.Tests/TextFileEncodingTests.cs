using Querify.Helpers.Tools;
using System.Text;
using Xunit;

namespace Querify.Tests;

/// <summary>Un fichier rouvert puis reenregistre sans modification doit rester identique octet pour octet.</summary>
public class TextFileEncodingTests
{
    private static readonly byte[] AnsiCafe = [0x63, 0x61, 0x66, 0xE9];
    private static readonly byte[] Utf8Cafe = [0x63, 0x61, 0x66, 0xC3, 0xA9];

    [Fact]
    public void Detect_InvalidUtf8_IsAnsi()
    {
        var encoding = TextFileEncoding.Detect(AnsiCafe);

        Assert.Same(TextFileEncoding.Ansi, encoding);
        Assert.Equal("café", TextFileEncoding.Decode(AnsiCafe, encoding));
        Assert.Equal(AnsiCafe, TextFileEncoding.Encode("café", encoding));
    }

    [Fact]
    public void Detect_Utf8WithoutBom_RoundTripsWithoutBom()
    {
        var encoding = TextFileEncoding.Detect(Utf8Cafe);

        Assert.Same(TextFileEncoding.Utf8WithoutBom, encoding);
        Assert.Equal("café", TextFileEncoding.Decode(Utf8Cafe, encoding));
        Assert.Equal(Utf8Cafe, TextFileEncoding.Encode("café", encoding));
    }

    [Fact]
    public void Detect_Utf8Bom_StripsBomOnDecode()
    {
        byte[] bytes = [0xEF, 0xBB, 0xBF, .. Utf8Cafe];
        var encoding = TextFileEncoding.Detect(bytes);

        Assert.Same(TextFileEncoding.Default, encoding);
        Assert.Equal("café", TextFileEncoding.Decode(bytes, encoding));
        Assert.Equal(bytes, TextFileEncoding.Encode("café", encoding));
    }

    [Fact]
    public void Detect_Utf16Bom_IsUnicode()
    {
        byte[] bytes = [.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes("café")];
        var encoding = TextFileEncoding.Detect(bytes);

        Assert.Equal(Encoding.Unicode.CodePage, encoding.CodePage);
        Assert.Equal("café", TextFileEncoding.Decode(bytes, encoding));
    }

    [Fact]
    public void Detect_EmptyFile_IsDefault()
        => Assert.Same(TextFileEncoding.Default, TextFileEncoding.Detect([]));

    [Fact]
    public void Encode_TextOutsideAnsi_FallsBackToUtf8WithBom()
    {
        var bytes = TextFileEncoding.Encode("café ✓", TextFileEncoding.Ansi);

        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);
        Assert.Equal("café ✓", TextFileEncoding.Decode(bytes, TextFileEncoding.Default));
    }
}
