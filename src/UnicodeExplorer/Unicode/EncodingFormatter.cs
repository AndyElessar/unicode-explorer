namespace UnicodeExplorer.Unicode;

/// <summary>Computes encodings and escape sequences for a code point.</summary>
public static class EncodingFormatter
{
    public static bool IsSurrogate(int cp) => cp is >= 0xD800 and <= 0xDFFF;

    /// <summary>The character as a string, or null for surrogate code points (which cannot exist alone).</summary>
    public static string? ToText(int cp) => IsSurrogate(cp) ? null : char.ConvertFromUtf32(cp);

    public static byte[] Utf8Bytes(int cp) => cp switch
    {
        < 0x80 => [(byte)cp],
        < 0x800 => [(byte)(0xC0 | cp >> 6), (byte)(0x80 | cp & 0x3F)],
        < 0x10000 => [(byte)(0xE0 | cp >> 12), (byte)(0x80 | cp >> 6 & 0x3F), (byte)(0x80 | cp & 0x3F)],
        _ => [(byte)(0xF0 | cp >> 18), (byte)(0x80 | cp >> 12 & 0x3F), (byte)(0x80 | cp >> 6 & 0x3F), (byte)(0x80 | cp & 0x3F)],
    };

    public static int[] Utf16Units(int cp) => cp < 0x10000
        ? [cp]
        : [0xD800 + ((cp - 0x10000) >> 10), 0xDC00 + ((cp - 0x10000) & 0x3FF)];

    public static string Utf8(int cp) => string.Join(' ', Utf8Bytes(cp).Select(b => b.ToString("X2")));

    public static string Utf16(int cp) => string.Join(' ', Utf16Units(cp).Select(u => u.ToString("X4")));

    public static string HtmlHex(int cp) => $"&#x{cp:X};";

    public static string Css(int cp) => $"\\{cp:X}";

    public static string JavaScript(int cp) => cp < 0x10000 ? $"\\u{cp:X4}" : $"\\u{{{cp:X}}}";

    /// <summary>UTF-16 escape, valid in JSON, JavaScript, Java, etc.</summary>
    public static string Utf16Escape(int cp) => string.Concat(Utf16Units(cp).Select(u => $"\\u{u:X4}"));

    public static string CSharp(int cp) => cp < 0x10000 ? $"\\u{cp:X4}" : $"\\U{cp:X8}";

    public static string Url(int cp) => string.Concat(Utf8Bytes(cp).Select(b => $"%{b:X2}"));
}
