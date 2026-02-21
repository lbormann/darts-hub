using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using darts_hub.model;
using Newtonsoft.Json.Linq;

namespace darts_hub.control
{
    /// <summary>
    /// Sends Pixelit templates directly to the configured Pixelit device for quick testing.
    /// </summary>
    public static class PixelitTestService
    {
        private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(5) };

        /// <summary>
        /// Tests a template against a specific endpoint URL
        /// </summary>
        public static async Task<(bool Success, string Message)> TestTemplateOnEndpointAsync(AppBase? app, Argument param, string endpointIp)
        {
            if (app?.Configuration?.Arguments == null)
            {
                return (false, "Pixelit App-Konfiguration fehlt.");
            }

            var templatePathArg = app.Configuration.Arguments
                .FirstOrDefault(a => a.Name.Equals("TP", StringComparison.OrdinalIgnoreCase));
            var templateBasePath = templatePathArg?.Value;
            if (string.IsNullOrWhiteSpace(templateBasePath))
            {
                return (false, "Kein Templates-Pfad (TP) konfiguriert.");
            }

            if (string.IsNullOrWhiteSpace(endpointIp))
            {
                return (false, "Kein Endpunkt angegeben.");
            }

            var endpointUrl = endpointIp.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? endpointIp
                : $"http://{endpointIp}";
            endpointUrl = endpointUrl.TrimEnd('/');

            return await SendTemplateToEndpointAsync(param.Value, templateBasePath, endpointUrl).ConfigureAwait(false);
        }

