using System.Windows;

namespace DroidBox.Windows;

public partial class SplashWindow : Window
{
    public SplashWindow() => InitializeComponent();

    public void SetStatus(string message)
    {
        StartupStatusText.Text = message;
    }
}
