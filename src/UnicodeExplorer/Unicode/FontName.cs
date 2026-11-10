namespace UnicodeExplorer.Unicode;

public static class FontName
{
    public const int MaxLength = 100;

    /// <summary>
    /// Cleans user input so it can only ever be a font family name:
    /// removes characters that could break out of a CSS value and trims the length.
    /// </summary>
    public static string Sanitize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        var cleaned = new string(input.Where(c => !"\"'`;:{}()<>\\/".Contains(c) && !char.IsControl(c)).ToArray()).Trim();
        return cleaned.Length > MaxLength ? cleaned[..MaxLength].Trim() : cleaned;
    }
}
