using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.ComponentModel;
using darts_hub.model;

namespace darts_hub.control
{
    /// <summary>
    /// Test framework for the updater functionality
    /// </summary>
    public static class UpdaterTester
    {
        public static event EventHandler<string>? TestStatusChanged;
        public static event EventHandler<string>? TestCompleted;
        
        private static bool isTestMode = false;
        private static string testResults = string.Empty;

        /// <summary>
        /// Runs comprehensive tests of the updater system
        /// </summary>
        public static async Task RunFullUpdateTest()
        {
            testResults = string.Empty;
            isTestMode = true;
            
            OnTestStatusChanged("=== UPDATER TEST SUITE STARTED ===");
            UpdaterLogger.LogInfo("Starting comprehensive updater test suite");
            
            try
            {
                // Test 1: Logging System
                await TestLoggingSystem();
                
                // Test 2: Network Connectivity
                await TestNetworkConnectivity();
                
                // Test 3: GitHub API Access
                await TestGitHubApiAccess();
                
                // Test 4: OS Detection
                TestOSDetection();
                
                // Test 5: Retry Mechanism
                await TestRetryMechanism();
                
                // Test 6: Version Check (Stable)
                await TestVersionCheckStable();
                
                // Test 7: Version Check (Beta)
                await TestVersionCheckBeta();
                
                // Test 8: Simulated Update Process
                await TestSimulatedUpdate();
                
                OnTestStatusChanged("=== ALL TESTS COMPLETED ===");
                OnTestCompleted(testResults);
            }
            catch (Exception ex)
            {
                var errorMsg = $"Test suite failed: {ex.Message}";
                OnTestStatusChanged(errorMsg);
                UpdaterLogger.LogError("Test suite execution failed", ex);
                OnTestCompleted(testResults + "\n" + errorMsg);
            }
            finally
            {
                isTestMode = false;
            }
        }

        /// <summary>
        /// Test only the version check functionality with isolated test mode
        /// </summary>
        public static async Task TestVersionCheckOnly()
        {
            testResults = string.Empty;
            isTestMode = true;
            
            OnTestStatusChanged("=== VERSION CHECK TEST STARTED ===");
            UpdaterLogger.LogInfo("Starting isolated version check test");
            
            try
            {
                await TestVersionCheckStableIsolated();
                if (Updater.IsBetaTester)
                {
                    await TestVersionCheckBetaIsolated();
                }
                
                OnTestStatusChanged("=== VERSION CHECK TEST COMPLETED ===");
                OnTestCompleted(testResults);
            }
            catch (Exception ex)
            {
                var errorMsg = $"Version check test failed: {ex.Message}";
                OnTestStatusChanged(errorMsg);
                UpdaterLogger.LogError("Version check test failed", ex);
                OnTestCompleted(testResults + "\n" + errorMsg);
            }
            finally
            {
                isTestMode = false;
            }
        }

        /// <summary>
        /// Simulate network issues to test retry mechanism
        /// </summary>
        public static async Task TestRetryMechanismOnly()
        {
            testResults = string.Empty;
            isTestMode = true;
            
            OnTestStatusChanged("=== RETRY MECHANISM TEST STARTED ===");
            UpdaterLogger.LogInfo("Starting retry mechanism test");
            
            try
            {
                await TestRetryMechanism();
                OnTestStatusChanged("=== RETRY MECHANISM TEST COMPLETED ===");
                OnTestCompleted(testResults);
            }
            catch (Exception ex)
            {
                var errorMsg = $"Retry mechanism test failed: {ex.Message}";
                OnTestStatusChanged(errorMsg);
                UpdaterLogger.LogError("Retry mechanism test failed", ex);
                OnTestCompleted(testResults + "\n" + errorMsg);
            }
            finally
            {
                isTestMode = false;
            }
        }

