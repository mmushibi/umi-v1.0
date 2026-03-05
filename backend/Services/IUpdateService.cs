using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UmiHealthPOS.Services
{
    public interface IUpdateService
    {
        Task<UpdateCheckResult> CheckForUpdatesAsync();
    }

    public class UpdateCheckResult
    {
        public bool UpdateAvailable { get; set; }
        public string CurrentVersion { get; set; } = string.Empty;
        public string LatestVersion { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public DateTime? LastChecked { get; set; }
        public bool IsPrerelease { get; set; }
        public List<string> BreakingChanges { get; set; } = new List<string>();
        public List<string> NewFeatures { get; set; } = new List<string>();
        public DateTime? AvailableAt { get; set; }
    }
}
