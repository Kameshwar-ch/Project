using System;
using System.Collections;
using Skillforge.Domain;
using Skillforge.Dto.ComplianceRecordDto;

namespace Skillforge.Repository;

public interface IComplianceRecord
{
    /// <summary>
    /// Asynchronously gets the list of compliance records
    /// </summary>
    public Task<IEnumerable<ComplianceRecord>> GetComplianceRecordAsync();

    /// <summary>
    /// Asynchronously adds a list of compliance records to database
    /// </summary>
    public Task PostComplianceRecords(IEnumerable<ComplianceRecord> complianceRecords);

    /// <summary>
    /// Asynchronously Deletes the records from table
    /// </summary>
    public Task DeleteComplianceRecords();

    /// <summary>
    /// Asynchronously ad a single reard to datalase
    /// </summary>
    public Task AddComplianceRecord(ComplianceRecord cr);
}