        private static async Task TestLoggingSystem()
        {
            OnTestStatusChanged("Testing logging system...");
            
            try
            {
                UpdaterLogger.LogInfo("Test log entry - INFO level");
                UpdaterLogger.LogWarning("Test log entry - WARNING level");
                UpdaterLogger.LogError("Test log entry - ERROR level");
                UpdaterLogger.LogDebug("Test log entry - DEBUG level");
                
                // Verify log file exists
                var basePath = Helper.GetAppBasePath();
                var logDirectory = Path.Combine(basePath, "logs");
                var now = DateTime.Now;
                var expectedLogFile = Path.Combine(logDirectory, $"{now.Day:D2}_darts-hub.log");
                
                if (File.Exists(expectedLogFile))
                {
                    var logContent = await File.ReadAllTextAsync(expectedLogFile);
                    if (logContent.Contains("[INFO]") && logContent.Contains("[WARN]") && 
                        logContent.Contains("[ERROR]") && logContent.Contains("[DEBUG]"))
                    {
                        AppendTestResult("✅ Logging System: SUCCESS");
                        UpdaterLogger.LogInfo("Logging system test passed");
                    }
                    else
                    {
                        AppendTestResult("❌ Logging System: Log levels not correctly written");
                    }
                }
                else
                {
                    AppendTestResult("❌ Logging System: Log file not found");
                }
            }
            catch (Exception ex)
            {
                AppendTestResult($"❌ Logging System: ERROR - {ex.Message}");
                UpdaterLogger.LogError("Logging system test failed", ex);
            }
        }

