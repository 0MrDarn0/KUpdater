// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Runtime.InteropServices;
using System.Text.Json;

namespace Kx.App;

record CrashReport(
    string Id,
    DateTime Timestamp,
    string AppVersion,
    string DotNetVersion,
    string OS,
    string Source,
    string ExceptionType,
    string Message,
    string StackTrace,
    string InnerExceptions,
    string RecentLogs,
    string SessionId,
    string UserComment
);

static class CrashReportStore {
    private static readonly string _reportsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Kx", "crashreports");

    private const int MaxStoredReports = 20;

    public static void SaveReportLocally(CrashReport report) {
        try {
            Directory.CreateDirectory(_reportsDir);
            var tmp = Path.Combine(_reportsDir, report.Id + ".json.tmp");
            var final = Path.Combine(_reportsDir, report.Id + ".json");

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(report, options);

            File.WriteAllText(tmp, json);
            File.Move(tmp, final, overwrite: true);

            PruneOldReports();
        }
        catch {
            // Best effort: Fehler beim Speichern ignorieren
        }
    }

    private static void PruneOldReports() {
        try {
            var di = new DirectoryInfo(_reportsDir);
            if (!di.Exists)
                return;
            var files = di.GetFiles("*.json");
            if (files.Length <= MaxStoredReports)
                return;
            Array.Sort(files, (a, b) => a.CreationTimeUtc.CompareTo(b.CreationTimeUtc));
            var toDelete = files.Length - MaxStoredReports;
            for (int i = 0; i < toDelete; i++)
                files[i].Delete();
        }
        catch {
            // ignore
        }
    }

    public static CrashReport[] LoadAllReports() {
        try {
            if (!Directory.Exists(_reportsDir))
                return [];
            var files = Directory.GetFiles(_reportsDir, "*.json");
            var options = new JsonSerializerOptions();
            var list = new System.Collections.Generic.List<CrashReport>();
            foreach (var f in files) {
                try {
                    var json = File.ReadAllText(f);
                    var r = JsonSerializer.Deserialize<CrashReport>(json, options);
                    if (r is not null)
                        list.Add(r);
                }
                catch { /* skip corrupt files */ }
            }
            return [.. list];
        }
        catch { return []; }
    }

    public static void DeleteReport(string id) {
        try {
            var path = Path.Combine(_reportsDir, id + ".json");
            if (File.Exists(path))
                File.Delete(path);
        }
        catch { }
    }

    // Public so GlobalExceptionHandler darauf zugreifen kann
    public static CrashReport BuildCrashReport(string source, Exception ex, string userComment = "") {
        return new CrashReport(
            Id: Guid.NewGuid().ToString("D"),
            Timestamp: DateTime.UtcNow,
            AppVersion: GetAppVersion(),
            DotNetVersion: RuntimeInformation.FrameworkDescription,
            OS: RuntimeInformation.OSDescription,
            Source: source,
            ExceptionType: ex.GetType().FullName ?? ex.GetType().Name,
            Message: ex.Message,
            StackTrace: ex.ToString(),
            InnerExceptions: GetInnerExceptions(ex),
            RecentLogs: "",
            SessionId: GetOrCreateSessionId(),
            UserComment: userComment ?? string.Empty
        );
    }

    private static string GetInnerExceptions(Exception ex) {
        try {
            var sb = new System.Text.StringBuilder();
            var cur = ex.InnerException;
            while (cur != null) {
                sb.AppendLine($"{cur.GetType().FullName}: {cur.Message}");
                sb.AppendLine(cur.StackTrace ?? "");
                cur = cur.InnerException;
            }
            return sb.ToString();
        }
        catch { return string.Empty; }
    }

    private static string GetAppVersion() => "kx-updater@1.0.0";

    private static string GetOrCreateSessionId() {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kx", "session.id");
        try {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            if (File.Exists(path))
                return File.ReadAllText(path);
            var id = Guid.NewGuid().ToString("D");
            File.WriteAllText(path, id);
            return id;
        }
        catch { return Guid.NewGuid().ToString("D"); }
    }
}


