// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using KUpdater.UI;

namespace KUpdater.Scripting {
    /// <summary>
    /// Central place for all UI element IDs and strongly-typed bindings.
    /// </summary>
    public static class UIBindings {
        // 🔑 Centralized IDs
        public static class Ids {
            public const string UpdateStatusLabel = "lb_update_status";
            public const string UpdateProgressBar = "pb_update_progress";

            public const string TitleLabel = "lb_title";
            public const string SubtitleLabel = "lb_subtitle";

            public const string CloseButton = "btn_close";
            public const string StartButton = "btn_start";
            public const string SettingsButton = "btn_settings";
            public const string WebsiteButton = "btn_website";

            public const string ChangeLogTextBox = "tb_changelog";
        }

        // 🔗 Binding helpers   
        public static Action<string> BindLabelText(UIElementManager mgr, string id)
            => text => mgr.Update<UILabel>(id, l => l.Text = text);

        public static Action<double> BindProgress(UIElementManager mgr, string id)
            => value => mgr.Update<UIProgressBar>(id, b => b.Progress = (float)Math.Clamp(value, 0.0, 1.0));

        public static Action<string> BindButtonText(UIElementManager mgr, string id)
            => text => mgr.Update<UIButton>(id, b => b.Text = text);

        public static Action<System.Drawing.Color> BindLabelColor(UIElementManager mgr, string id)
            => color => mgr.Update<UILabel>(id, l => l.Color = color);

        public static Action<string, string> UpdateLabel(UIElementManager mgr)
              => (id, text) => mgr.Update<UILabel>(id, l => l.Text = text);

        public static Action<string, double> UpdateProgress(UIElementManager mgr)
            => (id, value) => mgr.Update<UIProgressBar>(id, b => b.Progress = (float)Math.Clamp(value, 0.0, 1.0));

        public static Action<string> BindTextBoxText(UIElementManager mgr, string id)
           => text => mgr.Update<UITextBox>(id, tb => tb.Text = text);
    }
}
