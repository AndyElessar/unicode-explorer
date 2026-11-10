using Microsoft.JSInterop;

namespace UnicodeExplorer.Services;

/// <summary>Shows short messages such as "Copied U+0041".</summary>
public sealed class ToastService
{
    public event Action<string>? Shown;

    public void Show(string message) => Shown?.Invoke(message);
}

public sealed class ClipboardService(IJSRuntime js, ToastService toast)
{
    public async Task CopyAsync(string text, string description)
    {
        bool ok;
        try
        {
            ok = await js.InvokeAsync<bool>("ue.copy", text);
        }
        catch (JSException)
        {
            ok = false;
        }
        toast.Show(ok ? $"Copied {description}" : "Copy failed — please select the text and copy it manually");
    }
}

/// <summary>The font used to render characters (see docs/SPEC.md 3.5).</summary>
public sealed class FontService(IJSRuntime js)
{
    public static readonly IReadOnlyList<(string Group, string[] Fonts)> Presets =
    [
        ("Generic", ["serif", "sans-serif", "monospace"]),
        ("Windows", ["Segoe UI", "Segoe UI Symbol", "Segoe UI Emoji", "Segoe UI Historic", "Microsoft JhengHei", "Cambria Math"]),
        ("macOS / iOS", ["SF Pro", "Apple Color Emoji", "Apple Symbols", "PingFang TC", "STIX Two Math"]),
        ("Common", ["Arial", "Times New Roman", "Courier New", "Noto Sans", "Noto Serif", "Noto Sans CJK TC", "Noto Color Emoji"]),
    ];

    /// <summary>The saved font name, or "" (system default) when it cannot be read.</summary>
    public async Task<string> GetAsync()
    {
        try
        {
            return await js.InvokeAsync<string>("ue.getFont");
        }
        catch (JSException)
        {
            return "";
        }
    }

    public async Task SetAsync(string name)
    {
        try
        {
            await js.InvokeVoidAsync("ue.setFont", Unicode.FontName.Sanitize(name));
        }
        catch (JSException)
        {
            // The font is a convenience; keep the current one if it cannot be applied
        }
    }
}
