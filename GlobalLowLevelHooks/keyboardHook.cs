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
    private static DateTime lastKeyPressTime = DateTime.MinValue;
    private static int keyPressDelayMs = 100;
    private static bool isCopying = false;
    private static bool isReplacing = false;
    // Snippet API and mapping dictionary.
    static readonly SnippetIOApi.ISnippetIO s_SnippetIO = SnippetIOApi.Factory.Get();
    // Dictionary: composite key "word|normalizedKeyCombo" -> snippet code.
    private static Dictionary<string, string> shortcutMap;

    [STAThread] // For Clipboard
    public static void Run()
    {
        // Load snippets and create mapping
        shortcutMap = Helper.SnippetIOUtil.CreateShortcutMapping(s_SnippetIO.ReadAll().ToList());
        s_SnippetIO.AddObserver(CodeSnippetsListObserver);
        _hookID = SetHook(_proc);
        Application.Run();
        UnhookWindowsHookEx(_hookID);

    }

    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        // Process only keydown events
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN && !isReplacing)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            Keys key = (Keys)vkCode;

            // When we've collected exactly three unique keys and one is a modifier...
            if (collectedKeys.Count == 3 && collectedKeys.Any(k => IsModifier(k)))
            {
                handleKeys(nCode, wParam, lParam);
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
            else if (key == Keys.Back && lastWord.Length > 0)
            {
                lastWord.Remove(lastWord.Length - 1, 1);
                lastKeyPressTime = DateTime.Now;
                return CallNextHookEx(_hookID, nCode, wParam, lParam);
            }
            else if (key == Keys.Space || key == Keys.Enter)
            {
                lastWord.Clear();
                lastKeyPressTime = DateTime.Now;
                return CallNextHookEx(_hookID, nCode, wParam, lParam);
            }
            // (We no longer clear on Space/Enter so that the word remains until a shortcut is applied.)

            // Always add modifier keys when pressed
            if (IsModifier(key) && !collectedKeys.Contains(key))
            {
                // Ensure there are no other modifiers already added to the list
                if (!collectedKeys.Any(k => IsModifier(k)))
                {
                    collectedKeys.Add(key);
                    Console.WriteLine($"Added modifier: {key}");
                    if (collectedKeys.Count == 3)
                    {
                        handleKeys(nCode, wParam, lParam);
                    }
                }
            }
            // Add non-modifier keys only if at least one modifier is already pressed
            else if (!IsModifier(key) && !collectedKeys.Contains(key) &&
                collectedKeys.Count < 3 && key != Keys.Back)
            {
                if (collectedKeys.Count == 2)
                {
                    if (collectedKeys.Any(k => IsModifier(k)))
                    {
                        collectedKeys.Add(key);
                        Console.WriteLine($"Added modifier: {key}");
                    }
                    if (collectedKeys.Count == 3 && collectedKeys.Any(k => IsModifier(k)))
                    {
                        handleKeys(nCode, wParam, lParam);
                    }
                }
                else
                {
                    collectedKeys.Add(key);
                    Console.WriteLine($"Added modifier: {key}");
                }

            }

        }

        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    // Returns true if the key is a modifier (Control, Shift, Alt)
    private static bool IsModifier(Keys key)
    {
        return key == Keys.LControlKey || key == Keys.RControlKey ||
               key == Keys.LShiftKey || key == Keys.RShiftKey ||
               key == Keys.LMenu || key == Keys.RMenu;
    }

    // Replaces the currently typed word (removing it by sending backspaces equal to its length) and pastes the replacement.
    private static void ReplaceWord(string replacement)
    {
        isReplacing = true;
        // Capture the current length of the word before doing anything
        int wordLength = lastWord.Length;
        Console.WriteLine($"Replacing word with length: {wordLength}");

        // Place the replacement text into the clipboard.
        Clipboard.SetText(replacement);

        // Send backspace keystrokes one by one with a short delay.
        for (int i = 0; i++ < wordLength; i++)
        {
            SendKeys.SendWait("{BACKSPACE}");
        }

        // Paste the replacement text.
        SendKeys.SendWait("^v"); // Ctrl+V

        // Clear the word buffer.
        lastWord.Clear();
        isReplacing = false;

    }

    

    private static IntPtr handleKeys(int nCode, IntPtr wParam, IntPtr lParam)
    {
        foreach (var k in collectedKeys)
        {
            Console.WriteLine($"Collected key: {k}");
        }
        Console.WriteLine("lastWord: " + lastWord);
        // Separate modifier and non-modifier keys.
        Keys modifier = collectedKeys.FirstOrDefault(k => IsModifier(k));
        var nonModifiers = collectedKeys.Where(k => !IsModifier(k)).ToList();
        // We require exactly two non-modifiers.
        if (nonModifiers.Count == 2)
        {
            // Build two candidate key combos.
            string candidate1 = $"{modifier}+{nonModifiers[0]}+{nonModifiers[1]}";
            string candidate2 = $"{modifier}+{nonModifiers[1]}+{nonModifiers[0]}";
            string currentWord = lastWord.ToString().ToLower();
            string compositeKey1 = $"{currentWord}|{candidate1}";
            string compositeKey2 = $"{currentWord}|{candidate2}";

            Console.WriteLine($"Checking composite keys: {compositeKey1} or {compositeKey2}");
            if (shortcutMap.TryGetValue(compositeKey1, out string? value))
            {
                Console.WriteLine($"Found mapping for {compositeKey1}");
                collectedKeys.Clear();
                isCopying = true;
                ReplaceWord(value);
            }
            else if (shortcutMap.TryGetValue(compositeKey2, out string? value1))
            {
                Console.WriteLine($"Found mapping for {compositeKey2}");
                collectedKeys.Clear();
                isCopying = true;
                ReplaceWord(value1);
            }
        }
        while (isCopying && !isReplacing) // backspace to clean the last character 
        {
            Thread.Sleep(300);
            Console.WriteLine(lastWord);
            collectedKeys.Clear();
            isCopying = false;
            SendKeys.SendWait("{BACKSPACE}");
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
        // In either case, reset collected keys.
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }
    #region Observer Operations
    private static void QueryCodeSnippetsList()
    {
        shortcutMap = Helper.SnippetIOUtil.CreateShortcutMapping(s_SnippetIO.ReadAll().ToList());
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
