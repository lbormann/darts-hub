using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class ReadmeParser
{
    private static readonly HttpClient client = new HttpClient();

    public async Task<Dictionary<string, string>> GetArgumentsFromReadme(string url)
    {
        var arguments = new Dictionary<string, string>();
        try
        {
            System.Diagnostics.Debug.WriteLine($"=== README PARSER START ===");
            System.Diagnostics.Debug.WriteLine($"Fetching README from: {url}");
            
            var response = await client.GetStringAsync(url);
            System.Diagnostics.Debug.WriteLine($"README content received, length: {response.Length}");

            // Argumente extrahieren
            var argumentMatches = Regex.Matches(response, @"#### \*`-([^`]*)`");
            System.Diagnostics.Debug.WriteLine($"Found {argumentMatches.Count} argument matches");
            
            foreach (Match match in argumentMatches)
            {
                if (match.Groups.Count > 1)
                {
                    string argumentkey = match.Groups[1].Value.Split('/')[0];
                    var argument = argumentkey.Trim();
                    if (!arguments.ContainsKey(argument))
                    {
                        arguments[argument] = string.Empty;
                        System.Diagnostics.Debug.WriteLine($"Added argument: {argument}");
                    }
                }
            }

            // Beschreibungen extrahieren
            var descriptionMatches = Regex.Matches(response, @"<p>(.*?)</p>", RegexOptions.Singleline);
            System.Diagnostics.Debug.WriteLine($"Found {descriptionMatches.Count} description matches");
            
            foreach (Match match in descriptionMatches)
            {
                if (match.Groups.Count > 1)
                {
                    var description = match.Groups[1].Value.Trim();
                    System.Diagnostics.Debug.WriteLine($"Processing description: {description.Substring(0, Math.Min(50, description.Length))}...");
                    
                    // Hier wird angenommen, dass die Beschreibungen in der gleichen Reihenfolge wie die Argumente erscheinen
                    foreach (var key in arguments.Keys)
                    {
                        if (string.IsNullOrEmpty(arguments[key]))
                        {
                            arguments[key] = description;
                            System.Diagnostics.Debug.WriteLine($"Assigned description to {key}: {description.Substring(0, Math.Min(30, description.Length))}...");
                            break;
                        }
                    }
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"=== README PARSER COMPLETE ===");
            System.Diagnostics.Debug.WriteLine($"Final results: {arguments.Count} arguments with descriptions");
            foreach (var kvp in arguments)
            {
                var desc = string.IsNullOrEmpty(kvp.Value) ? "(no description)" : kvp.Value.Substring(0, Math.Min(30, kvp.Value.Length)) + "...";
                System.Diagnostics.Debug.WriteLine($"  {kvp.Key}: {desc}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR in README parser: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            Console.WriteLine($"Error fetching or parsing README.md: {ex.Message}");
        }

        return arguments;
    }
}
