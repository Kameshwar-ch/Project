using System;
using Microsoft.EntityFrameworkCore;
using Skillforge.Data;
using Skillforge.Domain;
using Skillforge.Dto.ComplianceRecordDto;

namespace Skillforge.Repository;

public class ComplianceRecordRepository : IComplianceRecord
{
    /// <summary>
    /// This repo is directly dependent upon the database layer so this needs the context class
    /// we intake the context class using the dependency injection
    /// </summary>
    private readonly SkillForgeDB _context;
    public ComplianceRecordRepository(SkillForgeDB context)
    {
        _context = context;
    }


    public async Task<IEnumerable<ComplianceRecord>> GetComplianceRecordAsync()
    {
        return await _context.ComplianceRecords.Include(c => c.Certification).ThenInclude(c => c.Course).Include(c => c.Employee).ToListAsync();
    }

    public async Task PostComplianceRecords(IEnumerable<ComplianceRecord> complianceRecords)
    {
        await _context.ComplianceRecords.AddRangeAsync(complianceRecords);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteComplianceRecords()
    {
        await _context.ComplianceRecords.ExecuteDeleteAsync();
    }

    public async Task AddComplianceRecord(ComplianceRecord cr)
    {
        _context.ComplianceRecords.Add(cr);
        await _context.SaveChangesAsync();
    }
}
