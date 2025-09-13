using darts_hub.model;
using System;
using System.IO;

namespace darts_hub.UI
{
    /// <summary>
    /// Helper class for getting app execution information
    /// </summary>
    public static class AppExecutableHelper
    {
        public static string GetAppExecutable(AppBase app)
        {
            try
            {
                // For most apps, we can get the executable info from configuration or derive it
                if (app.Configuration != null && app.Configuration.Arguments.Count > 0)
                {
                    // Check if first argument is path-to-executable or file for AppLocal/AppOpen
                    var firstArg = app.Configuration.Arguments[0];
                    if (firstArg.Name == "path-to-executable" || firstArg.Name == "file")
                    {
                        return !string.IsNullOrEmpty(firstArg.Value) ? firstArg.Value : "Not configured";
                    }
                }
                
                // For downloadable apps, try to construct typical path
                if (app.GetType().Name.Contains("Downloadable"))
                {
                    var basePath = Path.Combine(Environment.CurrentDirectory, "apps", app.Name);
                    var exePath = Path.Combine(basePath, $"{app.Name}.exe");
                    if (File.Exists(exePath))
                        return exePath;
                    
                    // Try without .exe for Linux/Mac
                    exePath = Path.Combine(basePath, app.Name);
                    if (File.Exists(exePath))
                        return exePath;
                        
                    // Check common installation patterns
                    if (app.Name == "darts-caller")
                    {
                        var commonPaths = new string[][]
                        {
                            new[] { "darts-caller.exe", "darts-caller" },
                            new[] { Path.Combine("darts-caller", "darts-caller.exe"), Path.Combine("darts-caller", "darts-caller") },
                            new[] { Path.Combine("darts-caller.exe"), Path.Combine("darts-caller") }
                        };
                        
                        foreach (var paths in commonPaths)
                        {
                            foreach (var path in paths)
                            {
                                var fullPath = Path.Combine(basePath, path);
                                if (File.Exists(fullPath))
                                    return fullPath;
                            }
                        }
                    }
                    
                    return $"Expected: {exePath}";
                }
                
                return $"{app.GetType().Name} executable";
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error getting executable for {app.CustomName}: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(errorMsg);
                Console.WriteLine($"[DEBUG] {errorMsg}");
                return "Error determining executable";
            }
        }
        
        public static string GetAppArguments(AppBase app)
        {
            try
            {
                if (app.Configuration == null)
                {
                    var msg = $"Configuration is null for app {app.CustomName}";
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] {msg}");
                    return "";
                }

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Getting arguments for {app.CustomName} ({app.Configuration.Arguments.Count} total arguments)");

                // Use the Configuration.GenerateArgumentString method with detailed error tracking
                var arguments = app.Configuration.GenerateArgumentString(app, null);
                
                var result = arguments?.Trim() ?? "";
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Generated arguments result length: {result.Length} characters");
                
                return result;
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error getting arguments for {app.CustomName}: {ex.Message}";
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] {errorMsg}");
                Console.WriteLine($"[DEBUG] {errorMsg}");
                
                // Try to get more detailed information about which argument failed
                if (app.Configuration != null)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] App has {app.Configuration.Arguments.Count} arguments - attempting to identify problematic argument...");
                        
                        for (int i = 0; i < Math.Min(app.Configuration.Arguments.Count, 10); i++) // Only check first 10 for debugging
                        {
                            try
                            {
                                var arg = app.Configuration.Arguments[i];
                                var name = arg.Name ?? "NULL";
                                var value = arg.Value ?? "NULL";
                                var typeClear = arg.GetTypeClear() ?? "NULL";
                                
                                System.Diagnostics.Debug.WriteLine($"[DEBUG] Argument {i} OK: {name}={value} (Type: {typeClear})");
                            }
                            catch (Exception argEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"[DEBUG] Problem with argument {i}: {argEx.Message}");
                            }
                        }
                        
                        if (app.Configuration.Arguments.Count > 10)
                        {
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] ... and {app.Configuration.Arguments.Count - 10} more arguments (not shown to prevent spam)");
                        }
                    }
                    catch (Exception detailEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Error during detailed analysis: {detailEx.Message}");
                    }
                }
                
                return "Error determining arguments - see debug output for details";
            }
        }
    }
}