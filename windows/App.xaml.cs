using System.Windows;
using Velopack;

namespace DroidBox.Windows;

public partial class App : Application
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Velopack must run before WPF initializes so install/update hooks can exit cleanly.
        VelopackApp.Build().Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
