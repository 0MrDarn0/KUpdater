// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.UI {
    public static class UIResources {
        public static string PathFor(string fileName) =>
            Path.Combine(AppContext.BaseDirectory, "kUpdater", "Resources", fileName);
    }
}
