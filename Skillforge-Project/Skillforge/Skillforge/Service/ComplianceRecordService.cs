using System;
using Skillforge.Domain;
using Skillforge.Dto.ComplianceRecordDto;
using Skillforge.Repository;
using Skillforge.Utility;

namespace Skillforge.Service;
/// <summary>
/// Contains business logic for compliance record operations including summary generation and refresh.
/// </summary>
public class ComplianceRecordService : IComplianceRecordService
{
    private readonly IComplianceRecord _complianceRecordRepo;
    private readonly ICertificationRepository _CertificationRepository;
    public ComplianceRecordService(IComplianceRecord ComplianceRecordRepository, ICertificationRepository CertificationRepository)
    {
        _complianceRecordRepo = ComplianceRecordRepository;
        _CertificationRepository = CertificationRepository;
    }

    /// <summary>
    /// This function gets the complete summary of all the employees, and tells whether they are compliant/non-compliant
    /// </summary>
    public async Task<ComplianceSummaryDto> GetComplianceSummaryAsync()
    {
        IEnumerable<ComplianceRecord> crs = await _complianceRecordRepo.GetComplianceRecordAsync();
        List<GetComplianceDto> ComplianceRecordDtos = crs.Select(c => new GetComplianceDto(c.ComplianceID, c.EmployeeID, c.Employee.Name, c.CertificationID, c.Certification.Course.Title, c.Status, c.Date)).ToList();
        int TotalEmp = crs.Select(c => c.EmployeeID).Distinct().Count();
        if (TotalEmp == 0)
            throw new DivideByZeroException(ComplianceRecordUtility.DivideByZero);
        int CompliantEmp = crs.GroupBy(c => c.EmployeeID).Count(c => c.All(c => c.Status));
        int NonCompliantEmp = TotalEmp - CompliantEmp;
        double CompliantPercent = ((1.0 * CompliantEmp) / TotalEmp) * 100;
        ComplianceSummaryDto csd = new ComplianceSummaryDto(TotalEmp, CompliantEmp, NonCompliantEmp, CompliantPercent, ComplianceRecordDtos);
        return csd;
    }

    /// <summary>
    /// This Updates the compliance records table with the latest updates from the certification table
    /// </summary>
    public async Task<string> UpdateComplianceRecords()
    {
        try
        {
            await _complianceRecordRepo.DeleteComplianceRecords();
        }
        catch (Exception)
        {
            throw new Exception(ComplianceRecordUtility.DeleteComplianceRecordsFailed);
        }

        List<Certification> certifications;
        try
        {
            certifications = await _CertificationRepository.GetAllCertifications();
        }
        catch (Exception)
        {
            throw new Exception(ComplianceRecordUtility.FetchCertificationsFailed);
        }

        List<ComplianceRecord> complianceRecords = new List<ComplianceRecord>();
        DateTime today = DateTime.UtcNow;
        foreach (Certification certificate in certifications)
        {
            ComplianceRecord cr = new ComplianceRecord();
            cr.CertificationID = certificate.CertificationID;
            cr.EmployeeID = certificate.EmployeeID;
            cr.Date = today;
            if (certificate.ExpiryDate >= today)
                cr.Status = true;
            else
                cr.Status = false;
            complianceRecords.Add(cr);
        }
        await _complianceRecordRepo.PostComplianceRecords(complianceRecords);
        return ComplianceRecordUtility.UpdateComplianceRecords;
    }

}
