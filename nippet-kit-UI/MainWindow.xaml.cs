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
            e.Handled = true; // מונע הכנסת טקסט לתיבה

            if (e.Key == Key.System || e.Key == Key.LWin || e.Key == Key.RWin)
                return;

            if (shortcutBuilder.Length > 0)
                shortcutBuilder.Append(" + ");

            shortcutBuilder.Append(e.Key.ToString());
            NewShortcutBox.Text = shortcutBuilder.ToString();
        }
    }
}
