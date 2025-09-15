using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net;
using System.Threading;
using Newtonsoft.Json;

namespace darts_hub.control.wizard
{
    /// <summary>
    /// Network scanner for discovering smart home devices like WLED and Pixelit
    /// </summary>
    public class NetworkDeviceScanner
    {
        private static readonly HttpClient httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(5) };

        /// <summary>
        /// Scans the network for WLED devices
        /// </summary>
        public static async Task<List<WledDevice>> ScanForWledDevices(CancellationToken cancellationToken)
        {
            var discoveredDevices = new List<WledDevice>();
            var tasks = new List<Task>();

            try
            {
                var networkInterfaces = GetNetworkInterfaces();
                System.Diagnostics.Debug.WriteLine($"[WLED Scanner] Found {networkInterfaces.Count} local network interfaces");

                // Only scan the first (primary) local network interface
                var primaryInterface = networkInterfaces.FirstOrDefault();
                if (primaryInterface == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[WLED Scanner] No local network interfaces found");
                    return discoveredDevices;
                }

                var subnet = GetSubnet(primaryInterface);
                System.Diagnostics.Debug.WriteLine($"[WLED Scanner] Scanning LOCAL subnet only: {subnet}.x");

                // Validate that we're scanning a private network
                if (!IsPrivateSubnet(subnet))
                {
                    System.Diagnostics.Debug.WriteLine($"[WLED Scanner] ⚠️ Subnet {subnet} is not a private network - aborting scan for security");
                    return discoveredDevices;
                }

                var ipRanges = GetCommonDeviceIpRanges();
                var semaphore = new SemaphoreSlim(10, 10);

                foreach (var ipSuffix in ipRanges)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    var targetIp = $"{subnet}.{ipSuffix}";
                    
                    tasks.Add(Task.Run(async () =>
                    {
                        await semaphore.WaitAsync(cancellationToken);
                        try
                        {
                            var device = await TestWledDevice(targetIp, cancellationToken);
                            if (device != null)
                            {
                                lock (discoveredDevices)
                                {
                                    discoveredDevices.Add(device);
                                    System.Diagnostics.Debug.WriteLine($"[WLED Scanner] ✅ Found WLED device: {device.Name} at {device.IpAddress}");
                                }
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            System.Diagnostics.Debug.WriteLine($"[WLED Scanner] Scan cancelled for {targetIp}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[WLED Scanner] Error scanning {targetIp}: {ex.Message}");
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, cancellationToken));
                }

                System.Diagnostics.Debug.WriteLine($"[WLED Scanner] Waiting for {tasks.Count} LOCAL scan tasks to complete...");
                await Task.WhenAll(tasks);
                System.Diagnostics.Debug.WriteLine($"[WLED Scanner] LOCAL scan completed. Found {discoveredDevices.Count} WLED devices.");
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine($"[WLED Scanner] Network scan was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WLED Scanner] Network scan exception: {ex.Message}");
            }

            return discoveredDevices.OrderBy(d => d.IpAddress).ToList();
        }

        /// <summary>
        /// Scans the network for Pixelit devices
        /// </summary>
        public static async Task<List<PixelitDevice>> ScanForPixelitDevices(CancellationToken cancellationToken)
        {
            var discoveredDevices = new List<PixelitDevice>();
            var tasks = new List<Task>();

            try
            {
                var networkInterfaces = GetNetworkInterfaces();
                System.Diagnostics.Debug.WriteLine($"[Pixelit Scanner] Found {networkInterfaces.Count} local network interfaces");

                // Only scan the first (primary) local network interface
                var primaryInterface = networkInterfaces.FirstOrDefault();
                if (primaryInterface == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Pixelit Scanner] No local network interfaces found");
                    return discoveredDevices;
                }

                var subnet = GetSubnet(primaryInterface);
                System.Diagnostics.Debug.WriteLine($"[Pixelit Scanner] Scanning LOCAL subnet only: {subnet}.x");

                // Validate that we're scanning a private network
                if (!IsPrivateSubnet(subnet))
                {
                    System.Diagnostics.Debug.WriteLine($"[Pixelit Scanner] ⚠️ Subnet {subnet} is not a private network - aborting scan for security");
                    return discoveredDevices;
                }

                var ipRanges = GetCommonDeviceIpRanges();
                var semaphore = new SemaphoreSlim(10, 10);

                foreach (var ipSuffix in ipRanges)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    var targetIp = $"{subnet}.{ipSuffix}";
                    
                    tasks.Add(Task.Run(async () =>
                    {
                        await semaphore.WaitAsync(cancellationToken);
                        try
                        {
                            var device = await TestPixelitDevice(targetIp, cancellationToken);
                            if (device != null)
                            {
                                lock (discoveredDevices)
                                {
                                    discoveredDevices.Add(device);
                                    System.Diagnostics.Debug.WriteLine($"[Pixelit Scanner] ✅ Found Pixelit device: {device.Name} at {device.IpAddress}");
                                }
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Pixelit Scanner] Scan cancelled for {targetIp}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Pixelit Scanner] Error scanning {targetIp}: {ex.Message}");
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, cancellationToken));
                }

                System.Diagnostics.Debug.WriteLine($"[Pixelit Scanner] Waiting for {tasks.Count} LOCAL scan tasks to complete...");
                await Task.WhenAll(tasks);
                System.Diagnostics.Debug.WriteLine($"[Pixelit Scanner] LOCAL scan completed. Found {discoveredDevices.Count} Pixelit devices.");
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine($"[Pixelit Scanner] Network scan was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Pixelit Scanner] Network scan exception: {ex.Message}");
            }

            return discoveredDevices.OrderBy(d => d.IpAddress).ToList();
        }

        private static List<IPAddress> GetNetworkInterfaces()
        {
            // Only get local network interfaces (no external/online connections)
            var localInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up && 
                           ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                           ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel &&
                           ni.NetworkInterfaceType != NetworkInterfaceType.Ppp)
                .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
                .Where(addr => addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                              IsPrivateNetworkAddress(addr.Address)) // Only private/local network addresses
                .Select(addr => addr.Address)
                .ToList();

            System.Diagnostics.Debug.WriteLine($"[Scanner] Found {localInterfaces.Count} local network interfaces:");
            foreach (var ip in localInterfaces)
            {
                System.Diagnostics.Debug.WriteLine($"[Scanner] Local interface: {ip}");
            }

            return localInterfaces;
        }

