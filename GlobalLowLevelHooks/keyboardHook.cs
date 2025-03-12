using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace GlobalLowLevelHooks;

class KeyboardHook
{
    // Buffer for the typed word (letters/digits)
    private static StringBuilder lastWord = new StringBuilder();
    // Collect unique keys (for the shortcut combination)
    private static List<Keys> collectedKeys = new List<Keys>();

    // Standard hook variables
    private static LowLevelKeyboardProc _proc = HookCallback;
    private static IntPtr _hookID = IntPtr.Zero;

    // Variables for key press delay and replacement flag
    private static DateTime lastKeyPressTime = DateTime.MinValue;
    private static readonly int keyPressDelayMs = 100;
    private static bool isReplacing = false;

    // Snippet API and mapping dictionary.
    static readonly SnippetIOApi.ISnippetIO s_SnippetIO = SnippetIOApi.Factory.Get();
    // Dictionary: composite key "word|normalizedKeyCombo" -> snippet code.
    private static Dictionary<string, string>? shortcutMap;

    [STAThread] // For Clipboard
    public static void Run()
    {
        // Load snippets and create mapping
        shortcutMap = Helper.KeyboardHookUtils.CreateShortcutMapping(s_SnippetIO.ReadAll().ToList());
        if(shortcutMap == null || shortcutMap.Count == 0)
        {
            throw new Exception("No snippets found");
        }
        s_SnippetIO.AddObserver(CodeSnippetsListObserver);
        _hookID = SetHook(_proc);
        Application.Run();
        UnhookWindowsHookEx(_hookID);

    }

    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule? curModule = curProcess.MainModule)
        {
            if(curModule == null)
            {
                throw new Exception("Failed to get current module");
            }
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        // Process only keydown events, AND DONT PROCCESS IF WE ARE REPLACING
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN && !isReplacing)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            Keys key = (Keys)vkCode;

            // When we've collected exactly three unique keys and one is a modifier...
            if (collectedKeys.Count == 3 && collectedKeys.Any(key => Helper.SnippetIOUtils.IsModifier(key)))
            {
                HandleKeys(nCode, wParam, lParam);
            }
            // Always add alphanumeric keys to our word buffer
            if (char.IsLetterOrDigit((char)vkCode) && !Control.ModifierKeys.HasFlag(Keys.Control)
                && !Control.ModifierKeys.HasFlag(Keys.Shift) && !Control.ModifierKeys.HasFlag(Keys.Alt))
            {
                // Introduce a lag if key presses come too fast.
                TimeSpan dt = DateTime.Now - lastKeyPressTime;
                if (dt.TotalMilliseconds >= keyPressDelayMs)
                {
                    lastWord.Append((char)vkCode);
                    lastKeyPressTime = DateTime.Now;
                }
                return CallNextHookEx(_hookID, nCode, wParam, lParam);
            }
            // delete last character on backspace
            else if (key == Keys.Back && lastWord.Length > 0)
            {
                lastWord.Remove(lastWord.Length - 1, 1);
                lastKeyPressTime = DateTime.Now;
                return CallNextHookEx(_hookID, nCode, wParam, lParam);
            }
            // clear on Space/Enter
            else if (key == Keys.Space || key == Keys.Enter)
            {
                collectedKeys.Clear();
                lastWord.Clear();
                lastKeyPressTime = DateTime.Now;
                return CallNextHookEx(_hookID, nCode, wParam, lParam);
            }
            // Always add modifier keys when pressed
            if (Helper.SnippetIOUtils.IsModifier(key) && !collectedKeys.Contains(key))
            {
                // Ensure there are no other modifiers already added to the list
                if (!collectedKeys.Any(key => Helper.SnippetIOUtils.IsModifier(key)))
                {
                    collectedKeys.Add(key);
                    Console.WriteLine($"Added modifier: {key}");
                    if (collectedKeys.Count == 3)
                    {
                        HandleKeys(nCode, wParam, lParam);
                    }
                }
            }
            // Add non-modifier keys only if at least one modifier is already pressed
            else if (!Helper.SnippetIOUtils.IsModifier(key) && !collectedKeys.Contains(key) &&
                collectedKeys.Count < 3 && !Helper.SnippetIOUtils.IsForbidden(key))
            {
                if (collectedKeys.Count == 2)
                {
                    // make sure we arent adding a non modifier key as the third key(and we have a no modifier key)
                    if (collectedKeys.Any(key => Helper.SnippetIOUtils.IsModifier(key)))
                    {
                        collectedKeys.Add(key);
                        Console.WriteLine($"Added Key: {key}");
                    }
                    if (collectedKeys.Count == 3)
                    {
                        HandleKeys(nCode, wParam, lParam);
                    }
                }
                else
                {
                    collectedKeys.Add(key);
                    Console.WriteLine($"Added Key: {key}");
                }

            }

        }

        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }
   
    // Replaces the currently typed word (removing it by sending backspaces equal to its length) and pastes the replacement.
    private static void ReplaceWord(string replacement, string deleteuntil)
    {
        isReplacing = true;
        int wordLength = deleteuntil.Length;
        Console.WriteLine($"Replacing word with length: {wordLength}");

        // Place the replacement text into the clipboard.
        Clipboard.SetText(replacement);

        // Send backspace keystrokes one by one with a short delay.
        for (int i = 0; i++ < wordLength; i++)
        {
            SendKeys.SendWait("{BACKSPACE}");
            Console.WriteLine("Backspace");
        }

        // Paste the replacement text.
        SendKeys.SendWait("^v"); // Ctrl+V

        // Clear the word buffer.
        lastWord.Clear();
        isReplacing = false;
    }

    

    private static IntPtr HandleKeys(int nCode, IntPtr wParam, IntPtr lParam)
    {
        foreach (var k in collectedKeys)
        {
            Console.WriteLine($"Collected key: {k}");
        }
        Console.WriteLine("lastWord: " + lastWord);
        // Separate modifier and non-modifier keys.
        Keys modifier = collectedKeys.FirstOrDefault(key => Helper.SnippetIOUtils.IsModifier(key));
        var nonModifiers = collectedKeys.Where(key => !Helper.SnippetIOUtils.IsModifier(key)).ToList();
        // We require exactly two non-modifiers.
        if (nonModifiers.Count == 2)
        {
            // Build two candidate key combos.
            string candidate1 = $"{modifier}+{nonModifiers[0]}+{nonModifiers[1]}";
            string candidate2 = $"{modifier}+{nonModifiers[1]}+{nonModifiers[0]}";
            if (candidate1 == "LControlKey+R+G" || candidate2 == "LControlKey+R+G")
            {
                collectedKeys.Clear();
                return CallNextHookEx(_hookID, nCode, wParam, lParam);
            }

            string currentWord = lastWord.ToString().ToLower();
            while (currentWord.Length > 0)
            {
                string compositeKey1 = $"{currentWord}|{candidate1}";
                string compositeKey2 = $"{currentWord}|{candidate2}";

                Console.WriteLine($"Checking composite keys: {compositeKey1} or {compositeKey2}");
                if (shortcutMap!.TryGetValue(compositeKey1, out string? value))
                {
                    Console.WriteLine($"Found mapping for {compositeKey1}");
                    collectedKeys.Clear();
                    ReplaceWord(value, currentWord);
                    break;
                }
                else if (shortcutMap.TryGetValue(compositeKey2, out string? value1))
                {
                    Console.WriteLine($"Found mapping for {compositeKey2}");
                    collectedKeys.Clear();
                    ReplaceWord(value1, currentWord);
                    break;
                }
                else
                {
                    if (currentWord.Length > 1) // Check if there's at least 2 characters left
                    {
                        currentWord = currentWord.Substring(1); // Take substring from index 1 to the end
                    }
                    else
                    {
                        collectedKeys.Clear();
                        break; // Break the loop if currentWord has only 1 or 0 characters left
                    }
                }
            }
        }
        while (!isReplacing) // backspace to clean the last character 
        {
            Thread.Sleep(300);
            Console.WriteLine(lastWord);
            collectedKeys.Clear();
            SendKeys.SendWait("{BACKSPACE}");
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
        // In either case, reset collected keys.
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }
    #region Observer Operations
    private static void QueryCodeSnippetsList()
    {
        shortcutMap = Helper.KeyboardHookUtils.CreateShortcutMapping(s_SnippetIO.ReadAll().ToList());
        if (shortcutMap == null || shortcutMap.Count == 0)
        {
            throw new Exception("No snippets found");
        }
    }
    private static void CodeSnippetsListObserver() => QueryCodeSnippetsList();
    #endregion Observer Operations
    #region Windows API Imports
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
    #endregion
}
