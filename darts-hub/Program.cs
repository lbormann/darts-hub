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

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        private const int ATTACH_PARENT_PROCESS = -1;

        [STAThread]
        public static async Task<int> Main(string[] args) 
        {
            bool needsConsole = HasConsoleCommands(args);
            bool consoleAttached = false;
            
            try
            {
                // Handle console for CLI commands
                if (needsConsole && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Try to attach to parent console first (PowerShell, CMD, etc.)
                    if (AttachConsole(ATTACH_PARENT_PROCESS))
                    {
                        consoleAttached = true;
                        
                        // Redirect console streams
                        System.Console.SetOut(new System.IO.StreamWriter(System.Console.OpenStandardOutput()) { AutoFlush = true });
                        System.Console.SetError(new System.IO.StreamWriter(System.Console.OpenStandardError()) { AutoFlush = true });
                    }
                    else
                    {
                        // If attach fails, allocate a new console
                        AllocConsole();
                    }
                }

                // Process command line arguments
                bool shouldStartGui = await ShouldStartGui(args);
                
                if (!shouldStartGui)
                {
                    // For CLI commands, we need to ensure output is flushed
                    if (consoleAttached)
                    {
                        System.Console.Out.Flush();
                        System.Console.Error.Flush();
                        
                        // Send a newline to separate from parent process prompt
                        System.Console.WriteLine();
                    }
                    
                    return 0;
                }

                // For GUI startup, free console if we allocated it
                if (needsConsole && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (!consoleAttached) // Only free if we allocated, not if we attached
                    {
                        FreeConsole();
                    }
                }
            }
            catch (Exception ex)
            {
                if (needsConsole)
                {
                    System.Console.WriteLine($"Error processing command line arguments: {ex.Message}");
                    
                    if (consoleAttached)
                    {
                        System.Console.Out.Flush();
                        System.Console.Error.Flush();
                        System.Console.WriteLine();
                    }
                }
                return 1;
            }

            // Continue with GUI startup
            const string MutexName = "DartsHub-UniqueMutexName";
            bool createdNew;

            _mutex = new Mutex(true, MutexName, out createdNew);

            if (!createdNew)
            {
                // Application already running
                return 0;
            }

            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
                return 0;
            }
            catch (Exception ex)
            {
                // Log GUI startup errors (but don't show in console for GUI mode)
                System.Diagnostics.Debug.WriteLine($"GUI startup error: {ex.Message}");
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
                        lowerArg.Contains("info") ||
                        lowerArg.Contains("profiles") ||
                        lowerArg.Contains("list") ||
                        lowerArg.Contains("cleanup") ||
                        lowerArg.Equals("--verbose") == false && // verbose starts GUI
                        lowerArg.Equals("--beta") == false)     // beta starts GUI
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
