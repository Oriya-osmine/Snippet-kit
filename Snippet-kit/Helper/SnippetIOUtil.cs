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
    // Parse a KeyShortcut string (e.g. "LeftCtrl + R + B") into a list of Keys.
    public static IEnumerable<Keys> ParseKeyShortcut(string keyShortcut)
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
    public static bool IsForbidden(Keys key)
    {
        return key == Keys.Back || key == Keys.CapsLock ||
               key == Keys.Tab || key == Keys.LWin ||
               key == Keys.RWin || key == Keys.Enter;
    }
    // Returns true if the key is a modifier (Control, Shift, Alt)
    public static bool IsModifier(Keys key)
    {
        return key == Keys.LControlKey || key == Keys.RControlKey ||
               key == Keys.LShiftKey || key == Keys.RShiftKey ||
               key == Keys.LMenu || key == Keys.RMenu;
    }
    public static void ValidKeyShortcuts(string KeyShortcut)
    {
        List<Keys> keyShortcuts = ParseKeyShortcut(KeyShortcut).ToList();

        int modifierCount = 0;
        foreach (Keys key in keyShortcuts)
        {
            if (Helper.SnippetIOUtil.IsForbidden(key))
            {
                throw new Exception($"The key: {key} is forbidden!");
            }

            if (Helper.SnippetIOUtil.IsModifier(key))
            {
                modifierCount++;
            }
        }
        if (modifierCount > 1)
        {
            throw new Exception("Keyboard shortcuts can only contain one modifier key.");
        }
    }
}
