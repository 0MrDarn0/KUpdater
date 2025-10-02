// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;

namespace KUpdater {
    public static class GameLauncher {
        public static void StartGame() {
            try {
                string exePath = Path.Combine(Application.StartupPath, "engine.exe");

                if (!File.Exists(exePath))
                    throw new FileNotFoundException("The game executable 'engine.exe' was not found.", exePath);

                Process.Start(new ProcessStartInfo {
                    FileName = exePath,
                    Arguments = "/load /config debug",
                    UseShellExecute = false
                });

                Environment.Exit(0); // Launcher sofort beenden
            }
            catch (Exception ex) {
                MessageBox.Show(
                    $"Unable to launch the game.\n\nDetails: {ex.Message}",
                    "Launch Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        public static void OpenSettings() {
            try {
                string exePath = Path.Combine(Application.StartupPath, "engine.exe");

                if (!File.Exists(exePath))
                    throw new FileNotFoundException("The game executable 'engine.exe' was not found.", exePath);

                Process.Start(new ProcessStartInfo {
                    FileName = exePath,
                    Arguments = "/setup",
                    UseShellExecute = false
                });
            }
            catch (Exception ex) {
                MessageBox.Show(
                    $"Unable to open the settings.\n\nDetails: {ex.Message}",
                    "Settings Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}
