using System.Collections.Specialized;
using System.Windows;

using ToxicFishing.App.ViewModels;

namespace ToxicFishing.App;

/// <summary>
/// The application's main window. Hosts the MVVM view bound to <see cref="MainViewModel"/>, draws the
/// custom themed title bar (minimize/maximize/close), and auto-scrolls the activity list to the newest
/// entry as items are added.
/// </summary>
public partial class MainWindow : Window
{
    private const int MaximizeGlyph = 0xE922;
    private const int RestoreGlyph = 0xE923;

    /// <summary>
    /// Initializes the window and hooks the loaded event used to wire activity auto-scroll.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm &&
            vm.ActivityEntries is INotifyCollectionChanged observableEntries)
        {
            observableEntries.CollectionChanged += (_, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add && ActivityList.Items.Count > 0)
                {
                    ActivityList.ScrollIntoView(ActivityList.Items[^1]!);
                }
            };
        }
    }

    private void OnMinimize(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);

    private void OnMaximizeRestore(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnClose(object sender, RoutedEventArgs e) => Close();

    /// <inheritdoc />
    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);

        if (MaxRestoreButton is null)
        {
            return;
        }

        MaxRestoreButton.Content = (char)(WindowState == WindowState.Maximized ? RestoreGlyph : MaximizeGlyph);
    }
}