internal static class GlobalExceptionHandler {
    private static bool _registered;
    private static Func<Task>? _shutdownHandler;

    public static void Register() {
        if (_registered)
            return;

        Application.ThreadException += OnThreadException;
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        _registered = true;
    }

    public static void Unregister() {
        if (!_registered)
            return;
        Application.ThreadException -= OnThreadException;
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        _registered = false;
    }

    public static void RegisterShutdownHandler(Func<Task> shutdownHandler) {
        ArgumentNullException.ThrowIfNull(shutdownHandler);
        _shutdownHandler = shutdownHandler;
    }

    private static void OnThreadException(object? sender, ThreadExceptionEventArgs e) {
        _ = HandleExceptionSafeAsync("UI Thread Exception", e.Exception);
    }

    private static void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e) {
        if (e.ExceptionObject is Exception ex)
            _ = HandleExceptionSafeAsync("Unhandled Exception", ex);
    }

    private static async Task HandleExceptionSafeAsync(string source, Exception exception) {
        try {
            await HandleExceptionAsync(source, exception).ConfigureAwait(false);
        }
        catch {
            Environment.Exit(1);
        }
    }

    static string MaskPaths(string input) {
        if (string.IsNullOrEmpty(input))
            return input;
        return System.Text.RegularExpressions.Regex.Replace(input, @"[A-Za-z]:\\[^\r\n]+", "[PATH]");
    }

    //public static void TriggerTestException() {
    //    var owner = Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null;
    //    if (owner is not null) {
    //        owner.BeginInvoke((Action)(() => throw new Exception("Simulated UI exception")));
    //    } else {
    //        new Thread(() => throw new Exception("Simulated background exception")) { IsBackground = true }.Start();
    //    }

    //    new Thread(() => throw new Exception("Simulated background exception")) { IsBackground = true }.Start();

    //    GC.Collect();
    //    GC.WaitForPendingFinalizers();
    //}

    private static async Task HandleExceptionAsync(string source, Exception exception) {
        try {
            // innerhalb HandleExceptionAsync, UI-Branch (ersetzt den CrashDialog-Teil)
            // inside HandleExceptionAsync, UI branch
            if (Application.MessageLoop) {
                var owner = Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null;
                DialogResult result;
                if (owner is not null) {
                    result = (DialogResult)owner.Invoke((Func<DialogResult>)(() =>
                        MessageBox.Show(owner,
                            "The application has crashed. Save a local crash report and notify the developer?",
                            "Crash",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Error)));
                } else {
                    result = MessageBox.Show(
                        "The application has crashed. Save a local crash report and notify the developer?",
                        "Crash",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Error);
                }

                if (result == DialogResult.Yes) {
                    var report = CrashReportStore.BuildCrashReport(source, exception, userComment: "");
                    report = report with {
                        Message = MaskPaths(report.Message),
                        StackTrace = MaskPaths(report.StackTrace)
                    };
                    CrashReportStore.SaveReportLocally(report);

                    if (owner is not null) {
                        owner.Invoke((Action)(() =>
                            MessageBox.Show(owner, "Crash report saved locally.", "Saved",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)));
                    } else {
                        MessageBox.Show("Crash report saved locally.", "Saved",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            } else {
                var report = CrashReportStore.BuildCrashReport(source, exception, userComment: "");
                CrashReportStore.SaveReportLocally(report);
            }
        }
        catch {
            try { Console.Error.WriteLine(exception.ToString()); }
            catch { }
        }
        finally {
            if (_shutdownHandler is not null) {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var task = _shutdownHandler();
                var completed = await Task.WhenAny(task, Task.Delay(Timeout.Infinite, cts.Token));
                if (completed != task)
                    Environment.Exit(1);
            } else {
                Environment.Exit(1);
            }
        }
    }
}
