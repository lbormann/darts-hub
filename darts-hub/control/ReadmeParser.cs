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
            var response = await client.GetStringAsync(url);

            // Argumente extrahieren
            var argumentMatches = Regex.Matches(response, @"#### \*`-([^`]*)`");
            foreach (Match match in argumentMatches)
            {
                if (match.Groups.Count > 1)
                {
                    string argumentkey = match.Groups[1].Value.Split('/')[0];
                    var argument = argumentkey.Trim();
                    if (!arguments.ContainsKey(argument))
                    {
                        arguments[argument] = string.Empty;
                    }
                }
            }

            // Beschreibungen extrahieren
            var descriptionMatches = Regex.Matches(response, @"<p>(.*?)</p>", RegexOptions.Singleline);
            foreach (Match match in descriptionMatches)
            {
                if (match.Groups.Count > 1)
                {
                    var description = match.Groups[1].Value.Trim();
                    // Hier wird angenommen, dass die Beschreibungen in der gleichen Reihenfolge wie die Argumente erscheinen
                    foreach (var key in arguments.Keys)
                    {
                        if (string.IsNullOrEmpty(arguments[key]))
                        {
                            arguments[key] = description;
                            break;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching or parsing README.md: {ex.Message}");
        }

        return arguments;
    }
}
