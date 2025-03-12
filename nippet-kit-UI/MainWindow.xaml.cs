using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SnippetUI;

public partial class MainWindow : Window
{
    private StringBuilder shortcutBuilder = new StringBuilder();
    static readonly SnippetIOApi.ISnippetIO s_SnippetIO = SnippetIOApi.Factory.Get();

    public MainWindow()
    {
        InitializeComponent();
        KeyShortcutBox.PreviewKeyDown += OnShortcutKeyDown;
        QueryCodeSnippetsList();
    }
    #region Events
    #region Window Events
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        s_SnippetIO.AddObserver(CodeSnippetsListObserver);
        SetDefaultItems();
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        s_SnippetIO.RemoveObserver(CodeSnippetsListObserver);
    }
    #endregion Window Events
    #region onChange Events
    private void CodeSnippetsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ChangeItemInList();
    }
    #endregion onChange Events
    #region Keyboard Events 
    private void OnShortcutKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true; // Prevents text from being added to the text box

        // If the pressed key is a system key or a Windows key, do nothing
        if ((e.Key == Key.System && e.SystemKey != Key.LeftAlt && e.SystemKey != Key.RightAlt) ||
            e.Key == Key.LWin || e.Key == Key.RWin)
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
        // cant have more than three keys
        else if (KeyShortcutBox.Text.Count(c => c == '+') == 2)
        {
            MessageBox.Show("Cannot have more than three keys.", "Invalid operation", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        else if (Helper.SnippetIOUtils.IsForbidden((System.Windows.Forms.Keys)KeyInterop.VirtualKeyFromKey(e.Key)))
        {
            MessageBox.Show($"Forbidden key: {e.Key}.", "Invalid operation", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        else
        {
            string keyString;
            // take care of special keys
            switch (e.Key)
            {
                case Key.System: // in case SystemKey is alt
                    switch (e.SystemKey)
                    {
                        case Key.LeftAlt:
                            keyString = "LeftAlt";
                            break;
                        case Key.RightAlt:
                            keyString = "RightAlt";
                            break;
                        default:
                            keyString = e.SystemKey.ToString();
                            break;
                    }
                    break;
                case Key.LeftCtrl:
                    keyString = "LeftCtrl";
                    break;
                case Key.RightCtrl:
                    keyString = "RightCtrl";
                    break;
                case Key.RightShift:
                    keyString = "RightShift";
                    break;
                case Key.LeftShift:
                    keyString = "LeftShift";
                    break;
                default:
                    keyString = e.Key.ToString();
                    break;
            }

            // dont add a key that is already in the shortcut
            if (!shortcutBuilder.ToString().Contains(keyString))
            {
                if (shortcutBuilder.Length > 0)
                    shortcutBuilder.Append(" + ");

                shortcutBuilder.Append(keyString);
            }
            else
            {
                MessageBox.Show("Cannot add the same key twice.", "Invalid operation", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
        }

        // update the text box
        KeyShortcutBox.Text = shortcutBuilder.ToString();
    }
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Hide the search icon when there is text, show it when empty
        SearchIcon.Visibility = string.IsNullOrWhiteSpace(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;

        if (!string.IsNullOrEmpty(SearchBox.Text))
        {
            SnippetGrid.ItemsSource = CodeSnippetsList.Where(snippet =>
                snippet.Id.Contains(SearchBox.Text, StringComparison.OrdinalIgnoreCase) ||
                snippet.Code.Contains(SearchBox.Text, StringComparison.OrdinalIgnoreCase) ||
                snippet.KeyShortcut.Contains(SearchBox.Text, StringComparison.OrdinalIgnoreCase) ||
                snippet.WordShortcut.Contains(SearchBox.Text, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        else
        {
            SnippetGrid.ItemsSource = CodeSnippetsList;
        }
    }

    // Clicking the icon focuses the search box
    private void SearchIcon_MouseDown(object sender, MouseButtonEventArgs e)
    {
        SearchBox.Focus();
    }

    private void WordShortcutBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        TextSnippet.WordShortcut = WordShortcutBox.Text;
        if (TextSnippet.WordShortcut.Length > 0)
        {
            if (TextSnippet.WordShortcut.Contains(' ')) // Check for spaces
            {
                WordShortcutBox.Foreground = Brushes.Red;
                MessageBox.Show("Word shortcut must be a single word (no spaces).", "Invalid operation", MessageBoxButton.OK, MessageBoxImage.Information);
                TextSnippet.WordShortcut = TextSnippet.WordShortcut.Replace(" ", "_");
                WordShortcutBox.Text = TextSnippet.WordShortcut;
                WordShortcutBox.CaretIndex = WordShortcutBox.Text.Length; // Move cursor to the end
            }
            else
            {
                WordShortcutBox.Foreground = Brushes.White;
            }
        }
    }
    #endregion Keyboard Events
    #region Button Events

    private void SaveAllButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Do you want to save All snippets?", "Confirm Save", MessageBoxButton.YesNo, MessageBoxImage.Question);

        try
        {
            if (result == MessageBoxResult.Yes)
            {
                string tmpid = SelectedSnippet.Id;
                s_SnippetIO.UpdateAll(CodeSnippetsList);
                SetDefaultItems(tmpid);
                AnimateGridColor(TimeSpan.FromSeconds(1));
            }
            else
            {
                MessageBox.Show("Snippets not saved.", "Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void SaveItemButton_Click(object sender, RoutedEventArgs e)
    {
        Button button = (sender as Button)!;

        string itemId = (string)button.CommandParameter;
        try
        {
            var row = FindParent<DataGridRow>(button!);
            if (row != null)
            {
                var snippetToUpdate = CodeSnippetsList.FirstOrDefault(s => s.Id == itemId);
                if (snippetToUpdate != null)
                {
                    var result = MessageBox.Show("Do you want to save the snippet?", "Confirm Save", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        s_SnippetIO.Update(snippetToUpdate);
                        SetDefaultItems(itemId);
                        AnimateRowColor(row, TimeSpan.FromSeconds(1));

                    }
                    else
                    {
                        MessageBox.Show("Snippet not saved.", "Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DeleteItemButton_Click(object sender, RoutedEventArgs e)
    {
        Button button = (sender as Button)!;

        string itemId = (string)button.CommandParameter;
        try
        {
            var snippetToDelete = CodeSnippetsList.FirstOrDefault(s => s.Id == itemId);
            if (snippetToDelete != null)
            {
                var result = MessageBox.Show("Do you want to delete the snippet?", "Confirm deletion", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    SetDefaultItems();
                    s_SnippetIO.Delete(snippetToDelete.Id);
                    QueryCodeSnippetsList();
                    SetDefaultItems();
                }
                else
                {
                    MessageBox.Show("Snippet not deleted.", "Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);
                }

            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void DeleteAllButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Do you want to delete all snippets?", "Confirm deletion", MessageBoxButton.YesNo, MessageBoxImage.Question);
        try
        {
            if (result == MessageBoxResult.Yes)
            {
                SetDefaultItems();
                s_SnippetIO.DeleteAll();
                QueryCodeSnippetsList();
                SetDefaultItems();

            }
            else
            {
                MessageBox.Show("Snippets not deleted.", "Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            TextSnippet.Id = CheckForDuplicateId(TextSnippet.Id);
            string tmpid = TextSnippet.Id;
            s_SnippetIO.Add(TextSnippet);
            SetDefaultItems(tmpid);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

        }
    }
    #endregion Button Events
    #endregion Events
    #region Helpers
    private string CheckForDuplicateId(string id)
    {

        if (CodeSnippetsList.Any(CodeSnippet => CodeSnippet.Id == id))
        {
            int suffix = 1;
            string newId = $"{id}{suffix}";
            while (true)
            {
                newId = $"{id}{suffix}";
                if (!CodeSnippetsList.Any(CodeSnippet => CodeSnippet.Id == newId))
                {
                    return newId;
                }
                suffix++;
            }
        }
        return id;
    }
    private void ChangeItemInList()
    {
        if (CodeSnippetsList != null && TextSnippet != null)
        {
            var tSelectedSnippet = CodeSnippetsList.FirstOrDefault(CodeSnippet => CodeSnippet.Id == TextSnippet.Id);
            if (tSelectedSnippet != null)
            {
                if (TextSnippet.Id != "")
                {
                    tSelectedSnippet.Code = TextSnippet.Code;
                    tSelectedSnippet.KeyShortcut = TextSnippet.KeyShortcut;
                    tSelectedSnippet.WordShortcut = TextSnippet.WordShortcut;
                    tSelectedSnippet.Id = TextSnippet.Id;
                }
            }
            TextSnippet = SelectedSnippet;
        }
    }
    private void SetDefaultItems(string? id = null)
    {
        int index = 0;
        SnippetIO.CodeSnippet? foundSnippet = null;

        // Search for the snippet with the matching ID
        if (id != null)
        {
            for (int i = 0; i < SnippetGrid.Items.Count; i++)
            {
                if (SnippetGrid.Items[i] is SnippetIO.CodeSnippet snippet && snippet.Id == id)
                {
                    foundSnippet = snippet;
                    index = i;
                    break; // Stop searching once found
                }
            }
        }
        else if (id == null)
        {
            TextSnippet = new() { Id = "", Code = " Waiting for you..", KeyShortcut = "", WordShortcut = "" };
            SelectedSnippet = TextSnippet;
        }
        // If not found, fallback to first item or default new snippet
        else if (foundSnippet == null)
        {
            foundSnippet = CodeSnippetsList.FirstOrDefault() ?? new SnippetIO.CodeSnippet
            {
                Id = "",
                Code = " Waiting for you..",
                KeyShortcut = "",
                WordShortcut = ""
            };
            index = SnippetGrid.Items.IndexOf(foundSnippet);
            TextSnippet = foundSnippet;
            SelectedSnippet = foundSnippet;
            SnippetGrid.SelectedIndex = index;
        }
        else
        {
            SelectedSnippet = foundSnippet;
            TextSnippet = foundSnippet;
            SnippetGrid.SelectedIndex = index;
        }
    }

    private T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        // Traverse the visual tree upwards
        while (child != null)
        {
            if (child is T)
            {
                return (T)child; // Return the parent if it's of type T
            }
            child = VisualTreeHelper.GetParent(child); // Move up the tree
        }

        return null;
    }
    // Helper method to find the visual child of a specific type in a visual tree
    private T? GetVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        // Use the default value for T, but differentiate for reference and value types.
        T? child = default(T);

        int childrenCount = VisualTreeHelper.GetChildrenCount(parent);

        for (int i = 0; i < childrenCount; i++)
        {
            DependencyObject childObject = VisualTreeHelper.GetChild(parent, i);

            // If childObject is of type T, return it.
            if (childObject is T)
            {
                child = (T)childObject;
                break;
            }

            // If the childObject itself is not of type T, recursively search its children.
            child = GetVisualChild<T>(childObject);

            if (child != null)
            {
                break;
            }
        }

        // Return null explicitly for reference types or default value for value types.
        return child;
    }
    // Helper method to get all the DataGridCells in a DataGridRow
    private IEnumerable<DataGridCell> GetCells(DataGridRow row)
    {
        var cells = new List<DataGridCell>();

        // Traverse the visual tree to find all DataGridCells
        for (int i = 0; i < SnippetGrid.Columns.Count; i++)
        {
            DataGridCell? cell = GetCell(row, i);
            if (cell != null)
            {
                cells.Add(cell);
            }
        }

        return cells;
    }

    // Helper method to get a DataGridCell from a DataGridRow and column index
    private DataGridCell? GetCell(DataGridRow row, int columnIndex)
    {
        DataGridCellsPresenter? presenter = GetVisualChild<DataGridCellsPresenter>(row);

        // Try to get the cell from the presenter
        if (presenter != null)
        {
            return presenter.ItemContainerGenerator.ContainerFromIndex(columnIndex) as DataGridCell;
        }

        return null;
    }
    private DataGridRow? GetDataGridRowBySnippetId(string snippetId)
    {
        foreach (var item in SnippetGrid.Items)
        {
            if (item is SnippetIO.CodeSnippet snippet && snippet.Id == snippetId)
            {
                return SnippetGrid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
            }
        }
        return null; // If no matching row is found
    }

    #endregion Helpers
    #region Animations
    private void AnimateRowColor(DataGridRow row, TimeSpan duration)
    {
        Color fromColor = Color.FromArgb(0xFF, 0x17, 0x17, 0x17);
        Color toColor = (Color)ColorConverter.ConvertFromString("MediumPurple");

        ColorAnimation colorAnimation = new()
        {
            From = fromColor,
            To = toColor,
            Duration = duration,
            AutoReverse = true,
            RepeatBehavior = new RepeatBehavior(1)
        };

        // Iterate over all cells in the row and apply the color animation
        foreach (DataGridCell cell in GetCells(row))
        {
            SolidColorBrush brush = new(fromColor);
            cell.Background = brush;

            // Use HandoffBehavior.Compose so that the animation blends with other changes
            brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation, HandoffBehavior.Compose);

            // Once the animation completes, clear the local value of Background so that the style trigger can apply
            colorAnimation.Completed += (s, e) =>
            {
                cell.ClearValue(DataGridCell.BackgroundProperty);
            };
        }
    }

    private void AnimateGridColor(TimeSpan duration)
    {
        Color fromColor = Color.FromArgb(0xFF, 0x17, 0x17, 0x17);
        Color toColor = (Color)ColorConverter.ConvertFromString("MediumPurple");

        ColorAnimation colorAnimation = new()
        {
            From = fromColor,
            To = toColor,
            Duration = duration,
            AutoReverse = true,
            RepeatBehavior = new RepeatBehavior(1)
        };

        SolidColorBrush brush = new(fromColor);
        SnippetGrid.Background = brush;
        brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);

    }
    #endregion Animations
    #region Propetries
    #endregion Propetries
    #region Dependency Properties
    public string SearchText
    {
        get { return (string)GetValue(SearchTextProperty); }
        set { SetValue(SearchTextProperty, value); }
    }
    public static readonly DependencyProperty SearchTextProperty =
    DependencyProperty.Register("SearchText", typeof(string), typeof(MainWindow), new PropertyMetadata(null));
    public SnippetIO.CodeSnippet TextSnippet
    {
        get { return (SnippetIO.CodeSnippet)GetValue(TextSnippetProperty); }
        set { SetValue(TextSnippetProperty, value); }
    }
    public static readonly DependencyProperty TextSnippetProperty =
    DependencyProperty.Register("TextSnippet", typeof(SnippetIO.CodeSnippet), typeof(MainWindow), new PropertyMetadata(null));
    public SnippetIO.CodeSnippet SelectedSnippet
    {
        get { return (SnippetIO.CodeSnippet)GetValue(SelectedSnippetProperty); }
        set { SetValue(SelectedSnippetProperty, value); }
    }
    public static readonly DependencyProperty SelectedSnippetProperty =
    DependencyProperty.Register("SelectedSnippet", typeof(SnippetIO.CodeSnippet), typeof(MainWindow), new PropertyMetadata(null));
    public List<SnippetIO.CodeSnippet> CodeSnippetsList
    {
        get { return (List<SnippetIO.CodeSnippet>)GetValue(CodeSnippetsListProperty); }
        set { SetValue(CodeSnippetsListProperty, value); }
    }
    public static readonly DependencyProperty CodeSnippetsListProperty =
        DependencyProperty.Register("CodeSnippetsList", typeof(IEnumerable<SnippetIO.CodeSnippet>), typeof(MainWindow), new PropertyMetadata(null));
    #endregion Dependency Properties

    #region Observer Operations
    private void QueryCodeSnippetsList()
    {
        CodeSnippetsList = s_SnippetIO.ReadAll().OrderBy(snippet => snippet.Id).ToList();
    }

    private void CodeSnippetsListObserver() => QueryCodeSnippetsList();
    #endregion Observer Operations


}
