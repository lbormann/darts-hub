using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace darts_hub.control
{
    public class PixelitTemplate
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Value { get; set; } = string.Empty;
        public List<string> AllowedArguments { get; set; } = new();
    }

    public static class PixelitTemplateProvider
    {
        private static readonly object SyncRoot = new();
        private static List<PixelitTemplate> cachedTemplates = new();
        private static DateTime? lastWriteTimeUtc;

        public static IReadOnlyList<PixelitTemplate> GetTemplatesForArgument(string argumentName)
        {
            var templates = GetTemplates();
            if (string.IsNullOrWhiteSpace(argumentName)) return templates;

            var argKey = argumentName.Trim();
            return templates.Where(t => IsTemplateAllowed(t, argKey)).ToList();
        }

        public static IReadOnlyList<PixelitTemplate> GetTemplates()
        {
            try
            {
                var templateFilePath = GetTemplateFilePath();
                if (!File.Exists(templateFilePath))
                {
                    lock (SyncRoot)
                    {
                        cachedTemplates = new List<PixelitTemplate>();
                        lastWriteTimeUtc = null;
                        return cachedTemplates;
                    }
                }

                var fileInfo = new FileInfo(templateFilePath);
                lock (SyncRoot)
                {
                    if (lastWriteTimeUtc.HasValue && lastWriteTimeUtc == fileInfo.LastWriteTimeUtc && cachedTemplates.Count > 0)
                    {
                        return cachedTemplates;
                    }

                    var fileContent = File.ReadAllText(templateFilePath);
                    var templates = JsonConvert.DeserializeObject<List<PixelitTemplate>>(fileContent) ?? new List<PixelitTemplate>();
                    cachedTemplates = templates
                        .Where(t => !string.IsNullOrWhiteSpace(t?.Value))
                        .Select(t => new PixelitTemplate
                        {
                            Name = t?.Name?.Trim() ?? string.Empty,
                            Description = t?.Description?.Trim(),
                            Value = t?.Value?.Trim() ?? string.Empty,
                            AllowedArguments = t?.AllowedArguments?
                                .Where(x => !string.IsNullOrWhiteSpace(x))
                                .Select(x => x.Trim())
                                .ToList() ?? new List<string>()
                        })
                        .Where(t => !string.IsNullOrWhiteSpace(t.Value))
                        .ToList();

                    lastWriteTimeUtc = fileInfo.LastWriteTimeUtc;
                    return cachedTemplates;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PixelitTemplates] Failed to load templates: {ex.Message}");
                lock (SyncRoot)
                {
                    cachedTemplates = new List<PixelitTemplate>();
                    lastWriteTimeUtc = null;
                    return cachedTemplates;
                }
            }
        }

        private static bool IsTemplateAllowed(PixelitTemplate template, string argumentName)
        {
            if (template.AllowedArguments == null || template.AllowedArguments.Count == 0) return true;

            var arg = argumentName.Trim();
            foreach (var allowed in template.AllowedArguments)
            {
                if (string.IsNullOrWhiteSpace(allowed)) continue;
                var token = allowed.Trim();
                if (token.Equals("ALL", StringComparison.OrdinalIgnoreCase)) return true;

                if (token.EndsWith("*", StringComparison.Ordinal))
                {
                    var prefix = token[..^1];
                    if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
                }
                else if (arg.Equals(token, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static string GetTemplateFilePath()
        {
            var basePath = Helper.GetAppBasePath();
            return Path.Combine(basePath, "configs", "pixelit_template_mapping.json");
        }
    }
}