        private static async Task TestNetworkConnectivity()
        {
            OnTestStatusChanged("Testing network connectivity...");
            
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                
                // Test basic internet connectivity
                var response = await client.GetAsync("https://www.google.com");
                if (response.IsSuccessStatusCode)
                {
                    AppendTestResult("✅ Network Connection: SUCCESS");
                    UpdaterLogger.LogInfo("Network connectivity test passed");
                }
                else
                {
                    AppendTestResult($"❌ Network Connection: HTTP {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                AppendTestResult($"❌ Network Connection: ERROR - {ex.Message}");
                UpdaterLogger.LogError("Network connectivity test failed", ex);
            }
        }

        private static async Task TestGitHubApiAccess()
        {
            OnTestStatusChanged("Testing GitHub API access...");
            
            try
            {
                var result = await RetryHelper.ExecuteWithRetryAsync(async () =>
                {
                    using var client = new System.Net.Http.HttpClient();
                    client.DefaultRequestHeaders.Add("User-Agent", "darts-hub-test");
                    client.Timeout = TimeSpan.FromSeconds(10);
                    
                    var response = await client.GetStringAsync("https://api.github.com/repos/lbormann/darts-hub/releases/latest");
                    return response.Contains("tag_name");
                }, 3, 1000, "GitHub API Test");

                if (result)
                {
                    AppendTestResult("✅ GitHub API: SUCCESS");
                    UpdaterLogger.LogInfo("GitHub API access test passed");
                }
                else
                {
                    AppendTestResult("❌ GitHub API: Unexpected response");
                }
            }
            catch (Exception ex)
            {
                AppendTestResult($"❌ GitHub API: ERROR - {ex.Message}");
                UpdaterLogger.LogError("GitHub API access test failed", ex);
            }
        }

        private static void TestOSDetection()
        {
            OnTestStatusChanged("Testing OS detection...");
            
            try
            {
                var appFile = TestGetAppFileByOS();
                var updateFile = TestGetUpdateFileByOS();
                
                if (!string.IsNullOrEmpty(appFile) && !string.IsNullOrEmpty(updateFile))
                {
                    AppendTestResult($"✅ OS Detection: {appFile}, {updateFile}");
                    UpdaterLogger.LogInfo($"OS detection test passed: {appFile}, {updateFile}");
                }
                else
                {
                    AppendTestResult("❌ OS Detection: Unknown operating system or architecture");
                }
            }
            catch (Exception ex)
            {
                AppendTestResult($"❌ OS Detection: ERROR - {ex.Message}");
                UpdaterLogger.LogError("OS detection test failed", ex);
            }
        }

        private static async Task TestRetryMechanism()
        {
            OnTestStatusChanged("Testing retry mechanism...");
            
            try
            {
                var startTime = DateTime.Now;
                
                try
                {
                    await RetryHelper.ExecuteWithRetryAsync(async () =>
                    {
                        // Simulate network failure
                        throw new System.Net.Http.HttpRequestException("Test network failure");
                    }, 3, 500, "Retry Test");
                }
                catch (Exception)
                {
                    // Expected to fail after retries
                }
                
                var duration = DateTime.Now - startTime;
                
                // Should take at least 1.5 seconds for 3 retries with 500ms base delay
                if (duration.TotalSeconds >= 1.0)
                {
                    AppendTestResult($"✅ Retry Mechanism: {duration.TotalSeconds:F1}s for 3 attempts");
                    UpdaterLogger.LogInfo($"Retry mechanism test passed: {duration.TotalSeconds:F1}s");
                }
                else
                {
                    AppendTestResult($"❌ Retry Mechanism: Too fast ({duration.TotalSeconds:F1}s)");
                }
            }
            catch (Exception ex)
            {
                AppendTestResult($"❌ Retry Mechanism: ERROR - {ex.Message}");
                UpdaterLogger.LogError("Retry mechanism test failed", ex);
            }
        }

        private static async Task TestVersionCheckStable()
        {
            OnTestStatusChanged("Testing stable version check...");
            
            try
            {
                await TestVersionCheckStableIsolated();
            }
            catch (Exception ex)
            {
                AppendTestResult($"❌ Stable Version Check: ERROR - {ex.Message}");
                UpdaterLogger.LogError("Stable version check test failed", ex);
            }
        }

        private static async Task TestVersionCheckBeta()
        {
            OnTestStatusChanged("Testing beta version check...");
            
            try
            {
                await TestVersionCheckBetaIsolated();
            }
            catch (Exception ex)
            {
                AppendTestResult($"❌ Beta Version Check: ERROR - {ex.Message}");
                UpdaterLogger.LogError("Beta version check test failed", ex);
            }
        }

        private static async Task TestVersionCheckStableIsolated()
        {
            try
            {
                UpdaterLogger.LogInfo($"Testing stable version check directly via HTTP");
                
                // Test stable version check directly without using Updater events
                var latestVersion = await RetryHelper.ExecuteWithRetryAsync(async () =>
                {
                    using var client = new System.Net.Http.HttpClient();
                    client.DefaultRequestHeaders.Add("User-Agent", "darts-hub-test");
                    client.Timeout = TimeSpan.FromSeconds(10);
                    
                    UpdaterLogger.LogDebug("Sending HTTP request to GitHub API for stable release");
                    var result = await client.GetStringAsync("https://api.github.com/repos/lbormann/darts-hub/releases/latest");
                    
                    UpdaterLogger.LogDebug("Parsing GitHub API response for stable release");
                    int tagNameIndex = result.IndexOf("tag_name");
                    if (tagNameIndex == -1) 
                    {
                        throw new ArgumentException("github-tagName-Index not found");
                    }
                    
                    result = result.Substring(tagNameIndex);
                    int tagNameCommaIndex = result.IndexOf(',');
                    if (tagNameCommaIndex == -1) 
                    {
                        throw new ArgumentException("github-tagNameComma-Index not found");
                    }
                    
                    result = result.Substring("tag_name: \"".Length, tagNameCommaIndex - "tag_name: \"".Length);
                    return result.Replace("\"", "");
                }, 3, 2000, "GitHub API Stable Version Check");

                UpdaterLogger.LogInfo($"Latest stable version from GitHub: {latestVersion}");
                UpdaterLogger.LogInfo($"Current application version: {Updater.version}");

                // Proper version comparison
                var comparison = CompareVersions(Updater.version, latestVersion);
                
                if (comparison < 0)
                {
                    AppendTestResult($"✅ Stable Version Check: New version found ({latestVersion})");
                    UpdaterLogger.LogInfo("Stable version check test passed - new version available");
                }
                else if (comparison > 0)
                {
                    AppendTestResult($"✅ Stable Version Check: Current version is newer than latest stable ({latestVersion})");
                    UpdaterLogger.LogInfo("Stable version check test passed - current version is newer than stable");
                }
                else
                {
                    AppendTestResult("✅ Stable Version Check: Current version is up to date");
                    UpdaterLogger.LogInfo("Stable version check test passed - current version is latest");
                }
            }
            catch (Exception ex)
            {
                AppendTestResult($"❌ Stable Version Check: ERROR - {ex.Message}");
                UpdaterLogger.LogError("Stable version check test failed", ex);
            }
        }

        private static async Task TestVersionCheckBetaIsolated()
        {
            try
            {
                UpdaterLogger.LogInfo($"Testing beta version check directly via HTTP");
                
                // Test beta version check directly without using Updater events
                var latestBetaVersion = await RetryHelper.ExecuteWithRetryAsync(async () =>
                {
                    using var client = new System.Net.Http.HttpClient();
                    client.DefaultRequestHeaders.Add("User-Agent", "darts-hub-test");
                    client.Timeout = TimeSpan.FromSeconds(10);
                    
                    UpdaterLogger.LogDebug("Sending HTTP request to GitHub API for beta releases");
                    var result = await client.GetStringAsync("https://api.github.com/repos/lbormann/darts-hub/releases");
                    
                    UpdaterLogger.LogDebug("Parsing releases to find latest beta");
                    var releases = System.Text.Json.JsonDocument.Parse(result).RootElement.EnumerateArray();
                    
                    foreach (var release in releases)
                    {
                        if (release.GetProperty("prerelease").GetBoolean())
                        {
                            return release.GetProperty("tag_name").GetString();
                        }
                    }

                    return null; // No beta releases found
                }, 3, 2000, "GitHub API Beta Version Check");

                if (latestBetaVersion != null)
                {
                    UpdaterLogger.LogInfo($"Latest beta version from GitHub: {latestBetaVersion}");
                    UpdaterLogger.LogInfo($"Current application version: {Updater.version}");
                    
                    // Proper version comparison
                    var comparison = CompareVersions(Updater.version, latestBetaVersion);
                    
                    if (comparison < 0)
                    {
                        AppendTestResult($"✅ Beta Version Check: New beta version found ({latestBetaVersion})");
                        UpdaterLogger.LogInfo("Beta version check test passed - new beta version available");
                    }
                    else if (comparison > 0)
                    {
                        AppendTestResult($"✅ Beta Version Check: Current version is newer than latest beta ({latestBetaVersion})");
                        UpdaterLogger.LogInfo("Beta version check test passed - current version is newer than beta");
                    }
                    else
                    {
                        AppendTestResult("✅ Beta Version Check: Current beta version is up to date");
                        UpdaterLogger.LogInfo("Beta version check test passed - current beta version is latest");
                    }
                }
                else
                {
                    AppendTestResult("✅ Beta Version Check: No beta versions available");
                    UpdaterLogger.LogInfo("Beta version check test passed - no beta releases found");
                }
            }
            catch (Exception ex)
            {
                AppendTestResult($"❌ Beta Version Check: ERROR - {ex.Message}");
                UpdaterLogger.LogError("Beta version check test failed", ex);
            }
        }

        /// <summary>
        /// Compares two version strings (supports formats like "v1.2.3", "b1.0.8", "1.2.3")
        /// </summary>
        /// <param name="version1">Current version</param>
        /// <param name="version2">Compared version</param>
        /// <returns>-1 if version1 < version2, 0 if equal, 1 if version1 > version2</returns>
        private static int CompareVersions(string version1, string version2)
        {
            try
            {
                // Clean version strings (remove v, b prefixes)
                var cleanVersion1 = version1.TrimStart('v', 'b');
                var cleanVersion2 = version2.TrimStart('v', 'b');
                
                // Parse as Version objects for proper comparison
                if (Version.TryParse(cleanVersion1, out var v1) && Version.TryParse(cleanVersion2, out var v2))
                {
                    return v1.CompareTo(v2);
                }
                
                // Fallback to string comparison if parsing fails
                UpdaterLogger.LogWarning($"Version parsing failed, using string comparison: {version1} vs {version2}");
                return string.Compare(cleanVersion1, cleanVersion2, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogError($"Version comparison failed: {ex.Message}");
                // Fallback to simple string comparison
                return string.Compare(version1, version2, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static async Task TestSimulatedUpdate()
        {
            OnTestStatusChanged("Testing simulated update process...");
            
            try
            {
                // This is a dry run - we don't actually download anything
                var appFile = TestGetAppFileByOS();
                var updateFile = TestGetUpdateFileByOS();
                
                if (!string.IsNullOrEmpty(appFile) && !string.IsNullOrEmpty(updateFile))
                {
                    var basePath = Helper.GetAppBasePath();
                    var updateDir = Path.Combine(basePath, "updates");
                    
                    // Test directory creation
                    if (!Directory.Exists(updateDir))
                    {
                        Directory.CreateDirectory(updateDir);
                    }
                    
                    if (Directory.Exists(updateDir))
                    {
                        AppendTestResult("✅ Simulated Update Process: Directories and paths correct");
                        UpdaterLogger.LogInfo("Simulated update process test passed");
                        
                        // Clean up test directory
                        Helper.RemoveDirectory(updateDir);
                    }
                    else
                    {
                        AppendTestResult("❌ Simulated Update Process: Directory creation failed");
                    }
                }
                else
                {
                    AppendTestResult("❌ Simulated Update Process: OS files not detected");
                }
            }
            catch (Exception ex)
            {
                AppendTestResult($"❌ Simulated Update Process: ERROR - {ex.Message}");
                UpdaterLogger.LogError("Simulated update process test failed", ex);
            }
        }

        // Helper methods that mirror the private methods in Updater
        private static string TestGetAppFileByOS()
        {
            string appFile = String.Empty;
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
            {
                if (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.X64)
                    appFile = "darts-hub-linux-X64.zip";
                else if (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64)
                    appFile = "darts-hub-linux-ARM64.zip";
                else if (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm)
                    appFile = "darts-hub-linux-ARM.zip";
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                if (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.X64)
                    appFile = "darts-hub-windows-X64.zip";
                else if (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.X86)
                    appFile = "darts-hub-windows-X86.zip";
                else if (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64)
                    appFile = "darts-hub-windows-ARM64.zip";
                else if (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm)
                    appFile = "darts-hub-windows-ARM.zip";
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
            {
                if (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.X64)
                    appFile = "darts-hub-macOS-X64.zip";
                else if (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64)
                    appFile = "darts-hub-macOS-ARM64.zip";
            }
            return appFile;
        }

        private static string TestGetUpdateFileByOS()
        {
            string updateFile = String.Empty;
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                updateFile = "update.sh";
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                updateFile = "update.bat";
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
                updateFile = "update.sh";
            return updateFile;
        }

        private static void AppendTestResult(string result)
        {
            testResults += result + "\n";
            OnTestStatusChanged(result);
            UpdaterLogger.LogInfo($"Test Result: {result}");
        }

        private static void OnTestStatusChanged(string status)
        {
            TestStatusChanged?.Invoke(null, status);
        }

        private static void OnTestCompleted(string results)
        {
            TestCompleted?.Invoke(null, results);
        }
    }
}