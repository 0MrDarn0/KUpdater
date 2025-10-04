// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Core.UI {
    /// <summary>
    /// Zentraler UI-Zustand für die Anwendung.
    /// Enthält Status, Fortschritt, Changelog und wird später erweitert.
    /// </summary>
    public class UIState {
        // Update-bezogene States
        public string Status { get; private set; } = "Status: Waiting...";
        public double Progress { get; private set; } = 0.0;
        public string Changelog { get; private set; } = "Changelog ...";

        public void SetStatus(string text) => Status = text;
        public void SetProgress(double percent) => Progress = Math.Clamp(percent / 100.0, 0.0, 1.0);
        public void SetChangelog(string text) => Changelog = text;
    }
}
