using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;

namespace UmiHealthPOS.Filters
{
    public class PerformanceMonitoringFilter : IActionFilter
    {
        private readonly ILogger<PerformanceMonitoringFilter> _logger;

        public PerformanceMonitoringFilter(ILogger<PerformanceMonitoringFilter> logger)
        {
            _logger = logger;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            context.HttpContext.Items["ActionStartTime"] = Stopwatch.StartNew();
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.HttpContext.Items.TryGetValue("ActionStartTime", out var startTimeObj) &&
                startTimeObj is Stopwatch stopwatch)
            {
                stopwatch.Stop();
                var duration = stopwatch.ElapsedMilliseconds;
                var actionName = context.ActionDescriptor.DisplayName;
                var path = context.HttpContext.Request.Path;

                // Log performance metrics
                if (duration > 1000) // Log warnings for slow requests
                {
                    _logger.LogWarning("Slow request detected: {Action} took {Duration}ms for path {Path}",
                        actionName, duration, path);
                }
                else
                {
                    _logger.LogInformation("Request completed: {Action} took {Duration}ms for path {Path}",
                        actionName, duration, path);
                }

                // Add performance headers
                context.HttpContext.Response.Headers["X-Response-Time"] = $"{duration}ms";

                // Add to performance metrics for monitoring
                var metrics = context.HttpContext.Items["PerformanceMetrics"] as List<PerformanceMetric> ?? new List<PerformanceMetric>();
                metrics.Add(new PerformanceMetric
                {
                    ActionName = actionName,
                    Path = path,
                    Duration = duration,
                    Timestamp = DateTime.UtcNow,
                    StatusCode = context.HttpContext.Response.StatusCode
                });
                context.HttpContext.Items["PerformanceMetrics"] = metrics;
            }
        }
    }

    public class PerformanceMetric
    {
        public string ActionName { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public long Duration { get; set; }
        public DateTime Timestamp { get; set; }
        public int StatusCode { get; set; }
    }
}
