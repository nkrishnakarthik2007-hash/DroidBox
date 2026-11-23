using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Input;
namespace DroidBox.Windows;
public partial class MainWindow : Window
{
    public MainWindow() { InitializeComponent(); }
    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Select Android APK", Filter = "Android packages (*.apk)|*.apk", CheckFileExists = true };
        if (dialog.ShowDialog() == true) ShowSelected(dialog.FileName);
    }
    private void Window_DragOver(object sender, DragEventArgs e) => e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0) ShowSelected(files[0]);
    }
    private void ShowSelected(string path)
    {
        var info = new FileInfo(path);
        if (!info.Exists || !string.Equals(info.Extension, ".apk", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Please select a valid APK file.", "DroidBox", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        FileNameText.Text = info.Name;
        FileInfoText.Text = $"{FormatSize(info.Length)} • Selected locally";
        InspectStatus.Text = "Selection received. Inspector integration is the next milestone.";
        SelectedPanel.Visibility = Visibility.Visible;
    }
    private static string FormatSize(long size) => size switch
    {
        < 1024 => $"{size} B",
        < 1024 * 1024 => $"{size / 1024.0:0.0} KB",
        _ => $"{size / 1024.0 / 1024:0.00} MB"
    };
}
