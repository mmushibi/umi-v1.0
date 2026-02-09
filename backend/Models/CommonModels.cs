using System.Collections.Generic;

namespace UmiHealthPOS.Models
{
    public class CsvImportResult
    {
        public int ImportedCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}
