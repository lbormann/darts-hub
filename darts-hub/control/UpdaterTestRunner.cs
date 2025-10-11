using System;
using System.Threading.Tasks;
using darts_hub.control;

namespace darts_hub.control
{
    /// <summary>
    /// Simplified test runner for cases where the GUI might have issues
    /// </summary>
    public static class UpdaterTestRunner
    {
        /// <summary>
        /// Run a quick version check test without GUI dependencies
        /// </summary>
        public static async Task<string> RunQuickVersionTest()
        {
            var results = "";
            
            try
            {
                UpdaterLogger.LogInfo("Starting quick version test");
                
                // Test GitHub API connectivity
                using var client = new System.Net.Http.HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "darts-hub-quick-test");
                client.Timeout = TimeSpan.FromSeconds(10);
                
                var response = await client.GetStringAsync("https://api.github.com/repos/lbormann/darts-hub/releases/latest");
                
                if (response.Contains("tag_name"))
                {
                    results += "✅ GitHub API: Reachable\n";
                    
                    // Extract version
                    int tagNameIndex = response.IndexOf("tag_name");
                    if (tagNameIndex != -1)
                    {
                        var versionPart = response.Substring(tagNameIndex);
                        int commaIndex = versionPart.IndexOf(',');
                        if (commaIndex != -1)
                        {
                            var versionString = versionPart.Substring("tag_name: \"".Length, commaIndex - "tag_name: \"".Length);
                            var latestVersion = versionString.Replace("\"", "");
                            
                            results += $"✅ Current Version: {Updater.version}\n";
                            results += $"✅ Latest Version: {latestVersion}\n";
                            
                            // Proper version comparison
                            var comparison = CompareVersions(Updater.version, latestVersion);
                            
                            if (comparison < 0)
                            {
                                results += "🆕 Update available!\n";
                            }
                            else if (comparison > 0)
                            {
                                results += "✅ Current version is newer than latest stable\n";
                            }
                            else
                            {
                                results += "✅ Version is up to date\n";
                            }
                        }
                    }
                }
                else
                {
                    results += "❌ GitHub API: Unexpected response\n";
                }
                
                UpdaterLogger.LogInfo("Quick version test completed successfully");
            }
            catch (Exception ex)
            {
                results += $"❌ Error: {ex.Message}\n";
                UpdaterLogger.LogError("Quick version test failed", ex);
            }
            
            return results;
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
        
        /// <summary>
        /// Run a basic connectivity test
        /// </summary>
        public static async Task<string> RunConnectivityTest()
        {
            var results = "";
            
            try
            {
                UpdaterLogger.LogInfo("Starting connectivity test");
                
                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                
                // Test basic internet
                var response = await client.GetAsync("https://www.google.com");
                if (response.IsSuccessStatusCode)
                {
                    results += "✅ Internet: Connected\n";
                }
                else
                {
                    results += $"❌ Internet: HTTP {response.StatusCode}\n";
                }
                
                // Test GitHub
                var githubResponse = await client.GetAsync("https://api.github.com");
                if (githubResponse.IsSuccessStatusCode)
                {
                    results += "✅ GitHub API: Reachable\n";
                }
                else
                {
                    results += $"❌ GitHub API: HTTP {githubResponse.StatusCode}\n";
                }
                
                UpdaterLogger.LogInfo("Connectivity test completed successfully");
            }
            catch (Exception ex)
            {
                results += $"❌ Connectivity error: {ex.Message}\n";
                UpdaterLogger.LogError("Connectivity test failed", ex);
            }
            
            return results;
        }
    }
}