using System;

namespace Skillforge.Dto.ComplianceRecordDto;

/// <summary>
/// Compliance Summary Dto helps in giving a standard Summary of Certifications 
/// This dto also provides metrics
/// </summary>
public record ComplianceSummaryDto(
    int TotalEmployees,
    int CompliantCount,
    int NonCompliantCount,
    double ComplianceRate,
    List<GetComplianceDto> Records
);