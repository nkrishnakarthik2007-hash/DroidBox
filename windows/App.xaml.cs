using System.Windows;
using System.Windows.Threading;
using Velopack;

namespace DroidBox.Windows;

public partial class App : Application
{
    [STAThread]
    public static void Main(string[] args)
    {
        AppLog.Initialize(args);
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
            AppLog.Error($"Unhandled process exception (terminating={eventArgs.IsTerminating})", eventArgs.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, eventArgs) =>
        {
            AppLog.Error("Unobserved background task exception", eventArgs.Exception);
            eventArgs.SetObserved();
        };

        try
        {
            AppLog.Info("Starting Velopack bootstrap.");
            VelopackApp.Build().Run();
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
        catch (Exception ex)
        {
            AppLog.Error("Fatal error before WPF startup completed.", ex);
            MessageBox.Show(
                $"DroidBox could not start. The error was saved to:\n{AppLog.LogDirectory}\n\n{ex.Message}",
                "NKK DroidBox startup error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, eventArgs) =>
            AppLog.Error("Unhandled WPF dispatcher exception.", eventArgs.Exception);

        SplashWindow? splash = null;
        try
        {
            AppLog.Info("WPF startup entered.");
            splash = new SplashWindow();
            splash.Show();
            AppLog.Info("Splash window shown.");
            await Dispatcher.Yield(DispatcherPriority.Render);

            splash.SetStatus("Preparing your app library…");
            AppLog.Info("Creating main launcher window.");
            var window = new MainWindow();
            MainWindow = window;

            var firstFrame = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            window.ContentRendered += (_, _) => firstFrame.TrySetResult();
            window.Show();
            AppLog.Info("Main launcher window shown; waiting for first rendered frame.");

            var completed = await Task.WhenAny(firstFrame.Task, Task.Delay(TimeSpan.FromSeconds(12)));
            if (completed == firstFrame.Task)
            {
                AppLog.Info("Main launcher first frame rendered successfully.");
            }
            else
            {
                AppLog.Warning("Main launcher did not report ContentRendered within 12 seconds; keeping startup diagnostics.");
                splash.SetStatus("Still preparing DroidBox…");
            }

            splash.Close();
            AppLog.Info("Splash window closed after main window handover.");
        }
        catch (Exception ex)
        {
            AppLog.Error("WPF startup failed.", ex);
            try { splash?.Close(); } catch { }
            MessageBox.Show(
                $"DroidBox could not finish starting. A diagnostic log was saved to:\n{AppLog.LogDirectory}\n\n{ex.Message}",
                "NKK DroidBox startup error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        AppLog.Info($"DroidBox shutting down with exit code {e.ApplicationExitCode}.");
        base.OnExit(e);
    }
}
