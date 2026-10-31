namespace Loupedeck.SubtitleEditPlugin
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;

    /// <summary>
    /// Loads an action's SVG so it can be drawn on the device (the Actions Ring bubble
    /// and console keys). The action symbols in the package draw the icon next to the
    /// name in the configuration list; the button image on the device is separate and
    /// comes from GetCommandImage / GetAdjustmentImage, which is what uses this.
    ///
    /// The same SVG files serve both, so they are read from the package's actionsymbols
    /// folder rather than duplicated. Results are cached; a missing file returns null so
    /// the caller falls back to drawing the action name.
    /// </summary>
    internal static class SeIconLoader
    {
        private static String _folder;
        private static readonly ConcurrentDictionary<String, String> Cache =
            new ConcurrentDictionary<String, String>(StringComparer.Ordinal);

        /// <summary>The resolved actionsymbols folder, or null. For diagnostics.</summary>
        public static String FolderPath => _folder;

        /// <summary>
        /// Finds the actionsymbols folder from the plugin's assembly path. The plugin
        /// service can load the assembly without setting Assembly.Location, so the path
        /// is passed in from Plugin.AssemblyFilePath, which is reliable. Falls back to
        /// the assembly location and the app base directory.
        /// </summary>
        public static void Initialize(String assemblyFilePath)
        {
            Cache.Clear();
            _loggedFirst = false;
            _folder = null;
            foreach (var baseDir in new[]
            {
                String.IsNullOrEmpty(assemblyFilePath) ? null : Path.GetDirectoryName(assemblyFilePath),
                String.IsNullOrEmpty(typeof(SeIconLoader).Assembly.Location)
                    ? null
                    : Path.GetDirectoryName(typeof(SeIconLoader).Assembly.Location),
                AppContext.BaseDirectory,
            })
            {
                if (String.IsNullOrEmpty(baseDir))
                {
                    continue;
                }

                // The plugin dll lives in bin/, with actionsymbols/ next to it.
                var sibling = Path.GetFullPath(Path.Combine(baseDir, "..", "actionsymbols"));
                if (Directory.Exists(sibling))
                {
                    _folder = sibling;
                    return;
                }

                var here = Path.Combine(baseDir, "actionsymbols");
                if (Directory.Exists(here))
                {
                    _folder = here;
                    return;
                }
            }

            _folder = null;
        }

        /// <summary>Returns the SVG for a symbol file name, or null if there is none.</summary>
        public static String LoadSvg(String fileName)
        {
            if (_folder == null || String.IsNullOrWhiteSpace(fileName) ||
                !String.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal) ||
                !fileName.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (Cache.TryGetValue(fileName, out var cached))
            {
                return cached;
            }

            try
            {
                var path = Path.Combine(_folder, fileName);
                if (!File.Exists(path))
                {
                    return null; // ConcurrentDictionary cannot cache null values.
                }

                var svg = File.ReadAllText(path);
                Cache.TryAdd(fileName, svg);
                return svg;
            }
            catch (Exception e)
            {
                PluginLog.Warning($"Could not read action symbol {fileName}: {e.Message}");
                return null;
            }
        }

        private static Boolean _loggedFirst;

        /// <summary>Draws the SVG for an action, or null to let the service draw the name.</summary>
        public static BitmapImage Image(String symbolFileName)
        {
            var svg = LoadSvg(symbolFileName);
            if (String.IsNullOrEmpty(svg))
            {
                return null;
            }

            try
            {
                var image = BitmapImage.FromSvg(svg);
                if (!_loggedFirst)
                {
                    _loggedFirst = true;
                    PluginLog.Info($"First action image drawn: {symbolFileName} -> {(image != null ? "ok" : "null")}");
                }

                return image;
            }
            catch (Exception e)
            {
                PluginLog.Warning($"Could not render action symbol {symbolFileName}: {e.Message}");
                return null;
            }
        }
    }
}
