using System;
using System.Collections.Generic;

namespace UmiHealthPOS.DTOs
{
    // Advanced Settings DTOs
    public class SettingsCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int SettingCount { get; set; }
        public int Order { get; set; }
    }

    public class SettingsImportData
    {
        public DateTime ExportDate { get; set; }
        public string Category { get; set; } = string.Empty;
        public int TotalSettings { get; set; }
        public List<ImportSettingDto> Settings { get; set; } = new();
    }

    public class ImportSettingDto
    {
        public string Category { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string? Environment { get; set; }
        public bool IsEncrypted { get; set; }
    }

    public class ImportResultDto
    {
        public int TotalRecords { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public bool IsSuccess => FailedCount == 0;
    }

    public class SettingValidationRequest
    {
        public string Category { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
    }

    public class SettingValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    public class ValidationResultDto
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    public class ResetSettingsRequest
    {
        public string? Category { get; set; }
        public List<string>? Keys { get; set; }
    }

    public class SettingsAuditFilterDto
    {
        public string? Category { get; set; }
        public string? Key { get; set; }
        public string? Action { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class SettingsAuditLogDto
    {
        public int Id { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
