// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using KUpdater.Core.UI;
using KUpdater.UI;
using KUpdater.Utility;
using MoonSharp.Interpreter;

namespace KUpdater.Scripting.Theme {
    public class MainTheme : ThemeBase {
        public MainTheme(Form form, UIElementManager mgr, UIState state, string lang)
            : base("theme_loader.lua", form, mgr, state, lang) { }

        protected override string GetThemeName() => "main_form";

        protected override void RegisterGlobals() {
            base.RegisterGlobals();
            SetGlobal(LuaKeys.Theme.ThemeDir, Paths.LuaThemes.Replace("\\", "/"));
            SetGlobal(LuaKeys.UI.GetWindowSize, () => DynValue.NewTuple(
                DynValue.NewNumber(_form.Width),
                DynValue.NewNumber(_form.Height)
            ));
            SetGlobal("open_website", (Action<string>)((url) => {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }));
            SetGlobal(LuaKeys.Actions.StartGame, (Action)(() => GameLauncher.StartGame()));
            SetGlobal(LuaKeys.Actions.OpenSettings, (Action)(() => GameLauncher.OpenSettings()));
            SetGlobal(LuaKeys.Actions.ApplicationExit, (Action)(() => Application.Exit()));
            ExposeToLua("uiElement", _mgr);
            ExposeToLua<Font>();
            ExposeToLua<Color>();
            ExposeMarkedTypes();
            SetGlobal("update_status", (Action<string>)(text => _mgr.Update<UILabel>("lb_update_status", l => l.Text = text)));
            SetGlobal("update_download_progress", (Action<double>)(percent => _mgr.Update<UIProgressBar>("pb_update_progress", b => b.Progress = (float)Math.Clamp(percent, 0.0, 1.0))));
            SetGlobal("update_label", UIBindings.UpdateLabel(_mgr));
            SetGlobal("update_progress", UIBindings.UpdateProgress(_mgr));
        }


        protected override void UpdateLastState() {
            _mgr.Update<UILabel>("lb_update_status", l => l.Text = _state.Status);
            _mgr.Update<UIProgressBar>("pb_update_progress", b => b.Progress = (float)_state.Progress);
            _mgr.Update<UITextBox>("tb_changelog", tb => tb.Text = _state.Changelog);
        }


        protected override ThemeBackground BuildBackground() {
            var bg = new ThemeTable(GetThemeTable("background"), Script);
            return new ThemeBackground {
                TopLeft = _resources.GetSkiaBitmap(bg.GetString("top_left")),
                TopCenter = _resources.GetSkiaBitmap(bg.GetString("top_center")),
                TopRight = _resources.GetSkiaBitmap(bg.GetString("top_right")),
                RightCenter = _resources.GetSkiaBitmap(bg.GetString("right_center")),
                BottomRight = _resources.GetSkiaBitmap(bg.GetString("bottom_right")),
                BottomCenter = _resources.GetSkiaBitmap(bg.GetString("bottom_center")),
                BottomLeft = _resources.GetSkiaBitmap(bg.GetString("bottom_left")),
                LeftCenter = _resources.GetSkiaBitmap(bg.GetString("left_center")),
                FillColor = bg.GetColor("fill_color", Color.Black)
            };
        }

        protected override ThemeLayout BuildLayout() {
            var layout = new ThemeTable(GetThemeTable("layout"), Script);
            return new ThemeLayout {
                TopWidthOffset = layout.GetInt("top_width_offset"),
                BottomWidthOffset = layout.GetInt("bottom_width_offset"),
                LeftHeightOffset = layout.GetInt("left_height_offset"),
                RightHeightOffset = layout.GetInt("right_height_offset"),
                FillPosOffset = layout.GetInt("fill_pos_offset"),
                FillWidthOffset = layout.GetInt("fill_width_offset"),
                FillHeightOffset = layout.GetInt("fill_height_offset")
            };
        }
    }

}
