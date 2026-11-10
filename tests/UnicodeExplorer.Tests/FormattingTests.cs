using UnicodeExplorer.Unicode;

namespace UnicodeExplorer.Tests;

public class EncodingFormatterTests
{
    private const string Backslash = "\\";

    [Test]
    [Arguments(0x0000, "00", "0000")]
    [Arguments(0x0041, "41", "0041")]
    [Arguments(0x00E9, "C3 A9", "00E9")]
    [Arguments(0x20AC, "E2 82 AC", "20AC")]
    [Arguments(0xFFFF, "EF BF BF", "FFFF")]
    [Arguments(0x10000, "F0 90 80 80", "D800 DC00")]
    [Arguments(0x1F600, "F0 9F 98 80", "D83D DE00")]
    [Arguments(0x10FFFF, "F4 8F BF BF", "DBFF DFFF")]
    public async Task Utf8_and_utf16(int cp, string utf8, string utf16)
    {
        await Assert.That(EncodingFormatter.Utf8(cp)).IsEqualTo(utf8);
        await Assert.That(EncodingFormatter.Utf16(cp)).IsEqualTo(utf16);
    }

    [Test]
    public async Task Escapes_for_supplementary_character()
    {
        const int cp = 0x1F600;
        await Assert.That(EncodingFormatter.HtmlHex(cp)).IsEqualTo("&#x1F600;");
        await Assert.That(EncodingFormatter.Css(cp)).IsEqualTo(Backslash + "1F600");
        await Assert.That(EncodingFormatter.JavaScript(cp)).IsEqualTo(Backslash + "u{1F600}");
        await Assert.That(EncodingFormatter.Utf16Escape(cp)).IsEqualTo(Backslash + "uD83D" + Backslash + "uDE00");
        await Assert.That(EncodingFormatter.CSharp(cp)).IsEqualTo(Backslash + "U0001F600");
        await Assert.That(EncodingFormatter.Url(cp)).IsEqualTo("%F0%9F%98%80");
        await Assert.That(EncodingFormatter.ToText(cp)).IsEqualTo("😀");
    }

    [Test]
    public async Task Escapes_for_bmp_character()
    {
        await Assert.That(EncodingFormatter.JavaScript(0x41)).IsEqualTo(Backslash + "u0041");
        await Assert.That(EncodingFormatter.CSharp(0x41)).IsEqualTo(Backslash + "u0041");
    }

    [Test]
    public async Task Surrogates_have_no_text() => await Assert.That(EncodingFormatter.ToText(0xD800)).IsNull();
}

public class CharDisplayTests
{
    private readonly UnicodeDatabase _db = TestData.SampleDb();

    [Test]
    [Arguments(0x0041, "A", false)]
    [Arguments(0x0000, "␀", false)]
    [Arguments(0x0301, "◌́", false)]
    [Arguments(0x0020, "SP", true)]
    [Arguments(0xE000, "PUA", true)]
    public async Task Display_rules(int cp, string text, bool isLabel)
    {
        var display = CharDisplay.For(_db.GetChar(cp)!);
        await Assert.That(display.Text).IsEqualTo(text);
        await Assert.That(display.IsLabel).IsEqualTo(isLabel);
    }
}

public class FontNameTests
{
    [Test]
    [Arguments("Noto Sans", "Noto Sans")]
    [Arguments("  Segoe UI  ", "Segoe UI")]
    [Arguments("\"Arial\"; color: red", "Arial color red")]
    [Arguments("x}body{display:none", "xbodydisplaynone")]
    [Arguments(null, "")]
    [Arguments("   ", "")]
    public async Task Sanitize(string? input, string expected) =>
        await Assert.That(FontName.Sanitize(input)).IsEqualTo(expected);

    [Test]
    public async Task Sanitize_limits_length() =>
        await Assert.That(FontName.Sanitize(new string('a', 500)).Length).IsEqualTo(FontName.MaxLength);
}
