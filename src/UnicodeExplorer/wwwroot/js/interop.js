// Small browser helpers called from Blazor. Loaded in <head> so the saved
// display font and theme are applied before the app renders.
window.ue = (() => {
    const FONT_KEY = "ue.glyphFont";
    const THEME_KEY = "ue.theme";
    const SYSTEM_STACK = 'system-ui, sans-serif, "Apple Color Emoji", "Segoe UI Emoji", "Noto Color Emoji"';
    const GENERIC = ["serif", "sans-serif", "monospace", "cursive", "fantasy", "system-ui"];

    // localStorage can throw (private mode, blocked storage); the app works without it.
    const storageGet = key => { try { return localStorage.getItem(key); } catch { return null; } };
    const storageSet = (key, value) => { try { localStorage.setItem(key, value); } catch { } };

    function applyFont(name) {
        name = (name || "").replace(/["'`\\;:{}()<>\/]/g, "").trim();
        const family = !name ? "" : GENERIC.includes(name) ? name + ", " : `"${name}", `;
        document.documentElement.style.setProperty("--glyph-font", family + SYSTEM_STACK);
    }

    // "light" or "dark" forces a theme; anything else follows the system (see app.css)
    function applyTheme(theme) {
        if (theme === "light" || theme === "dark") document.documentElement.dataset.theme = theme;
        else delete document.documentElement.dataset.theme;
    }

    applyFont(storageGet(FONT_KEY));
    applyTheme(storageGet(THEME_KEY));

    return {
        load: key => storageGet(key),
        save: (key, value) => storageSet(key, value),
        applyTheme,

        getFont: () => storageGet(FONT_KEY) || "",
        setFont: name => { storageSet(FONT_KEY, name); applyFont(name); },

        copy: async text => {
            try { await navigator.clipboard.writeText(text); return true; }
            catch { return false; }
        },

        // Reports the element's width to .NET whenever it changes. Returns an object with dispose().
        observeWidth: (element, dotnetRef) => {
            const observer = new ResizeObserver(entries =>
                dotnetRef.invokeMethodAsync("OnWidthChanged", entries[0].contentRect.width));
            observer.observe(element);
            return { dispose: () => observer.disconnect() };
        },

        getScrollTop: element => element ? element.scrollTop : 0,
        scrollToTop: element => { if (element && element.scrollTop !== 0) element.scrollTop = 0; },
    };
})();
