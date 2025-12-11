using System;
using System.Collections.Generic;

namespace darts_hub.model
{
    /// <summary>
    /// Metadata for exported configuration files
    /// </summary>
    public class ExportMetadata
    {
        /// <summary>
        /// Export type identifier
        /// </summary>
        public ExportType Type { get; set; }
        
        /// <summary>
        /// Version of the export format
        /// </summary>
        public string Version { get; set; } = "1.0";
        
        /// <summary>
        /// Timestamp of the export
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Source application version
        /// </summary>
        public string AppVersion { get; set; }
        
        /// <summary>
        /// Name(s) of the extension(s) included in the export
        /// </summary>
        public List<string> ExtensionNames { get; set; } = new List<string>();
        
        /// <summary>
        /// Optional: Specific parameter names if only partial export
        /// Key: Extension name, Value: List of parameter names
        /// </summary>
        public Dictionary<string, List<string>> ParameterNames { get; set; } = new Dictionary<string, List<string>>();
        
        /// <summary>
        /// Optional: Simplified parameter data for parameter exports (NameHuman + Value only)
        /// Key: Extension name, Value: List of simplified parameters
        /// </summary>
        public Dictionary<string, List<ExportParameter>> ParameterData { get; set; } = new Dictionary<string, List<ExportParameter>>();
        
        /// <summary>
        /// Optional description/notes for the export
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// The exported data (for Full and Extension exports)
        /// </summary>
        public List<AppDownloadable> Data { get; set; } = new List<AppDownloadable>();
    }
    
    /// <summary>
    /// Type of export
    /// </summary>
    public enum ExportType
    {
        /// <summary>
        /// Full export of all extensions and their configurations
        /// </summary>
        Full,
        
        /// <summary>
        /// One or more complete extensions
        /// </summary>
        Extensions,
        
        /// <summary>
        /// Specific parameters from one or more extensions
        /// </summary>
        Parameters
    }
}
