namespace Loupedeck.SubtitleEditPlugin
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Drives Subtitle Edit from a Logitech device by sending the keyboard shortcuts
    /// Subtitle Edit is already configured to listen for. The shortcuts are read from
    /// the user's own Settings.json, so remapping a shortcut in Subtitle Edit is
    /// picked up here without touching the plugin.
    ///
    /// Actions are sent to the focused application, so Subtitle Edit must be in front.
    /// </summary>
    public class SubtitleEditPlugin : Plugin
    {
        // Actions are keyboard shortcuts, not calls into an application API.
        public override Boolean UsesApplicationApiOnly => false;

        // The plugin is tied to Subtitle Edit and activates when it comes forward.
        public override Boolean HasNoApplication => false;

        private static readonly Object BindingsLock = new Object();
        private static Dictionary<String, SeBinding> _bindings = new Dictionary<String, SeBinding>(StringComparer.Ordinal);
        private static Boolean _loaded;

        public SubtitleEditPlugin()
        {
            PluginLog.Init(this.Log);
        }

        public override void Load()
        {
            // The plugin service can load the assembly without setting Assembly.Location,
            // so point the icon loader at the reliable assembly path.
            SeIconLoader.Initialize(this.AssemblyFilePath);
            ReloadBindings();

            PluginLog.Info("Icon folder: " + (SeIconLoader.FolderPath ?? "NOT FOUND"));
        }

        public override void Unload()
        {
        }

        /// <summary>Re-reads Settings.json. Called on load and by the reload action.</summary>
        public static void ReloadBindings()
        {
            var success = SeShortcuts.TryLoad(out var bindings);
            lock (BindingsLock)
            {
                if (success)
                {
                    _bindings = bindings;
                }
                else
                {
                    PluginLog.Warning("Could not reload shortcuts; retaining last known-good bindings");
                }

                _loaded = true;
            }
        }

        /// <summary>
        /// Loads the shortcuts if they have not been read yet. Actions may be built
        /// before Load() runs, and an action list built from an empty map would leave
        /// the plugin with nothing to offer, so every entry point goes through here.
        /// </summary>
        internal static void EnsureLoaded()
        {
            lock (BindingsLock)
            {
                if (_loaded)
                {
                    return;
                }
            }

            ReloadBindings();
        }

        internal static IReadOnlyCollection<SeBinding> GetBindings()
        {
            EnsureLoaded();
            lock (BindingsLock)
            {
                return new List<SeBinding>(_bindings.Values);
            }
        }

        internal static Boolean TryGetBinding(String actionName, out SeBinding binding)
        {
            EnsureLoaded();
            lock (BindingsLock)
            {
                return _bindings.TryGetValue(actionName, out binding);
            }
        }
    }
}
