using SnippetIO;
using SnippetIOApi;

namespace GlobalLowLevelHooks;

class Program
{
    [STAThread] // Required for Clipboard operations
    static void Main()
    {
        KeyboardHook.Run();
    }
}


