using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helper;

public static class KeyboardHookUtils
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
                List<Keys> keyList = Helper.SnippetIOUtils.ParseKeyShortcut(snippet.KeyShortcut).ToList();
                // Join them with "+" in the order they appear.
                string normalizedKeyCombo = string.Join("+", keyList);
                string compositeKey = $"{snippet.WordShortcut.ToLower()}|{normalizedKeyCombo}";
                mapping[compositeKey] = snippet.Code;
                Console.WriteLine($"Mapping: {compositeKey} -> {snippet.Code}");
            }
        }
        return mapping;
    }

}
