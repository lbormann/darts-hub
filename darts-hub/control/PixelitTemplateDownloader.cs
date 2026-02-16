using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;

namespace darts_hub.control
{
    internal static class PixelitTemplateDownloader
    {
        private const string ArchiveUrl = "https://github.com/lbormann/darts-pixelit/archive/42a56b9babafbc9178e993c403ed829576cf1527.zip";
        private const string ArchiveRoot = "darts-pixelit-42a56b9babafbc9178e993c403ed829576cf1527";
        private const string TemplatesFolder = "community/templates/";

        public static void EnsureTemplatesDownloaded(string targetDirectory)
        {
            if (string.IsNullOrWhiteSpace(targetDirectory)) return;

            try
            {
                if (Directory.Exists(targetDirectory) && Directory.EnumerateFiles(targetDirectory, "*", SearchOption.AllDirectories).Any())
                {
                    return;
                }

                Directory.CreateDirectory(targetDirectory);

                using var httpClient = new HttpClient();
                using var archiveStream = httpClient.GetStreamAsync(ArchiveUrl).GetAwaiter().GetResult();
                using var buffer = new MemoryStream();
                archiveStream.CopyTo(buffer);
                buffer.Position = 0;

                using var archive = new ZipArchive(buffer, ZipArchiveMode.Read);
                var prefix = $"{ArchiveRoot}/{TemplatesFolder}";

                foreach (var entry in archive.Entries)
                {
                    if (entry.FullName.EndsWith("/", StringComparison.Ordinal)) continue;
                    if (!entry.FullName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;

                    var relativePath = entry.FullName[prefix.Length..];
                    var destinationPath = Path.Combine(targetDirectory, relativePath);
                    var destinationDir = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrWhiteSpace(destinationDir))
                    {
                        Directory.CreateDirectory(destinationDir);
                    }

                    using var entryStream = entry.Open();
                    using var fileStream = File.Create(destinationPath);
                    entryStream.CopyTo(fileStream);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PixelitTemplates] Failed to download templates: {ex.Message}");
            }
        }
    }
}
