using SnippetIO;
using SnippetIOApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helper;

public static class SnippetIOUtil
{
    // Create a dictionary mapping composite keys to snippet code.
    // Composite key format: "{wordShortcut}|{normalizedKeyCombo}"
    public static Dictionary<string, string> CreateShortcutMapping(List<SnippetIO.CodeSnippet> snippets)
    {
        var mapping = new Dictionary<string, string>();
        foreach (var snippet in snippets)
        {
            // Only add if both word and key shortcuts are provided.
            if (!string.IsNullOrWhiteSpace(snippet.WordShortcut) &&
                !string.IsNullOrWhiteSpace(snippet.KeyShortcut))
            {
                // Parse the snippet's KeyShortcut into a list of Keys.
                List<Keys> keyList = ParseKeyShortcut(snippet.KeyShortcut);
                // Join them with "+" in the order they appear.
                string normalizedKeyCombo = string.Join("+", keyList);
                string compositeKey = $"{snippet.WordShortcut.ToLower()}|{normalizedKeyCombo}";
                mapping[compositeKey] = snippet.Code;
                Console.WriteLine($"Mapping: {compositeKey} -> {snippet.Code}");
            }
        }
        return mapping;
    }

    // Parse a KeyShortcut string (e.g. "LeftCtrl + R + B") into a list of Keys.
    public static List<Keys> ParseKeyShortcut(string keyShortcut)
    {
        var tokens = keyShortcut.Split(new char[] { '+', ',' }, StringSplitOptions.RemoveEmptyEntries);
        var keys = new List<Keys>();
        foreach (var token in tokens)
        {
            string trimmed = token.Trim();
            switch (trimmed.ToLower())
            {
                case "leftctrl":
                case "ctrl":
                case "control":
                    keys.Add(Keys.LControlKey);
                    break;
                case "leftshift":
                case "shift":
                    keys.Add(Keys.LShiftKey);
                    break;
                case "leftalt":
                case "alt":
                    keys.Add(Keys.LMenu);
                    break;
                default:
                    if (Enum.TryParse(trimmed, true, out Keys parsedKey))
                        keys.Add(parsedKey);
                    break;
            }
        }
        return keys;
    }
}
