using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace UmiHealthPOS.Services
{
    public class UpdateService : IUpdateService
    {
        private readonly ILogger<UpdateService> _logger;
        private readonly IConfiguration _configuration;

        public UpdateService(ILogger<UpdateService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<UpdateCheckResult> CheckForUpdatesAsync()
        {
            try
            {
                var currentVer = GetApplicationVersion();
                var latestVer = await GetLatestVersionAsync();
                
                var updateAvailable = CompareVersions(currentVer, latestVer, out var breakingChanges, out var newFeatures);
                
                return new UpdateCheckResult
                {
                    UpdateAvailable = updateAvailable,
                    CurrentVersion = currentVer,
                    LatestVersion = latestVer,
                    ReleaseNotes = GenerateReleaseNotes(updateAvailable, breakingChanges, newFeatures),
                    DownloadUrl = updateAvailable ? "https://github.com/umihealth-pos/releases/latest" : string.Empty,
                    LastChecked = DateTime.UtcNow,
                    IsPrerelease = IsPrerelease(currentVer, latestVer),
                    BreakingChanges = breakingChanges,
                    NewFeatures = newFeatures,
                    AvailableAt = updateAvailable ? DateTime.UtcNow : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for updates");
                
                return new UpdateCheckResult
                {
                    UpdateAvailable = false,
                    CurrentVersion = GetApplicationVersion(), // Fallback to current on error
                    LatestVersion = GetApplicationVersion(), // Fallback to current on error
                    ReleaseNotes = "Error checking for updates: " + ex.Message,
                    LastChecked = DateTime.UtcNow
                };
            }
        }

        private string GetApplicationVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "2.1.0";
        }

        private async Task<string> GetLatestVersionAsync()
        {
            // Simulate version check - in production, this would call actual update API
            await Task.Delay(500); // Simulate network call
            
            // For demo, return a newer version
            return "2.1.1";
        }

        private bool CompareVersions(string current, string latest, out List<string> breakingChanges, out List<string> newFeatures)
        {
            var currentVersionObj = new Version(current);
            var latestVersionObj = new Version(latest);
            
            var updateAvailable = latestVersionObj > currentVersionObj;
            
            breakingChanges = new List<string>();
            newFeatures = new List<string>();
            
            if (updateAvailable)
            {
                // Simulate breaking changes
                if (latestVersionObj.Major > currentVersionObj.Major)
                {
                    breakingChanges.Add($"Major version update: {current} -> {latest}");
                }
                
                // Simulate new features
                newFeatures.Add("Enhanced reporting capabilities");
                newFeatures.Add("Improved dashboard performance");
                newFeatures.Add("Bug fixes and stability improvements");
            }
            
            return updateAvailable;
        }

        private bool IsPrerelease(string current, string latest)
        {
            try
            {
                var currentVersionObj = new Version(current);
                var latestVersionObj = new Version(latest);
                
                // Check if it's a prerelease (contains alpha, beta, rc, etc.)
                return latest.Contains("-") || latestVersionObj < currentVersionObj;
            }
            catch
            {
                return false;
            }
        }

        private string GenerateReleaseNotes(bool updateAvailable, List<string> breakingChanges, List<string> newFeatures)
        {
            if (!updateAvailable)
            {
                return "You are running the latest version.";
            }

            var notes = "Update available!\\n\\n";
            
            if (breakingChanges.Any())
            {
                notes += "⚠️ Breaking Changes:\\n";
                foreach (var change in breakingChanges)
                {
                    notes += $"- {change}\\n";
                }
                notes += "\\n";
            }
            
            if (newFeatures.Any())
            {
                notes += "✨ New Features:\\n";
                foreach (var feature in newFeatures)
                {
                    notes += $"- {feature}\\n";
                }
                notes += "\\n";
            }
            
            notes += "Please update to get the latest features and improvements.";
            
            return notes;
        }
    }
}
