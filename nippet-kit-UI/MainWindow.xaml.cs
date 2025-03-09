using System;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace SnippetIO
{
    public partial class MainWindow : Window
    {
        private StringBuilder shortcutBuilder = new StringBuilder();

        public MainWindow()
        {
            InitializeComponent();
            NewShortcutBox.PreviewKeyDown += OnShortcutKeyDown;
        }

        private void OnShortcutKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true; // Prevents text from being added to the text box

            // If the pressed key is a system key or a Windows key, do nothing
            if (e.Key == Key.System || e.Key == Key.LWin || e.Key == Key.RWin)
                return;

            // If the pressed key is Backspace, remove the last added shortcut
            if (e.Key == Key.Back && shortcutBuilder.Length > 0)
            {
                int lastPlusIndex = shortcutBuilder.ToString().LastIndexOf(" + ");
                if (lastPlusIndex != -1)
                {
                    shortcutBuilder.Remove(lastPlusIndex, shortcutBuilder.Length - lastPlusIndex);
                }
                else
                {
                    shortcutBuilder.Clear(); // If no " + " found, clear the whole string
                }
            }
            else
            {
                // If not Backspace, add the pressed key to the shortcut string
                if (shortcutBuilder.Length > 0)
                    shortcutBuilder.Append(" + ");

                shortcutBuilder.Append(e.Key.ToString());
            }

            // Update the text box with the new shortcut
            NewShortcutBox.Text = shortcutBuilder.ToString();
        }

    }
}
