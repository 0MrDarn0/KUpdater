// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Scripting {
    public static class LuaKeys {

        public const string ExeDirectory = "exe_directory";

        public static class UI {
            public const string AddLabel = "add_label";
            public const string AddButton = "add_button";
            public const string GetWindowSize = "get_window_size";
        }

        public static class Theme {
            public const string LoadTheme = "load_theme";
            public const string GetTheme = "get_theme";
            public const string ThemeDir = "THEME_DIR";
        }

        public static class Actions {
            public const string StartGame = "start_game";
            public const string OpenSettings = "open_settings";
            public const string ApplicationExit = "application_exit";
            public const string RunUpdate = "run_update";
            public const string CheckUpdate = "check_update";
        }

        public static class Classes {
            public const string Updater = "updater";
        }

    }
}