        /// <summary>
        /// Checks if an IP address is in a private network range (RFC 1918)
        /// </summary>
        private static bool IsPrivateNetworkAddress(IPAddress ipAddress)
        {
            var bytes = ipAddress.GetAddressBytes();
            
            // 10.0.0.0/8 (10.0.0.0 - 10.255.255.255)
            if (bytes[0] == 10)
                return true;
            
            // 172.16.0.0/12 (172.16.0.0 - 172.31.255.255)
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                return true;
            
            // 192.168.0.0/16 (192.168.0.0 - 192.168.255.255)
            if (bytes[0] == 192 && bytes[1] == 168)
                return true;
            
            // Link-local addresses: 169.254.0.0/16
            if (bytes[0] == 169 && bytes[1] == 254)
                return true;
            
            System.Diagnostics.Debug.WriteLine($"[Scanner] Skipping non-private IP: {ipAddress}");
            return false;
        }

        /// <summary>
        /// Validates that a subnet string represents a private network
        /// </summary>
        private static bool IsPrivateSubnet(string subnet)
        {
            try
            {
                var parts = subnet.Split('.');
                if (parts.Length != 3) return false;

                var octet1 = int.Parse(parts[0]);
                var octet2 = int.Parse(parts[1]);
                var octet3 = int.Parse(parts[2]);

                // 10.x.x.x
                if (octet1 == 10)
                {
                    System.Diagnostics.Debug.WriteLine($"[Scanner] ✅ Private subnet: {subnet}.x (10.x.x.x range)");
                    return true;
                }

                // 172.16.x.x - 172.31.x.x
                if (octet1 == 172 && octet2 >= 16 && octet2 <= 31)
                {
                    System.Diagnostics.Debug.WriteLine($"[Scanner] ✅ Private subnet: {subnet}.x (172.16-31.x.x range)");
                    return true;
                }

                // 192.168.x.x
                if (octet1 == 192 && octet2 == 168)
                {
                    System.Diagnostics.Debug.WriteLine($"[Scanner] ✅ Private subnet: {subnet}.x (192.168.x.x range)");
                    return true;
                }

                // Link-local: 169.254.x.x
                if (octet1 == 169 && octet2 == 254)
                {
                    System.Diagnostics.Debug.WriteLine($"[Scanner] ✅ Link-local subnet: {subnet}.x (169.254.x.x range)");
                    return true;
                }

                System.Diagnostics.Debug.WriteLine($"[Scanner] ⚠️ Non-private subnet detected: {subnet}.x");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Scanner] Error validating subnet {subnet}: {ex.Message}");
                return false;
            }
        }

        private static string GetSubnet(IPAddress ipAddress)
        {
            var parts = ipAddress.ToString().Split('.');
            return $"{parts[0]}.{parts[1]}.{parts[2]}";
        }

        private static List<int> GetCommonDeviceIpRanges()
        {
            var ipRanges = new List<int>();
            
            // Router and common device ranges - expanded for better coverage
            for (int i = 1; i <= 20; i++) ipRanges.Add(i);       // 192.168.1.1-20 (routers and common devices)
            for (int i = 30; i <= 60; i++) ipRanges.Add(i);      // 192.168.1.30-60 (static devices)
            for (int i = 100; i <= 140; i++) ipRanges.Add(i);    // 192.168.1.100-140 (DHCP range start)
            for (int i = 200; i <= 240; i++) ipRanges.Add(i);    // 192.168.1.200-240 (high DHCP range)
            
            // Add specific examples if not already included
            if (!ipRanges.Contains(117)) ipRanges.Add(117);      // Pixelit example
            if (!ipRanges.Contains(254)) ipRanges.Add(254);      // Common router IP

            System.Diagnostics.Debug.WriteLine($"[Scanner] Will scan {ipRanges.Count} IP addresses in subnet");
            return ipRanges;
        }

        private static async Task<WledDevice?> TestWledDevice(string ipAddress, CancellationToken cancellationToken)
        {
            try
            {
                // Test if device responds to ping first (quick check)
                var ping = new Ping();
                var reply = await ping.SendPingAsync(ipAddress, 1000);
                
                if (reply.Status != IPStatus.Success) 
                {
                    System.Diagnostics.Debug.WriteLine($"[WLED Scanner] {ipAddress} - Ping failed: {reply.Status}");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"[WLED Scanner] {ipAddress} - Ping successful, testing HTTP endpoints");

                // Test WLED specific endpoints - check both web UI and JSON API
                var testUrls = new[]
                {
                    $"http://{ipAddress}/",              // Root page - WLED WebUI with <title>WLED</title>
                    $"http://{ipAddress}/json/info",     // JSON API info endpoint
                    $"http://{ipAddress}/json"           // JSON API state endpoint
                };

                using var client = new HttpClient() { Timeout = TimeSpan.FromSeconds(3) };

                foreach (var testUrl in testUrls)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[WLED Scanner] Testing URL with GET: {testUrl}");
                        
                        // Always use GET - WLED responds to GET requests
                        using var response = await client.GetAsync(testUrl, cancellationToken);
                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync(cancellationToken);
                            
                            System.Diagnostics.Debug.WriteLine($"[WLED Scanner] {testUrl} - GET Response length: {content.Length} chars");
                            
                            // Check for WLED title tag (main web UI)
                            if (content.Contains("<title>WLED</title>", StringComparison.OrdinalIgnoreCase))
                            {
                                System.Diagnostics.Debug.WriteLine($"[WLED Scanner] ✅ Found WLED title tag at {testUrl}");
                                
                                var deviceName = ExtractWledDeviceName(content) ?? $"WLED-{ipAddress.Split('.').Last()}";
                                var ledCount = ExtractWledLedCount(content);
                                
                                System.Diagnostics.Debug.WriteLine($"[WLED Scanner] ✅ Confirmed WLED device: {deviceName} at {ipAddress} with {ledCount} LEDs");
                                
                                return new WledDevice
                                {
                                    IpAddress = ipAddress,
                                    Name = deviceName,
                                    Endpoint = testUrl,
                                    ResponseContent = content,
                                    LedCount = ledCount
                                };
                            }
                            // Check for WLED JSON API response
                            else if (testUrl.Contains("/json") && content.TrimStart().StartsWith("{"))
                            {
                                try
                                {
                                    dynamic json = JsonConvert.DeserializeObject(content);
                                    
                                    // Check for WLED-specific JSON fields
                                    if (json?.ver != null || json?.info?.ver != null || 
                                        json?.leds != null || json?.info?.leds != null ||
                                        (json?.name != null && json.name.ToString().ToLower().Contains("wled")))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[WLED Scanner] ✅ Found WLED JSON API response at {testUrl}");
                                        
                                        var deviceName = ExtractWledDeviceNameFromJson(json) ?? $"WLED-{ipAddress.Split('.').Last()}";
                                        var ledCount = ExtractWledLedCountFromJson(json);
                                        
                                        System.Diagnostics.Debug.WriteLine($"[WLED Scanner] ✅ Confirmed WLED device via JSON: {deviceName} at {ipAddress} with {ledCount} LEDs");
                                        
                                        return new WledDevice
                                        {
                                            IpAddress = ipAddress,
                                            Name = deviceName,
                                            Endpoint = $"http://{ipAddress}/", // Use root URL as main endpoint
                                            ResponseContent = content,
                                            LedCount = ledCount
                                        };
                                    }
                                }
                                catch (Exception jsonEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[WLED Scanner] {testUrl} - JSON parsing failed: {jsonEx.Message}");
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[WLED Scanner] {testUrl} - No WLED indicators found");
                                
                                // Log a snippet of the content for debugging
                                var snippet = content.Length > 200 ? content.Substring(0, 200) + "..." : content;
                                System.Diagnostics.Debug.WriteLine($"[WLED Scanner] {testUrl} - Response snippet: {snippet}");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[WLED Scanner] {testUrl} - HTTP GET returned {response.StatusCode}");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        System.Diagnostics.Debug.WriteLine($"[WLED Scanner] {testUrl} - GET request cancelled");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[WLED Scanner] {testUrl} - GET request exception: {ex.Message}");
                        continue;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[WLED Scanner] {ipAddress} - No WLED device detected on any endpoint");
            }
            catch (OperationCanceledException)
            {
                throw; // Re-throw cancellation
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WLED Scanner] {ipAddress} - Device test failed: {ex.Message}");
            }

            return null;
        }

        private static async Task<PixelitDevice?> TestPixelitDevice(string ipAddress, CancellationToken cancellationToken)
        {
            try
            {
                // Test if device responds to ping first (quick check)
                var ping = new Ping();
                var reply = await ping.SendPingAsync(ipAddress, 1000);
                
                if (reply.Status != IPStatus.Success) 
                {
                    System.Diagnostics.Debug.WriteLine($"[Pixelit Scanner] {ipAddress} - Ping failed: {reply.Status}");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"[Pixelit Scanner] {ipAddress} - Ping successful, testing HTTP endpoints");

                // Test Pixelit specific endpoints - root page is most likely to have the title
                var testUrls = new[]
                {
                    $"http://{ipAddress}/",          // Root page - most likely to have PixelIt WebUI title
                    $"http://{ipAddress}/config",    // Config page
                    $"http://{ipAddress}/api",       // API endpoint
                    $"http://{ipAddress}/status"     // Status page
                };

                using var client = new HttpClient() { Timeout = TimeSpan.FromSeconds(3) }; // Reduced timeout for faster scanning

                foreach (var testUrl in testUrls)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[Pixelit Scanner] Testing URL: {testUrl}");
                        
                        using var response = await client.GetAsync(testUrl, cancellationToken);
                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync(cancellationToken);
                            
                            System.Diagnostics.Debug.WriteLine($"[Pixelit Scanner] {testUrl} - Response length: {content.Length} chars");
                            
                            // Check specifically for PixelIt WebUI title tag
                            if (content.Contains("<title>PixelIt WebUI</title>", StringComparison.OrdinalIgnoreCase))
                            {
                                System.Diagnostics.Debug.WriteLine($"[Pixelit Scanner] ✅ Found PixelIt WebUI title tag at {testUrl}");
                                
                                var deviceName = ExtractPixelitDeviceName(content) ?? $"Pixelit-{ipAddress.Split('.').Last()}";
                                
                                System.Diagnostics.Debug.WriteLine($"[Pixelit Scanner] ✅ Confirmed Pixelit device: {deviceName} at {ipAddress}");
                                
                                return new PixelitDevice
                                {
                                    IpAddress = ipAddress,
                                    Name = deviceName,
                                    Endpoint = testUrl,
                                    ResponseContent = content
                                };
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[Pixelit Scanner] {testUrl} - No PixelIt WebUI title tag found");
                                
                                // Log a snippet for debugging
                                var snippet = content.Length > 200 ? content.Substring(0, 200) + "..." : content;
                                System.Diagnostics.Debug.WriteLine($"[Pixelit Scanner] {testUrl} - Response snippet: {snippet}");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[Pixelit Scanner] {testUrl} - HTTP {response.StatusCode}");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Pixelit Scanner] {testUrl} - Request cancelled");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Pixelit Scanner] {testUrl} - Exception: {ex.Message}");
                        continue;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[Pixelit Scanner] {ipAddress} - No Pixelit device detected on any endpoint");
            }
            catch (OperationCanceledException)
            {
                throw; // Re-throw cancellation
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Pixelit Scanner] {ipAddress} - Device test failed: {ex.Message}");
            }

            return null;
        }

        private static string? ExtractWledDeviceName(string content)
        {
            try
            {
                // First check for the specific WLED title
                if (content.Contains("<title>WLED</title>", StringComparison.OrdinalIgnoreCase))
                {
                    return "WLED";
                }

                // Try to extract device name from JSON response (if available)
                if (content.TrimStart().StartsWith("{"))
                {
                    dynamic json = JsonConvert.DeserializeObject(content);
                    return ExtractWledDeviceNameFromJson(json);
                }

                // Try to extract from any HTML title
                if (content.Contains("<title>"))
                {
                    var titleStart = content.IndexOf("<title>") + 7;
                    var titleEnd = content.IndexOf("</title>", titleStart);
                    if (titleEnd > titleStart)
                    {
                        var title = content.Substring(titleStart, titleEnd - titleStart).Trim();
                        if (!string.IsNullOrEmpty(title))
                        {
                            return title;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WLED Scanner] Error extracting device name: {ex.Message}");
            }

            return null;
        }

        private static string? ExtractWledDeviceNameFromJson(dynamic json)
        {
            try
            {
                // Try different JSON fields for device name
                var jsonName = json?.info?.name ?? json?.name ?? json?.info?.hostname ?? 
                              json?.hostname ?? json?.info?.title ?? json?.title;
                
                if (jsonName != null)
                {
                    var name = jsonName.ToString();
                    System.Diagnostics.Debug.WriteLine($"[WLED Scanner] Extracted device name from JSON: {name}");
                    return name;
                }
                
                // Default WLED name if found via JSON API
                return "WLED";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WLED Scanner] Error extracting device name from JSON: {ex.Message}");
                return "WLED";
            }
        }

        private static string? ExtractPixelitDeviceName(string content)
        {
            try
            {
                // First check for the specific PixelIt WebUI title
                if (content.Contains("<title>PixelIt WebUI</title>", StringComparison.OrdinalIgnoreCase))
                {
                    return "PixelIt WebUI";
                }

                // Try to extract device name from JSON response
                if (content.TrimStart().StartsWith("{"))
                {
                    dynamic json = JsonConvert.DeserializeObject(content);
                    var jsonName = json?.name ?? json?.device_name ?? json?.hostname ?? json?.title;
                    if (jsonName != null)
                    {
                        return jsonName.ToString();
                    }
                }

                // Fallback: extract from URL or use generic name
                var ipSuffix = content.Split('/').LastOrDefault();
                return $"Pixelit-{ipSuffix}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Pixelit Scanner] Error extracting device name: {ex.Message}");
                return null;
            }
        }

        private static int ExtractWledLedCount(string content)
        {
            try
            {
                // Try to extract LED count from JSON response
                if (content.TrimStart().StartsWith("{"))
                {
                    dynamic json = JsonConvert.DeserializeObject(content);
                    return ExtractWledLedCountFromJson(json);
                }

                // Look for common LED count patterns in HTML/text
                var ledPatterns = new[]
                {
                    @"(\d+)\s*leds?",
                    @"(\d+)\s*LEDs?",
                    @"count[:\s]*(\d+)",
                    @"length[:\s]*(\d+)"
                };

                foreach (var pattern in ledPatterns)
                {
                    var match = System.Text.RegularExpressions.Regex.Match(content, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (match.Success && int.TryParse(match.Groups[1].Value, out var count))
                    {
                        return count;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WLED Scanner] Error extracting LED count: {ex.Message}");
            }

            return 0; // Default/unknown
        }

        private static int ExtractWledLedCountFromJson(dynamic json)
        {
            try
            {
                // Try different JSON structures for LED count
                var ledCount = json?.info?.leds?.count ?? json?.leds?.count ?? 
                              json?.info?.ledCount ?? json?.ledCount ?? 
                              json?.info?.leds?.total ?? json?.leds?.total ??
                              json?.strip?.ledCount ?? json?.strip?.count;
                
                if (ledCount != null)
                {
                    var count = (int)ledCount;
                    System.Diagnostics.Debug.WriteLine($"[WLED Scanner] Extracted LED count from JSON: {count}");
                    return count;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WLED Scanner] Error extracting LED count from JSON: {ex.Message}");
            }

            return 0; // Default/unknown
        }
    }

    /// <summary>
    /// Represents a discovered WLED device on the network
    /// </summary>
    public class WledDevice
    {
        public string IpAddress { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Endpoint { get; set; } = null!;
        public string ResponseContent { get; set; } = null!;
        public int LedCount { get; set; }
    }

    /// <summary>
    /// Represents a discovered Pixelit device on the network
    /// </summary>
    public class PixelitDevice
    {
        public string IpAddress { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Endpoint { get; set; } = null!;
        public string ResponseContent { get; set; } = null!;
    }
}