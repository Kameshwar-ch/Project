using System;
using Skillforge.Dto.ComplianceRecordDto;

namespace Skillforge.Service;

public interface IComplianceRecordService
{
    public Task<ComplianceSummaryDto> GetComplianceSummaryAsync();
    public Task<string> UpdateComplianceRecords();
}
