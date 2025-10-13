using Avalonia;
using System;
using System.Threading;
using System.Threading.Tasks;
using darts_hub.UI;

namespace darts_hub
{
    internal class Program
    {
        private static Mutex _mutex;

        [STAThread]
        public static async Task<int> Main(string[] args) 
        {
            // Process command line arguments first
            try
            {
                bool shouldStartGui = await ShouldStartGui(args);
                if (!shouldStartGui)
                {
                    return 0; // Exit after command line operation
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error processing command line arguments: {ex.Message}");
                return 1;
            }

            // Continue with normal GUI startup
            const string MutexName = "DartsHub-UniqueMutexName";
            bool createdNew;

            _mutex = new Mutex(true, MutexName, out createdNew);

            if (!createdNew)
            {
                System.Console.WriteLine("Application is already running.");
                return 0;
            }

            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
                return 0;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Application startup failed: {ex.Message}");
                return 1;
            }
            finally
            {
                _mutex?.Close();
            }
        }

        private static async Task<bool> ShouldStartGui(string[] args)
        {
            // Process command line arguments
            // Returns false if a command was executed and GUI should not start
            // Returns true if GUI should start (no command or --verbose/--beta flags)
            bool processedCommand = await CommandLineHelper.ProcessCommandLineArgs(args);
            return !processedCommand;
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
    }
}