        /// <summary>
        /// Builds a preview for a specific template value (without e: parameters)
        /// </summary>
        public static async Task<(bool Success, List<PreviewFrame> Frames, string Message)> BuildPreviewForTemplateValueAsync(AppBase? app, string templateValue)
        {
            if (app?.Configuration?.Arguments == null)
            {
                return (false, new List<PreviewFrame>(), "Pixelit App-Konfiguration fehlt.");
            }

            var templatePathArg = app.Configuration.Arguments
                .FirstOrDefault(a => a.Name.Equals("TP", StringComparison.OrdinalIgnoreCase));
            var templateBasePath = templatePathArg?.Value;
            if (string.IsNullOrWhiteSpace(templateBasePath))
            {
                return (false, new List<PreviewFrame>(), "Kein Templates-Pfad (TP) konfiguriert.");
            }

            var commands = ParseTemplateCommands(templateValue);
            if (commands.Count == 0)
            {
                return (false, new List<PreviewFrame>(), "Keine Template-Befehle definiert.");
            }

            return await BuildPreviewFramesAsync(commands, templateBasePath).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends the configured Pixelit template to the device for testing.
        /// </summary>
        public static async Task<(bool Success, string Message)> TestTemplateAsync(AppBase? app, Argument param)
        {
            if (app?.Configuration?.Arguments == null)
            {
                return (false, "Pixelit App-Konfiguration fehlt.");
            }

            var endpointArg = app.Configuration.Arguments
                .FirstOrDefault(a => a.Name.Equals("PEPS", StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrWhiteSpace(endpointArg?.Value))
            {
                return (false, "Kein Pixelit-Endpunkt (PEPS) konfiguriert.");
            }

            var templatePathArg = app.Configuration.Arguments
                .FirstOrDefault(a => a.Name.Equals("TP", StringComparison.OrdinalIgnoreCase));
            var templateBasePath = templatePathArg?.Value;
            if (string.IsNullOrWhiteSpace(templateBasePath))
            {
                return (false, "Kein Templates-Pfad (TP) konfiguriert.");
            }

            var endpoint = endpointArg.Value
                .Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return (false, "Pixelit-Endpunkt (PEPS) ist leer.");
            }

            var endpointUrl = endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? endpoint
                : $"http://{endpoint}";
            endpointUrl = endpointUrl.TrimEnd('/');

            return await SendTemplateToEndpointAsync(param.Value, templateBasePath, endpointUrl).ConfigureAwait(false);
        }

        private static async Task<(bool Success, string Message)> SendTemplateToEndpointAsync(string? paramValue, string templateBasePath, string endpointUrl)
        {
            var commands = ParseTemplateCommands(paramValue);
            if (commands.Count == 0)
            {
                return (false, "Keine Template-Befehle definiert.");
            }

            foreach (var command in commands)
            {
                Debug.WriteLine($"[PixelitTest] Command: template='{command.TemplateName}', delay={command.DelayMs}, override='{command.TextOverride}'");

                string payload;
                var filePath = ResolveTemplatePath(templateBasePath, command.TemplateName);
                if (File.Exists(filePath) || File.Exists(Path.ChangeExtension(filePath, ".json")))
                {
                    if (!File.Exists(filePath))
                    {
                        filePath = Path.ChangeExtension(filePath, ".json");
                    }

                    Debug.WriteLine($"[PixelitTest] Loading template file: {filePath}");
                    payload = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
                    payload = ApplyPlaceholderReplacements(payload);

                    if (!string.IsNullOrEmpty(command.TextOverride))
                    {
                        var overrideText = command.TextOverride.Replace("{}", " ");
                        overrideText = ApplyPlaceholderReplacements(overrideText);
                        Debug.WriteLine($"[PixelitTest] Applying text override: '{overrideText}'");
                        payload = ApplyTextOverride(payload, overrideText);
                    }
                }
                else
                {
                    // Treat as inline payload (manual input)
                    payload = command.TextOverride ?? command.TemplateName;
                    payload = payload.Replace("{}", " ");
                    payload = ApplyPlaceholderReplacements(payload);
                    Debug.WriteLine($"[PixelitTest] Using inline payload (no file found) for '{command.TemplateName}' after replacements: '{payload}'");
                }

                var logPayload = FormatJsonSafe(payload);

                try
                {
                    var url = $"{endpointUrl}/api/screen";
                    var content = new StringContent(payload, Encoding.UTF8, "application/data");

                    var response = await HttpClient.PostAsync(url, content).ConfigureAwait(false);

                    var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Debug.WriteLine($"[PixelitTest] POST {url} => {(int)response.StatusCode} {response.StatusCode}");
                    Debug.WriteLine($"[PixelitTest] Request Payload (pretty): {logPayload}");
                    Debug.WriteLine($"[PixelitTest] Request Payload (raw): {payload}");
                    Debug.WriteLine($"[PixelitTest] Response: {responseBody}");

                    if (!response.IsSuccessStatusCode)
                    {
                        return (false, $"Senden fehlgeschlagen ({response.StatusCode}): {responseBody}");
                    }

                    Debug.WriteLine($"[PixelitTest] Template '{command.TemplateName}' gesendet an {endpointUrl}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PixelitTest] Fehler beim Senden: {ex.Message}");
                    return (false, $"Fehler: {ex.Message}");
                }

                if (command.DelayMs.HasValue && command.DelayMs.Value > 0)
                {
                    Debug.WriteLine($"[PixelitTest] Warte {command.DelayMs.Value}ms vor dem nächsten Template...");
                    await Task.Delay(command.DelayMs.Value).ConfigureAwait(false);
                }
            }

            return (true, "Template gesendet.");
        }

        private static string FormatJsonSafe(string payload)
        {
            try
            {
                var token = JToken.Parse(payload);
                return token.ToString(Newtonsoft.Json.Formatting.Indented);
            }
            catch
            {
                return payload;
            }
        }

        private class TemplateCommand
        {
            public TemplateCommand(string templateName, int? delayMs, string? textOverride)
            {
                TemplateName = templateName;
                DelayMs = delayMs;
                TextOverride = textOverride;
            }
            public string TemplateName { get; }
            public int? DelayMs { get; }
            public string? TextOverride { get; }
        }

        private static string ResolveTemplatePath(string basePath, string templateName)
        {
            var name = templateName;
            name = name.Trim('"', '\'', ' ');
            return Path.Combine(basePath, name);
        }

        private static string ApplyTextOverride(string payload, string overrideText)
        {
            try
            {
                var root = JToken.Parse(payload);
                bool replaced = false;
                JObject? firstTextObject = null;
                ApplyTextOverrideInternal(root, overrideText, ref replaced, ref firstTextObject);

                // If no textString was found, add one to the first text object or root
                if (!replaced)
                {
                    if (firstTextObject != null)
                    {
                        firstTextObject["textString"] = overrideText;
                        replaced = true;
                        Debug.WriteLine($"[PixelitTest] Added textString to text object: {overrideText}");
                    }
                    else if (root is JObject ro)
                    {
                        ro["textString"] = overrideText;
                        replaced = true;
                        Debug.WriteLine($"[PixelitTest] Added textString to root: {overrideText}");
                    }
                }

                return root.ToString(Newtonsoft.Json.Formatting.None);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PixelitTest] Text override failed, fallback to original payload: {ex.Message}");
                return payload;
            }
        }

