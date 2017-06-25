using System;

namespace Resonance.Data.Models
{
    public class ScanProgress
    {
        public ScanProgress()
        {
            StartDateTime = DateTime.UtcNow;
        }

        public int CurrentCollection { get; set; }
        public Guid CurrentCollectionId { get; set; }
        public long CurrentFile { get; set; }
        public string CurrentFilename { get; set; }
        public DateTime StartDateTime { get; set; }
        public int TotalCollectionCount { get; set; }
        public long TotalFileCount { get; set; }
    }
}