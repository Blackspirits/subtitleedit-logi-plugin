namespace Loupedeck.SubtitleEditPlugin
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// One plugin action per Subtitle Edit shortcut that has keys assigned. Pressing
    /// the button sends that shortcut to the focused application.
    ///
    /// The name is kept short on purpose. Each action symbol file is named after this
    /// class plus the parameter name, and a tar entry name cannot exceed 100
    /// characters, which the longest Subtitle Edit action name would otherwise break.
    ///
    /// The parameters are built from the settings file, so an action the user has not
    /// bound in Subtitle Edit never appears here: there would be no keystroke to send.
    /// </summary>
    public class SeCommand : PluginDynamicCommand
    {
        public SeCommand()
            : base()
        {
        }

        protected override Boolean OnLoad()
        {
            var bindings = new List<SeBinding>(SubtitleEditPlugin.GetBindings());
            bindings.Sort((a, b) => String.CompareOrdinal(
                SeCatalog.GetDisplayName(a.ActionName),
                SeCatalog.GetDisplayName(b.ActionName)));

            foreach (var binding in bindings)
            {
                this.AddParameter(
                    binding.ActionName,
                    SeCatalog.GetDisplayName(binding.ActionName),
                    SeCatalog.GetGroupName(binding.ActionName, binding.ControlName));
            }

            PluginLog.Info($"Subtitle Edit plugin exposed {bindings.Count} actions");
            return true;
        }

        protected override void RunCommand(String actionParameter)
        {
            if (String.IsNullOrEmpty(actionParameter))
            {
                return;
            }

            if (!SubtitleEditPlugin.TryGetBinding(actionParameter, out var binding))
            {
                PluginLog.Warning($"No Subtitle Edit shortcut bound for {actionParameter}");
                return;
            }

            try
            {
                this.Plugin.ClientApplication.SendKeyboardShortcut(binding.KeyCode, binding.Modifiers);
                PluginLog.Verbose($"Sent {binding.KeysText} for {binding.ActionName}");
            }
            catch (Exception e)
            {
                PluginLog.Error(e, $"Could not send shortcut for {binding.ActionName}");
            }
        }

        // Draw the action's icon on the device button, for example the Actions Ring
        // bubble. Without this the service draws the action name as text. Falls back to
        // the name when the action has no symbol.
        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var image = SeIconLoader.Image($"Loupedeck.SubtitleEditPlugin.SeCommand___{actionParameter}.svg");
            return image ?? base.GetCommandImage(actionParameter, imageSize);
        }
    }
}
