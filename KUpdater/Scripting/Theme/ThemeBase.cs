// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.Core.UI;
using KUpdater.Extensions;
using KUpdater.UI;
using KUpdater.Utility;
using MoonSharp.Interpreter;
using SkiaSharp;
using SniffKit.UI;

namespace KUpdater.Scripting.Theme {
    public abstract class ThemeBase : Lua, ITheme {
        protected readonly Form _form;
        protected readonly ControlManager _mgr;
        protected readonly UIState _state;
        protected readonly IResourceProvider _resourceProvider;
        private ThemeBackground? _cachedBackground;
        private ThemeLayout? _cachedLayout;

        protected ThemeBase(string themeScript, Form form, ControlManager mgr, UIState state, string lang, IResourceProvider resourceProvider)
            : base(themeScript) {
            _form = form;
            _mgr = mgr;
            _state = state;
            _resourceProvider = resourceProvider;
            RegisterGlobals();
            LoadLanguage(lang);
            LoadTheme(GetThemeName());
        }

        protected SKBitmap? GetSkiaBitmapFromProvider(string? id) {
            if (string.IsNullOrWhiteSpace(id))
                return null;
            try {
                // Provider bietet TryGetSkiaBitmap; verwende diese (non-throwing)
                var sk = _resourceProvider.TryGetSkiaBitmap(id);
                return sk;
            }
            catch {
                return null;
            }
        }

        protected abstract string GetThemeName();

        protected override void RegisterGlobals() {
            base.RegisterGlobals();
        }

        protected void LoadLanguage(string langCode) {
            var langPath = Paths.LuaLanguage(langCode);
            var fallbackPath = Paths.LuaDefaultLanguage;
            var langTable = Script.DoString(File.ReadAllText(langPath)).Table;
            var fallbackTable = Script.DoString(File.ReadAllText(fallbackPath)).Table;
            SetGlobal("L", langTable);
            SetGlobal("L_Fallback", fallbackTable);
            SetGlobal("T", (Func<string, string>)(key => {
                string? Lookup(Table table) {
                    var node = DynValue.NewTable(table);
                    foreach (var part in key.Split('.')) {
                        if (!node.IsTable())
                            return null;
                        node = node.Table.Get(part);
                    }
                    return node.AsString();
                }
                return Lookup(langTable) ?? Lookup(fallbackTable) ?? $"[MISSING:{key}]";
            }));
            Localization.Initialize(Script);
        }

        protected void LoadTheme(string themeName) {
            Invoke(LuaKeys.Theme.LoadTheme, themeName);
            var initFunc = new LuaValue<Closure>(Invoke(LuaKeys.Theme.GetTheme).Table.Get("init"));
            if (initFunc.IsValid)
                Invoke(initFunc.Raw);
        }

        protected Table GetThemeTable(string key) {
            var theme = Invoke(LuaKeys.Theme.GetTheme).Table;
            var table = new LuaValue<Table>(theme.Get(key));
            return table.Value ?? new Table(Script);
        }

        public void ApplyLastState() => UpdateLastState();
        public ThemeBackground GetBackground() => _cachedBackground ??= BuildBackground();
        public ThemeLayout GetLayout() => _cachedLayout ??= BuildLayout();

        protected abstract void UpdateLastState();
        protected abstract ThemeBackground BuildBackground();
        protected abstract ThemeLayout BuildLayout();
    }

}
