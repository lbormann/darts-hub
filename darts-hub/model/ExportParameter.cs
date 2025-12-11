using System;

namespace darts_hub.model
{
    /// <summary>
    /// Simplified parameter representation for export/import
    /// Contains only the essential fields: NameHuman and Value
    /// </summary>
    public class ExportParameter
    {
        /// <summary>
        /// The internal parameter name (e.g., "U", "P", "B")
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// The human-readable parameter name (e.g., "-U / --autodarts_email")
        /// </summary>
        public string NameHuman { get; set; }
        
        /// <summary>
        /// The parameter value
        /// </summary>
        public string Value { get; set; }
    }
}
