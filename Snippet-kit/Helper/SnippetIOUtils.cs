using SnippetIO;
using SnippetIOApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Helper;

public static class SnippetIOUtils
{
    // Parse a KeyShortcut string (e.g. "moifier + key1 + key2") into a list of Keys.
    public static IEnumerable<Keys> ParseKeyShortcut(string keyShortcut)
    {
        var tokens = keyShortcut.Split(new char[] { '+', ',' }, StringSplitOptions.RemoveEmptyEntries);
        var keys = new List<Keys>();
        foreach (var token in tokens)
        {
            string trimmed = token.Trim();
            switch (trimmed.ToLower())
            {
                case "leftcontrolkey":
                case "leftcontrol":
                case "leftctrlkey":
                case "lcontrolkey":
                case "lctrlkey":
                case "leftctrl":
                case "lcontrol":
                case "lctrl":
                case "ctrl":
                case "control":
                    keys.Add(Keys.LControlKey);
                    break;
                case "leftshiftkey":
                case "leftshift":
                case "lshiftkey":
                case "lshift":
                case "shift":
                    keys.Add(Keys.LShiftKey);
                    break;
                case "leftaltkey":
                case "leftalt":
                case "laltkey":
                case "lalt":
                case "alt":
                    keys.Add(Keys.LMenu);
                    break;
                case "rightcontrolkey":
                case "rightcontrol":
                case "rightctrlkey":
                case "rcontrolkey":
                case "rightctrl":
                case "rctrlkey":
                case "rcontrol":
                case "rctrl":
                    keys.Add(Keys.RControlKey); //shift
                    break;
                case "rightshiftkey":
                case "rightshift":
                case "rshiftkey":
                case "rshift":
                    keys.Add(Keys.RShiftKey);
                    break;
                case "rightaltkey":
                case "rightalt":
                case "raltkey":
                case "ralt":
                    keys.Add(Keys.RMenu);
                    break;
                default:
                    if (Enum.TryParse(trimmed, true, out Keys parsedKey))
                        keys.Add(parsedKey);
                    break;
            }
        }
        return keys;
    }
    public static string NormalizeKeyShortcut(string keyShortcut)
    {
        List<Keys> keys = Helper.SnippetIOUtils.ParseKeyShortcut(keyShortcut).ToList();

        if (keys.Count != 3)
        {
            throw new Exception("KeyShortcut must contain exactly three keys.");
        }

        // Find the modifier key and its index
        int modifierIndex = -1;
        for (int i = 0; i < keys.Count; i++)
        {
            if (Helper.SnippetIOUtils.IsModifier(keys[i]))
            {
                if (modifierIndex != -1) // More than one modifier found
                {
                    throw new Exception("Only one modifier key is allowed in KeyShortcut.");
                }
                modifierIndex = i;
            }
        }

        if (modifierIndex == -1) // No modifier found
        {
            throw new Exception("KeyShortcut must contain a modifier key.");
        }

        // Move the modifier to the first position
        if (modifierIndex != 0)
        {
            Keys modifierKey = keys[modifierIndex];
            keys.RemoveAt(modifierIndex);
            keys.Insert(0, modifierKey);
        }

        // Sort the non-modifier keys
        // This is necessary because the order of non-modifier keys is not important(eg. Ctrl + A + B == Ctrl + B + A)
        List<Keys> nonModifierKeys = keys.Skip(1).ToList();
        nonModifierKeys.Sort();

        return $"{keys[0]} + {nonModifierKeys[0]} + {nonModifierKeys[1]}"; // Ensure correct formatting (eg. modifier + Key1 + Key2)
    }
    public static bool IsForbidden(Keys key)
    {
        return key == Keys.Back || key == Keys.CapsLock ||
               key == Keys.Tab || key == Keys.LWin ||
               key == Keys.RWin || key == Keys.Enter || key == Keys.Oemplus;
    }
    // Returns true if the key is a modifier (Control, Shift, Alt)
    public static bool IsModifier(Keys key)
    {
        return key == Keys.LControlKey || key == Keys.RControlKey ||
               key == Keys.LShiftKey || key == Keys.RShiftKey ||
               key == Keys.LMenu || key == Keys.RMenu;
    }

}
