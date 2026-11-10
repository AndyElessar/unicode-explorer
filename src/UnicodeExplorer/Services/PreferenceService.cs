using System.Globalization;
using Microsoft.JSInterop;

namespace UnicodeExplorer.Services;

/// <summary>Recently viewed characters, favorites, and theme, stored in localStorage (see docs/SPEC.md 3.7).</summary>
public sealed class PreferenceService
{
    public const int MaxRecent = 50;
    public static readonly string[] Themes = ["system", "light", "dark"];

    private const string RecentKey = "ue.recent";
    private const string FavoritesKey = "ue.favorites";
    private const string ThemeKey = "ue.theme";

    // In Blazor WebAssembly JS interop can be called synchronously, so preferences are available immediately.
    private readonly IJSInProcessRuntime _js;
    private List<int> _recent;
    private List<int> _favorites;

    public PreferenceService(IJSRuntime js)
    {
        _js = (IJSInProcessRuntime)js;
        _recent = CodePointList.Parse(Load(RecentKey));
        _favorites = CodePointList.Parse(Load(FavoritesKey));
        var theme = Load(ThemeKey);
        Theme = theme != null && Themes.Contains(theme) ? theme : "system";
    }

    public event Action? Changed;

    /// <summary>Most recent first.</summary>
    public IReadOnlyList<int> Recent => _recent;

    /// <summary>In the order they were added.</summary>
    public IReadOnlyList<int> Favorites => _favorites;

    public string Theme { get; private set; }

    public void AddRecent(int cp)
    {
        if (_recent.Count > 0 && _recent[0] == cp) return;
        _recent = CodePointList.AddRecent(_recent, cp, MaxRecent);
        Save(RecentKey, _recent);
    }

    public void ClearRecent()
    {
        _recent = [];
        Save(RecentKey, _recent);
    }

    public bool IsFavorite(int cp) => _favorites.Contains(cp);

    public void ToggleFavorite(int cp)
    {
        _favorites = CodePointList.Toggle(_favorites, cp);
        Save(FavoritesKey, _favorites);
    }

    /// <summary>Cycles System → Light → Dark → System.</summary>
    public void CycleTheme() => SetTheme(ThemeRules.Next(Theme));

    public void SetTheme(string theme)
    {
        Theme = Themes.Contains(theme) ? theme : "system";
        Call("ue.save", ThemeKey, Theme);
        Call("ue.applyTheme", Theme);
        Changed?.Invoke();
    }

    private string? Load(string key)
    {
        try
        {
            return _js.Invoke<string?>("ue.load", key);
        }
        catch (JSException)
        {
            return null; // Fall back to defaults
        }
    }

    private void Save(string key, List<int> list)
    {
        Call("ue.save", key, CodePointList.Format(list));
        Changed?.Invoke();
    }

    /// <summary>Preferences are conveniences: the in-memory value still works for this session if JS fails.</summary>
    private void Call(string identifier, params object?[] args)
    {
        try
        {
            _js.InvokeVoid(identifier, args);
        }
        catch (JSException) { }
    }
}

public static class ThemeRules
{
    /// <summary>The theme after <paramref name="theme"/> in the order of <see cref="PreferenceService.Themes"/>; unknown values start over.</summary>
    public static string Next(string theme)
    {
        var themes = PreferenceService.Themes;
        return themes[(Array.IndexOf(themes, theme) + 1) % themes.Length];
    }

    public static string Label(string theme) => theme switch
    {
        "light" => "Light",
        "dark" => "Dark",
        _ => "System",
    };
}

/// <summary>Pure helpers for code point lists stored as comma-separated hex, e.g. "1F600,41".</summary>
public static class CodePointList
{
    public static List<int> Parse(string? text) =>
        (text ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var cp) ? cp : -1)
            .Where(cp => cp is >= 0 and <= Unicode.UnicodeDatabase.MaxCodePoint)
            .Distinct()
            .ToList();

    public static string Format(IEnumerable<int> list) => string.Join(',', list.Select(cp => cp.ToString("X")));

    /// <summary>Moves (or adds) cp to the front and keeps at most max items.</summary>
    public static List<int> AddRecent(IEnumerable<int> list, int cp, int max) =>
        list.Where(x => x != cp).Prepend(cp).Take(max).ToList();

    /// <summary>Removes cp if present, otherwise appends it.</summary>
    public static List<int> Toggle(IReadOnlyList<int> list, int cp) =>
        list.Contains(cp) ? list.Where(x => x != cp).ToList() : [.. list, cp];
}
