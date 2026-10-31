namespace Loupedeck.SubtitleEditPlugin
{
    using System;

    /// <summary>
    /// A dial that sends one Subtitle Edit shortcut per tick: one action when turned
    /// one way and another when turned the other way. Pressing the dial sends an
    /// optional third action.
    ///
    /// A dial can report many ticks at once when spun quickly. The count is capped so
    /// a fast spin cannot flood the application with keystrokes.
    /// </summary>
    public abstract class SeAdjustmentBase : PluginDynamicAdjustment
    {
        private const Int32 MaxTicksPerEvent = 10;

        private readonly String _forwardAction;
        private readonly String _backAction;
        private readonly String _pressAction;

        protected SeAdjustmentBase(String displayName, String description, String groupName,
            String forwardAction, String backAction, String pressAction)
            : base(displayName, description, groupName, hasReset: pressAction != null)
        {
            this._forwardAction = forwardAction;
            this._backAction = backAction;
            this._pressAction = pressAction;
        }

        protected override void ApplyAdjustment(String actionParameter, Int32 diff)
        {
            if (diff == 0)
            {
                return;
            }

            var action = diff > 0 ? this._forwardAction : this._backAction;
            // Math.Abs(Int32.MinValue) throws; widening first handles every possible diff.
            var ticks = (Int32)Math.Min(Math.Abs((Int64)diff), MaxTicksPerEvent);
            for (var i = 0; i < ticks; i++)
            {
                this.Send(action);
            }
        }

        protected override void RunCommand(String actionParameter)
        {
            if (this._pressAction != null)
            {
                this.Send(this._pressAction);
            }
        }

        protected override String GetAdjustmentValue(String actionParameter) => String.Empty;

        // Draw the dial's icon on the device. The symbol is named after the concrete
        // adjustment class, so the derived type resolves to the right file.
        protected override BitmapImage GetAdjustmentImage(String actionParameter, PluginImageSize imageSize)
        {
            var image = SeIconLoader.Image(this.GetType().FullName + ".svg");
            return image ?? base.GetAdjustmentImage(actionParameter, imageSize);
        }

        private void Send(String actionName)
        {
            if (!SubtitleEditPlugin.TryGetBinding(actionName, out var binding))
            {
                PluginLog.Warning($"No Subtitle Edit shortcut bound for {actionName}, dial does nothing");
                return;
            }

            try
            {
                this.Plugin.ClientApplication.SendKeyboardShortcut(binding.KeyCode, binding.Modifiers);
            }
            catch (Exception e)
            {
                PluginLog.Error(e, $"Could not send dial shortcut for {actionName}");
            }
        }
    }

    /// <summary>Turn to move through the subtitle list, press to focus the selected line.</summary>
    public class SeLineAdjustment : SeAdjustmentBase
    {
        public SeLineAdjustment()
            : base("Subtitle line", "Move to the next or previous subtitle line", "Lines",
                   forwardAction: "GoToNextLineCommand",
                   backAction: "GoToPreviousLineCommand",
                   pressAction: "FocusSelectedLineCommand")
        {
        }
    }

    /// <summary>Turn to scrub the video in 500 ms steps, press to play or pause.</summary>
    public class SeVideoAdjustment : SeAdjustmentBase
    {
        public SeVideoAdjustment()
            : base("Video position", "Move the video back or forward, press to play or pause", "Playback",
                   forwardAction: "Video500MsForwardCommand",
                   backAction: "Video500MsBackCommand",
                   pressAction: "TogglePlayPauseCommand")
        {
        }
    }

    /// <summary>Turn to zoom the waveform.</summary>
    public class SeWaveformZoomAdjustment : SeAdjustmentBase
    {
        public SeWaveformZoomAdjustment()
            : base("Waveform zoom", "Zoom the waveform in or out", "Timing",
                   forwardAction: "WaveformVerticalZoomInCommand",
                   backAction: "WaveformVerticalZoomOutCommand",
                   pressAction: null)
        {
        }
    }

    /// <summary>Turn to step through errors found by Subtitle Edit.</summary>
    public class SeErrorAdjustment : SeAdjustmentBase
    {
        public SeErrorAdjustment()
            : base("Next error", "Move to the next or previous error", "Lines",
                   forwardAction: "GoToNextErrorCommand",
                   backAction: "GoToPreviousErrorCommand",
                   pressAction: "ListErrorsCommand")
        {
        }
    }
}
