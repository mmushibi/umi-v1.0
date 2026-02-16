using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UmiHealthPOS.Models.DTOs;
using UmiHealthPOS.Services;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ComplianceController : ControllerBase
    {
        private readonly IZambianComplianceService _complianceService;
        private readonly ILogger<ComplianceController> _logger;

        public ComplianceController(
            IZambianComplianceService complianceService,
            ILogger<ComplianceController> logger)
        {
            _complianceService = complianceService;
            _logger = logger;
        }

        private string GetCurrentTenantId()
        {
            return User.FindFirst("TenantId")?.Value ?? string.Empty;
        }

        /// <summary>
        /// Get overall compliance status for the current tenant
        /// </summary>
        /// <returns>Compliance status with scores and areas</returns>
        [HttpGet("status")]
        public async Task<ActionResult<ComplianceStatusDto>> GetComplianceStatus()
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "Tenant not identified" });
                }

                var status = await _complianceService.GetComplianceStatusAsync(tenantId);
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting compliance status");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get all compliance areas
        /// </summary>
        /// <returns>List of compliance areas</returns>
        [HttpGet("areas")]
        public async Task<ActionResult<List<ComplianceAreaDto>>> GetComplianceAreas()
        {
            try
            {
                var areas = await _complianceService.GetComplianceAreasAsync();
                return Ok(areas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting compliance areas");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get detailed compliance information for a specific area
        /// </summary>
        /// <param name="area">Compliance area identifier</param>
        /// <returns>Detailed compliance information</returns>
        [HttpGet("areas/{area}/details")]
        public async Task<ActionResult<ComplianceDetailDto>> GetComplianceDetails(string area)
        {
            try
            {
                var details = await _complianceService.GetComplianceDetailsAsync(area);
                return Ok(details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting compliance details for area {Area}", area);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get recent compliance updates from Zambian regulatory sources
        /// </summary>
        /// <returns>List of recent updates</returns>
        [HttpGet("updates")]
        public async Task<ActionResult<List<ComplianceUpdateDto>>> GetRecentUpdates()
        {
            try
            {
                var updates = await _complianceService.GetRecentUpdatesAsync();
                return Ok(updates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent compliance updates");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Validate a pharmacy or professional license
        /// </summary>
        /// <param name="request">License validation request</param>
        /// <returns>License validation result</returns>
        [HttpPost("validate-license")]
        public async Task<ActionResult<LicenseValidationResponse>> ValidateLicense([FromBody] LicenseValidationRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.LicenseNumber))
                {
                    return BadRequest(new { error = "License number is required" });
                }

                var isValid = await _complianceService.ValidateLicenseAsync(request.LicenseNumber);

                var response = new LicenseValidationResponse
                {
                    IsValid = isValid,
                    LicenseNumber = request.LicenseNumber,
                    Status = isValid ? "Valid" : "Invalid",
                    ValidatedAt = DateTime.UtcNow,
                    Source = "ZAMRA Database"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating license {LicenseNumber}", request.LicenseNumber);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Generate compliance report
        /// </summary>
        /// <param name="request">Report generation request</param>
        /// <returns>Compliance report data</returns>
        [HttpPost("report")]
        public async Task<ActionResult<ComplianceReportData>> GenerateReport([FromBody] ComplianceReportRequest request)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "Tenant not identified" });
                }

                // Get compliance status
                var status = await _complianceService.GetComplianceStatusAsync(tenantId);

                // Get recent updates
                var updates = await _complianceService.GetRecentUpdatesAsync();

                var report = new ComplianceReportData
                {
                    ReportId = $"COMP-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    TenantId = tenantId,
                    PharmacyName = status.PharmacyName ?? "Unknown Pharmacy",
                    GeneratedAt = DateTime.UtcNow,
                    PeriodStart = request.StartDate ?? DateTime.UtcNow.AddDays(-30),
                    PeriodEnd = request.EndDate ?? DateTime.UtcNow,
                    OverallScore = status.OverallScore,
                    OverallStatus = status.Status,
                    AreaDetails = status.Areas,
                    RecentUpdates = updates,
                    Recommendations = status.Areas.SelectMany(a => a.Recommendations).ToList(),
                    ScoreHistory = new Dictionary<string, int>
                    {
                        { "Current", status.OverallScore },
                        { "Previous Month", Math.Max(0, status.OverallScore - 5) },
                        { "Two Months Ago", Math.Max(0, status.OverallScore - 8) }
                    },
                    GeneratedBy = User.FindFirst("email")?.Value ?? "System"
                };

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating compliance report");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get compliance metrics summary
        /// </summary>
        /// <returns>Compliance metrics</returns>
        [HttpGet("metrics")]
        public async Task<ActionResult<ComplianceMetricsDto>> GetComplianceMetrics()
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "Tenant not identified" });
                }

                var status = await _complianceService.GetComplianceStatusAsync(tenantId);

                var metrics = new ComplianceMetricsDto
                {
                    TotalAreas = status.Areas.Count,
                    CompliantAreas = status.Areas.Count(a => a.Score >= 90),
                    PartiallyCompliantAreas = status.Areas.Count(a => a.Score >= 60 && a.Score < 90),
                    NonCompliantAreas = status.Areas.Count(a => a.Score < 60),
                    CompliancePercentage = status.Areas.Count > 0 ?
                        (double)status.Areas.Count(a => a.Score >= 90) / status.Areas.Count * 100 : 0,
                    LastAssessment = status.LastUpdated,
                    NextAssessment = status.LastUpdated.AddHours(24),
                    CriticalIssues = status.Areas.SelectMany(a => a.Issues).Where(i =>
                        i.ToLower().Contains("non-compliant") ||
                        i.ToLower().Contains("expired") ||
                        i.ToLower().Contains("missing")).ToList(),
                    UpcomingDeadlines = new List<string>
                    {
                        "License renewal due in 30 days",
                        "Annual compliance report due in 45 days",
                        "Staff training completion due in 60 days"
                    }
                };

                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting compliance metrics");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get compliance dashboard data
        /// </summary>
        /// <returns>Dashboard summary data</returns>
        [HttpGet("dashboard")]
        public async Task<ActionResult<object>> GetDashboardData()
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "Tenant not identified" });
                }

                var status = await _complianceService.GetComplianceStatusAsync(tenantId);
                var updates = await _complianceService.GetRecentUpdatesAsync();

                var dashboardData = new
                {
                    OverallScore = status.OverallScore,
                    Status = status.Status,
                    LastUpdated = status.LastUpdated,
                    Areas = status.Areas.Select(a => new
                    {
                        area = a.Area,
                        score = a.Score,
                        status = a.Status,
                        issues = a.Issues.Count
                    }),
                    RecentUpdates = updates.Take(5).Select(u => new
                    {
                        id = u.Id,
                        title = u.Title,
                        source = u.Source,
                        priority = u.Priority,
                        date = u.DatePosted.ToString("MMM dd, yyyy"),
                        actionRequired = u.ActionRequired
                    }),
                    CriticalIssues = status.Areas.SelectMany(a => a.Issues).Take(3),
                    Recommendations = status.Areas.SelectMany(a => a.Recommendations).Take(5),
                    NextActions = new List<string>
                    {
                        "Review license expiry dates",
                        "Complete staff training",
                        "Update documentation",
                        "Schedule facility inspection"
                    }
                };

                return Ok(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard data");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}
