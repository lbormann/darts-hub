using Avalonia;
using System;
using System.Threading;
using System.Threading.Tasks;
using darts_hub.UI;
using System.Runtime.InteropServices;

namespace darts_hub
{
    internal class Program
    {
        private static Mutex _mutex;
        
        // Console allocation for Windows
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [STAThread]
        public static async Task<int> Main(string[] args) 
        {
            // Check if we have command line arguments that need console output
            bool needsConsole = HasConsoleCommands(args);
            bool consoleAllocated = false;

            // Process command line arguments first
            try
            {
                // Only allocate console if we need it and we're on Windows
                if (needsConsole && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (GetConsoleWindow() == IntPtr.Zero)
                    {
                        AllocConsole();
                        consoleAllocated = true;
                    }
                }

                bool shouldStartGui = await ShouldStartGui(args);
                if (!shouldStartGui)
                {
                    // Free console before exiting if we allocated it
                    if (consoleAllocated && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        FreeConsole();
                    }
                    return 0; // Exit after command line operation
                }

                // Free console before starting GUI if we allocated it
                if (consoleAllocated && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    FreeConsole();
                }
            }
            catch (Exception ex)
            {
                if (needsConsole)
                {
                    System.Console.WriteLine($"Error processing command line arguments: {ex.Message}");
                }
                
                // Free console on error if we allocated it
                if (consoleAllocated && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    FreeConsole();
                }
                return 1;
            }

            // Continue with normal GUI startup (no console window)
            const string MutexName = "DartsHub-UniqueMutexName";
            bool createdNew;

            _mutex = new Mutex(true, MutexName, out createdNew);

            if (!createdNew)
            {
                // For GUI startup, we don't want to show console errors
                return 0;
            }

            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
                return 0;
            }
            catch (Exception)
            {
                // For GUI startup, we don't want to show console errors
                return 1;
            }
            finally
            {
                _mutex?.Close();
            }
        }

        /// <summary>
        /// Checks if the command line arguments require console output
        /// </summary>
        private static bool HasConsoleCommands(string[] args)
        {
            if (args == null || args.Length == 0)
                return false;

            // Check for CLI commands that need console output
            foreach (var arg in args)
            {
                var lowerArg = arg.ToLowerInvariant();
                if (lowerArg.StartsWith("--") || lowerArg.StartsWith("-"))
                {
                    // These are CLI commands that need console
                    if (lowerArg.Contains("help") || 
                        lowerArg.Contains("version") || 
                        lowerArg.Contains("backup") || 
                        lowerArg.Contains("restore") || 
                        lowerArg.Contains("test") || 
                        lowerArg.Contains("status") ||
                        lowerArg.Contains("verbose"))
                    {
                        return true;
                    }
                }
            }

            return false;
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
