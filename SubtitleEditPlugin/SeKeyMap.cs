namespace Loupedeck.SubtitleEditPlugin
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Translates the key names Subtitle Edit stores in its settings into the key and
    /// modifier values the plugin service sends.
    ///
    /// Subtitle Edit uses Avalonia key names ("A", "D4", "Up", "Return"), while the
    /// plugin API uses its own names ("KeyA", "Key4", "ArrowUp", "Return"). The
    /// primary modifier is stored as "Win" on macOS and "Ctrl" on Windows, which is
    /// what ShortcutsMain.GetCommandOrWin() produces. ModifierKey.Windows and
    /// ModifierKey.Command are the same flag, so "Win" maps correctly on both.
    /// </summary>
    internal static class SeKeyMap
    {
        private static readonly Dictionary<String, ModifierKey> Modifiers =
            new Dictionary<String, ModifierKey>(StringComparer.OrdinalIgnoreCase)
            {
                { "Ctrl", ModifierKey.Control },
                { "Control", ModifierKey.Control },
                { "Alt", ModifierKey.Alt },
                { "Option", ModifierKey.Alt },
                { "Shift", ModifierKey.Shift },
                { "Win", ModifierKey.Windows },
                { "Cmd", ModifierKey.Windows },
                { "Command", ModifierKey.Windows },
                { "Meta", ModifierKey.Windows },
            };

        // Only the names that differ from the plugin API spelling. Everything else
        // (Delete, Insert, Back, Tab, Space, Home, End, PageUp, PageDown, F1..F24,
        // Add, Subtract, Multiply, Divide) already matches and falls through to
        // Enum.TryParse below.
        private static readonly Dictionary<String, String> Aliases =
            new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase)
            {
                { "Up", "ArrowUp" },
                { "Down", "ArrowDown" },
                { "Left", "ArrowLeft" },
                { "Right", "ArrowRight" },
                { "Enter", "Return" },
                { "Esc", "Escape" },
                { "OemPeriod", "Period" },
                { "OemComma", "Comma" },
                { "OemMinus", "Minus" },
                { "OemPlus", "Equals" },
                { "Backspace", "Back" },
            };

        public static Boolean IsModifier(String token) => Modifiers.ContainsKey(token);

        /// <summary>
        /// Parses a Subtitle Edit key list such as ["Win", "Shift", "B"] into the
        /// modifiers and the single main key. Returns false when the list holds no
        /// main key, which is how Subtitle Edit stores an unassigned shortcut.
        /// </summary>
        public static Boolean TryParse(IReadOnlyList<String> keys, out VirtualKeyCode keyCode, out ModifierKey modifiers)
        {
            keyCode = VirtualKeyCode.None;
            modifiers = ModifierKey.None;

            if (keys == null || keys.Count == 0)
            {
                return false;
            }

            var haveKey = false;
            foreach (var raw in keys)
            {
                var token = (raw ?? String.Empty).Trim();
                if (token.Length == 0)
                {
                    continue;
                }

                if (Modifiers.TryGetValue(token, out var modifier))
                {
                    modifiers |= modifier;
                    continue;
                }

                // Exactly one non-modifier key per shortcut.
                if (haveKey || !TryParseKey(token, out keyCode))
                {
                    return false;
                }

                haveKey = true;
            }

            return haveKey;
        }

        private static Boolean TryParseKey(String token, out VirtualKeyCode keyCode)
        {
            keyCode = VirtualKeyCode.None;

            if (Aliases.TryGetValue(token, out var alias))
            {
                return ParseDefinedKey(alias, out keyCode);
            }

            // "A".."Z"
            if (token.Length == 1 && Char.IsLetter(token[0]))
            {
                return ParseDefinedKey("Key" + Char.ToUpperInvariant(token[0]), out keyCode);
            }

            // "0".."9"
            if (token.Length == 1 && Char.IsDigit(token[0]))
            {
                return ParseDefinedKey("Key" + token, out keyCode);
            }

            // Avalonia spells the number row "D0".."D9"
            if (token.Length == 2 && (token[0] == 'D' || token[0] == 'd') && Char.IsDigit(token[1]))
            {
                return ParseDefinedKey("Key" + token[1], out keyCode);
            }

            // Delete, Insert, Back, Tab, Space, Return, Escape, Home, End,
            // PageUp, PageDown, F1..F24, Add, Subtract, ...
            return ParseDefinedKey(token, out keyCode);
        }

        private static Boolean ParseDefinedKey(String name, out VirtualKeyCode keyCode)
        {
            return Enum.TryParse(name, ignoreCase: true, out keyCode) &&
                   keyCode != VirtualKeyCode.None &&
                   Enum.IsDefined(typeof(VirtualKeyCode), keyCode);
        }
    }
}
