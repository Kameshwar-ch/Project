using System;

namespace Skillforge.Dto.ComplianceRecordDto;

/// <summary>
/// Dto to get the list of all the compliance records
/// </summary>
public record GetComplianceDto(
int ComplianceId,    
int EmployeeId,
string EmployeeName,
int CertificationId,
string CourseName,
bool Status,
DateTime Date
);
