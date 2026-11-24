using System.Windows;
using Velopack;
namespace DroidBox.Windows;
public partial class App : Application
{
 [STAThread]
 public static void Main(string[] args)
 {
  VelopackApp.Build().Run();
  var app = new App();
  app.InitializeComponent();
  app.Run();
 }
 protected override async void OnStartup(StartupEventArgs e)
 {
  base.OnStartup(e);
  var splash = new SplashWindow();
  splash.Show();
  await Task.Delay(1150);
  var window = new MainWindow();
  MainWindow = window;
  window.Show();
  splash.Close();
 }
}