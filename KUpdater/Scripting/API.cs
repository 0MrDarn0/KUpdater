// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using KUpdater.Core.Attributes;

namespace KUpdater.Scripting {
    [ExposeToLua("MessageBox")]
    public static class MessageAPI {
        public static void Show(string text, string title = "Info") {
            MessageBox.Show(text, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    [ExposeToLua]
    public class ProcessStart {
        public ProcessStart(string fileName, string arguments = "", bool useshell = false) {
            FileName = fileName;
            Arguments = arguments;
            UseShellExecute = useshell;
        }
        public string FileName { get; set; }
        public string Arguments { get; set; } = "";
        public bool UseShellExecute { get; set; } = false;
    }

    [ExposeToLua("Process")]
    public static class ProcessAPI {
        public static void Start(ProcessStart info) {
            try {
                Process.Start(new ProcessStartInfo {
                    FileName = info.FileName,
                    Arguments = info.Arguments,
                    UseShellExecute = info.UseShellExecute
                });
            }
            catch (Exception ex) {
                MessageBox.Show($"Failed to start process: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    [ExposeToLua("Application")]
    public static class AppAPI {
        public static void Exit() => Application.Exit();
    }

    [ExposeToLua("File")]
    public static class FileAPI {
        public static bool Exists(string path) => File.Exists(path);
    }
}
