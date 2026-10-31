using System;
using System.IO;
using System.Collections.Generic;

namespace Loupedeck.SubtitleEditPlugin
{
    [Flags]
    public enum ModifierKey { None = 0, Control = 1, Alt = 2, Shift = 4, Windows = 8 }

    // Minimal SDK stand-in: the actual plugin is compiled against Logitech's SDK.
    public enum VirtualKeyCode
    {
        None, KeyA, KeyB, KeyC, KeyD, KeyX, Key4, ArrowUp, ArrowDown,
        ArrowLeft, ArrowRight, Add, Subtract, Back, Return, Escape, F1
    }

    internal static class PluginLog
    {
        public static void Warning(String message) => Console.Error.WriteLine(message);
        public static void Info(String message) => Console.WriteLine(message);
        public static void Verbose(String message) { }
        public static void Error(Exception exception, String message) =>
            Console.Error.WriteLine(message + ": " + exception.Message);
    }

    internal static class Program
    {
        private static Int32 _assertions;
        private static void Check(Boolean condition, String description)
        {
            _assertions++;
            if (!condition) throw new Exception("FAILED: " + description);
        }

        private static void Keys(String[] input, VirtualKeyCode key, ModifierKey modifiers)
        {
            Check(SeKeyMap.TryParse(input, out var actualKey, out var actualModifiers),
                "Expected valid shortcut: " + String.Join("+", input));
            Check(actualKey == key && actualModifiers == modifiers,
                "Shortcut mismatch: " + String.Join("+", input));
        }

        private static void TestKeyMap()
        {
            Keys(new[] { "Ctrl", "D4" }, VirtualKeyCode.Key4, ModifierKey.Control);
            Keys(new[] { "Win", "Alt", "Shift", "D" },
                VirtualKeyCode.KeyD, ModifierKey.Windows | ModifierKey.Alt | ModifierKey.Shift);
            Keys(new[] { "Up" }, VirtualKeyCode.ArrowUp, ModifierKey.None);
            Keys(new[] { "Ctrl", "Add" }, VirtualKeyCode.Add, ModifierKey.Control);
            Keys(new[] { "Backspace" }, VirtualKeyCode.Back, ModifierKey.None);
            Check(!SeKeyMap.TryParse(new[] { "Ctrl" }, out _, out _), "modifier alone");
            Check(!SeKeyMap.TryParse(new[] { "Ctrl", "A", "B" }, out _, out _), "two main keys");
            Check(!SeKeyMap.TryParse(new[] { "99999" }, out _, out _), "numeric enum spoofing");
            Check(!SeKeyMap.TryParse(new[] { "NotAKey" }, out _, out _), "unknown key");
            Check(!SeKeyMap.TryParse(Array.Empty<String>(), out _, out _), "empty shortcut");
        }

        private static void TestSettings()
        {
            var saved = Environment.GetEnvironmentVariable("SUBTITLE_EDIT_SETTINGS");
            var folder = Path.Combine(Path.GetTempPath(), "se-logi-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            var file = Path.Combine(folder, "Settings.json");

            try
            {
                Environment.SetEnvironmentVariable("SUBTITLE_EDIT_SETTINGS", file);
                File.WriteAllText(file,
                    "{\"shortcuts\":[{\"ActionName\":\"GoToNextLineCommand\",\"Keys\":[\"Ctrl\",\"D4\"]," +
                    "\"ControlName\":\"SubtitleGrid\"}," +
                    "{\"ActionName\":\"Unused\",\"Keys\":[]}," +
                    "{\"ActionName\":123,\"Keys\":[\"A\"]}," +
                    "{\"ActionName\":\"Bad\",\"Keys\":[\"A\",\"B\"]}," +
                    "null]}");
                Check(SeShortcuts.TryLoad(out var bindings), "valid JSON loaded");
                Check(bindings.Count == 1, "invalid and unassigned entries skipped");
                Check(bindings["GoToNextLineCommand"].KeyCode == VirtualKeyCode.Key4,
                    "case-insensitive JSON properties");

                File.WriteAllText(file, "{\"Shortcuts\": [");
                Check(!SeShortcuts.TryLoad(out _), "truncated settings rejected");
                File.WriteAllText(file, "{\"Shortcuts\": {}}");
                Check(!SeShortcuts.TryLoad(out _), "wrong shortcuts shape rejected");
                File.WriteAllText(file, "{\"Shortcuts\": []}");
                Check(SeShortcuts.TryLoad(out bindings) && bindings.Count == 0,
                    "valid empty shortcut list is accepted");
            }
            finally
            {
                Environment.SetEnvironmentVariable("SUBTITLE_EDIT_SETTINGS", saved);
                Directory.Delete(folder, true);
            }
        }

        private static Int32 Main()
        {
            try
            {
                TestKeyMap();
                TestSettings();
                Console.WriteLine("Core regression tests passed (" + _assertions + " assertions).");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
        }
    }
}
