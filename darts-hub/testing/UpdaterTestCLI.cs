using System;
using System.Threading.Tasks;
using darts_hub.control;

namespace darts_hub.testing
{
    /// <summary>
    /// Command-line interface for running updater tests
    /// </summary>
    public static class UpdaterTestCLI
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== DARTS-HUB UPDATER TEST CONSOLE ===");
            Console.WriteLine();
            
            // Subscribe to test events
            UpdaterTester.TestStatusChanged += (sender, status) =>
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {status}");
            };
            
            UpdaterTester.TestCompleted += (sender, results) =>
            {
                Console.WriteLine();
                Console.WriteLine("=== FINAL TEST RESULTS ===");
                Console.WriteLine(results);
                Console.WriteLine("=== TEST COMPLETE ===");
            };

            if (args.Length == 0)
            {
                await ShowMenu();
            }
            else
            {
                await ProcessArguments(args);
            }
        }

        private static async Task ShowMenu()
        {
            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("Verfügbare Tests:");
                Console.WriteLine("1. Vollständiger Test (alle Komponenten)");
                Console.WriteLine("2. Version Check Test");
                Console.WriteLine("3. Retry Mechanismus Test");
                Console.WriteLine("4. Nur Logging Test");
                Console.WriteLine("5. Beenden");
                Console.WriteLine();
                Console.Write("Wählen Sie eine Option (1-5): ");

                var input = Console.ReadLine();
                
                switch (input)
                {
                    case "1":
                        await RunFullTest();
                        break;
                    case "2":
                        await RunVersionTest();
                        break;
                    case "3":
                        await RunRetryTest();
                        break;
                    case "4":
                        await RunLoggingTest();
                        break;
                    case "5":
                        Console.WriteLine("Auf Wiedersehen!");
                        return;
                    default:
                        Console.WriteLine("Ungültige Auswahl. Bitte versuchen Sie es erneut.");
                        break;
                }
            }
        }

        private static async Task ProcessArguments(string[] args)
        {
            var command = args[0].ToLower();
            
            switch (command)
            {
                case "--full":
                case "-f":
                    await RunFullTest();
                    break;
                case "--version":
                case "-v":
                    await RunVersionTest();
                    break;
                case "--retry":
                case "-r":
                    await RunRetryTest();
                    break;
                case "--logging":
                case "-l":
                    await RunLoggingTest();
                    break;
                case "--help":
                case "-h":
                    ShowHelp();
                    break;
                default:
                    Console.WriteLine($"Unbekannter Parameter: {command}");
                    ShowHelp();
                    break;
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine("DARTS-HUB UPDATER TEST CLI");
            Console.WriteLine();
            Console.WriteLine("Verwendung:");
            Console.WriteLine("  UpdaterTestCLI [OPTION]");
            Console.WriteLine();
            Console.WriteLine("Optionen:");
            Console.WriteLine("  -f, --full        Vollständiger Test aller Komponenten");
            Console.WriteLine("  -v, --version     Test der Versionsprüfung");
            Console.WriteLine("  -r, --retry       Test des Retry-Mechanismus");
            Console.WriteLine("  -l, --logging     Test des Logging-Systems");
            Console.WriteLine("  -h, --help        Diese Hilfe anzeigen");
            Console.WriteLine();
            Console.WriteLine("Ohne Parameter wird ein interaktives Menü angezeigt.");
        }

        private static async Task RunFullTest()
        {
            Console.WriteLine();
            Console.WriteLine("🔍 Starte vollständigen Update-Test...");
            Console.WriteLine("Dies kann einige Minuten dauern...");
            
            try
            {
                await UpdaterTester.RunFullUpdateTest();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test fehlgeschlagen: {ex.Message}");
            }
        }

        private static async Task RunVersionTest()
        {
            Console.WriteLine();
            Console.WriteLine("📋 Starte Version-Check-Test...");
            
            try
            {
                await UpdaterTester.TestVersionCheckOnly();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test fehlgeschlagen: {ex.Message}");
            }
        }

        private static async Task RunRetryTest()
        {
            Console.WriteLine();
            Console.WriteLine("🔄 Starte Retry-Mechanismus-Test...");
            
            try
            {
                await UpdaterTester.TestRetryMechanismOnly();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test fehlgeschlagen: {ex.Message}");
            }
        }

        private static async Task RunLoggingTest()
        {
            Console.WriteLine();
            Console.WriteLine("📝 Starte Logging-Test...");
            
            try
            {
                UpdaterLogger.LogInfo("CLI Test - INFO Level");
                UpdaterLogger.LogWarning("CLI Test - WARNING Level");
                UpdaterLogger.LogError("CLI Test - ERROR Level");
                UpdaterLogger.LogDebug("CLI Test - DEBUG Level");
                
                Console.WriteLine("✅ Logging-Test abgeschlossen. Prüfen Sie die Log-Datei im logs/ Verzeichnis.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Logging-Test fehlgeschlagen: {ex.Message}");
            }
        }
    }
}