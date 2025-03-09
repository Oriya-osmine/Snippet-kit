using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace SnippetIO;

public partial class MainWindow : Window
{
    private StringBuilder shortcutBuilder = new StringBuilder();
    static readonly SnippetIOApi.ISnippetIO s_SnippetIO = SnippetIOApi.Factory.Get();

    public MainWindow()
    {
        InitializeComponent();
        NewShortcutBox.PreviewKeyDown += OnShortcutKeyDown;
        QueryCodeSnippetsList();
        SelectedSnippet = CodeSnippetsList.FirstOrDefault() ?? new CodeSnippet { Id = "new snippet", Code = " Waiting for you..",Shortcut = ""};
    }
    #region Window Events
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        s_SnippetIO.AddObserver(CodeSnippetsListObserver);
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        s_SnippetIO.RemoveObserver(CodeSnippetsListObserver);
    }
    #endregion Window Events
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
   
    private void CodeSnippetsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
       // if (SelectedSnippet != null)
        //{
        //    NewShortcutBox.Text = "";
        //    SnippetContentBox.Text = "";
        //    s_SnippetIO.Delete(SelectedSnippet.Id);
       // }
    }
    #region Propetries
    #endregion Propetries
    #region Dependency Properties

    public SnippetIO.CodeSnippet SelectedSnippet
    {
        get { return (SnippetIO.CodeSnippet)GetValue(SelectedSnippetProperty); }
        set { SetValue(SelectedSnippetProperty, value); }
    }
    public static readonly DependencyProperty SelectedSnippetProperty =
    DependencyProperty.Register("SelectedSnippet", typeof(SnippetIO.CodeSnippet), typeof(MainWindow), new PropertyMetadata(null));
    public IEnumerable<SnippetIO.CodeSnippet> CodeSnippetsList
    {
        get { return (IEnumerable<SnippetIO.CodeSnippet>)GetValue(CodeSnippetsListProperty); }
        set { SetValue(CodeSnippetsListProperty, value); }
    }
    public static readonly DependencyProperty CodeSnippetsListProperty =
        DependencyProperty.Register("CodeSnippetsList", typeof(IEnumerable<SnippetIO.CodeSnippet>), typeof(MainWindow), new PropertyMetadata(null));
    #endregion Dependency Properties

    #region Observer Operations
    private void QueryCodeSnippetsList()
    {
        CodeSnippetsList = s_SnippetIO.ReadAll();
    }
    private void CodeSnippetsListObserver() => QueryCodeSnippetsList();
    #endregion Observer Operations

}