        private static JProperty? FindPropertyIgnoreCase(JObject obj, string name)
        {
            return obj.Properties().FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        private static void ApplyTextOverrideInternal(JToken token, string text, ref bool replaced, ref JObject? firstTextObject)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    var obj = (JObject)token;

                    var textProp = FindPropertyIgnoreCase(obj, "text");
                    if (textProp != null)
                    {
                        if (firstTextObject == null)
                        {
                            firstTextObject = textProp.Value as JObject ?? obj;
                        }

                        if (textProp.Value is JObject textObj)
                        {
                            var tsProp = FindPropertyIgnoreCase(textObj, "textString");
                            if (tsProp != null)
                            {
                                tsProp.Value = text;
                                replaced = true;
                                Debug.WriteLine($"[PixelitTest] textString overridden in text object: {text}");
                            }
                            else
                            {
                                textObj["textString"] = text;
                                replaced = true;
                                Debug.WriteLine($"[PixelitTest] textString added to text object: {text}");
                            }
                        }
                    }

                    var directTextString = FindPropertyIgnoreCase(obj, "textString");
                    if (directTextString != null)
                    {
                        directTextString.Value = text;
                        replaced = true;
                        Debug.WriteLine($"[PixelitTest] textString overridden: {text}");
                    }

                    foreach (var child in obj.Properties())
                    {
                        ApplyTextOverrideInternal(child.Value, text, ref replaced, ref firstTextObject);
                    }
                    break;

                case JTokenType.Array:
                    foreach (var child in token.Children())
                    {
                        ApplyTextOverrideInternal(child, text, ref replaced, ref firstTextObject);
                    }
                    break;
            }
        }

        private static List<TemplateCommand> ParseTemplateCommands(string? value)
        {
            var commands = new List<TemplateCommand>();
            if (string.IsNullOrWhiteSpace(value)) return commands;

            // Extract tokens inside quotes first; fallback to whitespace split
            var matches = System.Text.RegularExpressions.Regex.Matches(value, "\"([^\"]+)\"");
            var tokens = matches.Count > 0
                ? matches.Select(m => m.Groups[1].Value).ToList()
                : value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            foreach (var token in tokens)
            {
                var cleaned = token.Trim();
                if (string.IsNullOrWhiteSpace(cleaned)) continue;

                var parts = cleaned.Split('|');
                var templateName = parts[0];
                int? delayMs = null;
                string? textOverride = null;

                foreach (var part in parts.Skip(1))
                {
                    if (string.IsNullOrWhiteSpace(part)) continue;

                    if (part.StartsWith("d", StringComparison.OrdinalIgnoreCase))
                    {
                        var durationValue = part.TrimStart('d', 'D').TrimStart(':');
                        if (int.TryParse(durationValue, out var ms) && ms > 0)
                        {
                            delayMs = ms;
                        }
                    }
                    else if (part.StartsWith("t:", StringComparison.OrdinalIgnoreCase))
                    {
                        textOverride = part.Substring(2);
                    }
                }

                commands.Add(new TemplateCommand(templateName, delayMs, textOverride));
            }

            return commands;
        }

        private static string ApplyPlaceholderReplacements(string templateContent)
        {
            if (string.IsNullOrEmpty(templateContent)) return templateContent;

            var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["playername"] = "Player",
                ["player"] = "Player",
                ["name"] = "Player",
                ["score-left"] = "0",
                ["score_left"] = "0",
                ["score"] = "26",
                ["points-left"] = "000",
                ["game-mode"] = "X1",
                ["gamemode"] = "X1",
                ["game_mode"] = "X1",
                ["game-mode-extra"] = "501",
                ["gamemodeextra"] = "501",
                ["game_mode_extra"] = "501"
            };

            // Try JSON parse to only replace inside string values
            try
            {
                if (templateContent.TrimStart().StartsWith("{") || templateContent.TrimStart().StartsWith("["))
                {
                    var root = JToken.Parse(templateContent);
                    ReplacePlaceholdersInStrings(root, replacements);
                    return root.ToString(Newtonsoft.Json.Formatting.None);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PixelitTest] Placeholder parsing failed, fallback to raw: {ex.Message}");
            }

            // Fallback: replace in plain text
            return ReplacePlaceholdersInText(templateContent, replacements);
        }

        private static string ReplacePlaceholdersInText(string text, Dictionary<string, string> replacements)
        {
            return System.Text.RegularExpressions.Regex.Replace(
                text,
                "\\{([^{}]+)\\}",
                m =>
                {
                    var key = m.Groups[1].Value.Trim();
                    if (replacements.TryGetValue(key, out var val))
                    {
                        Debug.WriteLine($"[PixelitTest] Placeholder '{{{key}}}' -> '{val}'");
                        return val;
                    }
                    Debug.WriteLine($"[PixelitTest] Placeholder '{{{key}}}' unknown -> '' (leer ersetzt)");
                    return string.Empty;
                },
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline);
        }

        private static void ReplacePlaceholdersInStrings(JToken token, Dictionary<string, string> replacements)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (var child in token.Children<JProperty>())
                    {
                        ReplacePlaceholdersInStrings(child.Value, replacements);
                    }
                    break;
                case JTokenType.Array:
                    foreach (var child in token.Children())
                    {
                        ReplacePlaceholdersInStrings(child, replacements);
                    }
                    break;
                case JTokenType.String:
                    var value = token.Value<string>() ?? string.Empty;
                    var replaced = ReplacePlaceholdersInText(value, replacements);
                    if (!ReferenceEquals(value, replaced))
                    {
                        ((JValue)token).Value = replaced;
                    }
                    break;
            }
        }

        public record PreviewFrame(string Payload, string? TextString, int DelayMs);

        public static async Task<(bool Success, List<PreviewFrame> Frames, string Message)> BuildPreviewPayloadAsync(AppBase? app, Argument param)
        {
            if (app?.Configuration?.Arguments == null)
            {
                return (false, new List<PreviewFrame>(), "Pixelit App-Konfiguration fehlt.");
            }

            var endpointArg = app.Configuration.Arguments
                .FirstOrDefault(a => a.Name.Equals("PEPS", StringComparison.OrdinalIgnoreCase));
            var templatePathArg = app.Configuration.Arguments
                .FirstOrDefault(a => a.Name.Equals("TP", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(endpointArg?.Value))
            {
                return (false, new List<PreviewFrame>(), "Kein Pixelit-Endpunkt (PEPS) konfiguriert.");
            }

            var templateBasePath = templatePathArg?.Value;
            if (string.IsNullOrWhiteSpace(templateBasePath))
            {
                return (false, new List<PreviewFrame>(), "Kein Templates-Pfad (TP) konfiguriert.");
            }

            var commands = ParseTemplateCommands(param.Value);
            if (commands.Count == 0)
            {
                return (false, new List<PreviewFrame>(), "Keine Template-Befehle definiert.");
            }

            return await BuildPreviewFramesAsync(commands, templateBasePath).ConfigureAwait(false);
        }

        private static async Task<(bool Success, List<PreviewFrame> Frames, string Message)> BuildPreviewFramesAsync(List<TemplateCommand> commands, string templateBasePath)
        {
            var frames = new List<PreviewFrame>();
            foreach (var command in commands)
            {
                string payload;
                var filePath = ResolveTemplatePath(templateBasePath, command.TemplateName);
                if (File.Exists(filePath) || File.Exists(Path.ChangeExtension(filePath, ".json")))
                {
                    if (!File.Exists(filePath))
                    {
                        filePath = Path.ChangeExtension(filePath, ".json");
                    }

                    payload = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
                    payload = ApplyPlaceholderReplacements(payload);

                    if (!string.IsNullOrEmpty(command.TextOverride))
                    {
                        var overrideText = command.TextOverride.Replace("{}", " ");
                        overrideText = ApplyPlaceholderReplacements(overrideText);
                        payload = ApplyTextOverride(payload, overrideText);
                    }
                }
                else
                {
                    payload = command.TextOverride ?? command.TemplateName;
                    payload = payload.Replace("{}", " ");
                    payload = ApplyPlaceholderReplacements(payload);
                }

                var overrideTextForFrame = command.TextOverride?.Replace("{}", " ");
                if (overrideTextForFrame != null)
                {
                    overrideTextForFrame = ApplyPlaceholderReplacements(overrideTextForFrame);
                }
                var textString = overrideTextForFrame ?? ExtractFirstTextString(payload);
                var pretty = FormatJsonSafe(payload);

                int delayMs;
                if (command.DelayMs.HasValue)
                {
                    delayMs = command.DelayMs.Value;
                }
                else
                {
                    delayMs = EstimateDisplayTime(payload, textString);
                }

                frames.Add(new PreviewFrame(pretty, textString, delayMs));
            }

            return (true, frames, string.Empty);
        }

        private static int EstimateDisplayTime(string payload, string? textString)
        {
            const int defaultDelay = 5000;
            const int matrixWidth = 32;
            const int charWidth = 4; // 3px font + 1px spacing

            if (string.IsNullOrWhiteSpace(textString))
                return 2000;

            int scrollDelayMs = 100;
            try
            {
                var root = JToken.Parse(payload);
                var text = root.SelectToken("text");
                if (text != null)
                {
                    var sd = text["scrollTextDelay"] ?? text["scrollDelay"];
                    if (sd != null && int.TryParse(sd.ToString(), out var parsed) && parsed > 0)
                    {
                        scrollDelayMs = parsed;
                    }
                }
            }
            catch { }

            int textColumns = textString.Length * charWidth + 2;
            int totalSteps = textColumns + matrixWidth;
            int estimatedMs = totalSteps * scrollDelayMs;

            return Math.Max(defaultDelay, estimatedMs);
        }

        private static string? ExtractFirstTextString(string payload)
        {
            try
            {
                var token = JToken.Parse(payload);
                return FindTextString(token);
            }
            catch
            {
                // Try simple regex
                var match = Regex.Match(payload, "\"textString\"\\s*:\\s*\"([^\\\"]*)\"", RegexOptions.IgnoreCase);
                return match.Success ? match.Groups[1].Value : null;
            }
        }

        private static string? FindTextString(JToken token)
        {
            if (token.Type == JTokenType.Object)
            {
                foreach (var prop in ((JObject)token).Properties())
                {
                    if (prop.Name.Equals("textString", StringComparison.OrdinalIgnoreCase))
                    {
                        return prop.Value?.ToString();
                    }

                    var childResult = FindTextString(prop.Value);
                    if (!string.IsNullOrEmpty(childResult)) return childResult;
                }
            }
            else if (token.Type == JTokenType.Array)
            {
                foreach (var child in token.Children())
                {
                    var childResult = FindTextString(child);
                    if (!string.IsNullOrEmpty(childResult)) return childResult;
                }
            }

            return null;
        }
    }
}
