using System;
using Skillforge.Domain;
using Skillforge.Data;
using Microsoft.EntityFrameworkCore;
namespace Skillforge.Repository;

public class ResultRepository : IResultRepository
{
    private readonly SkillForgeDB context;

    public ResultRepository(SkillForgeDB context)
    {
        this.context = context;
    }

    public async Task AddAuditLog(AuditLog auditLog)
    {
        await context.AuditLogs.AddAsync(auditLog);
        await context.SaveChangesAsync();
    }

    public async Task SubmitAssessmentResult(Result result)
    {
        await context.Results.AddAsync(result);
        await context.SaveChangesAsync();
    }

    public async Task<List<Result>> GetPendingResultsForTrainerAsync(int trainerId)
    {
        return await context.Results
            .Include(r => r.Assessment)
                .ThenInclude(a => a.Course)
            .Include(r => r.UserRoleEmployee)
            .Where(r => r.Status == ResultStatus.Pending
                     && r.Assessment.Course.TrainerID == trainerId)
            .ToListAsync();
    }

    public async Task<Result?> GetResultByCompositeKeyAsync(int assessmentId, int employeeId)
    {
        return await context.Results
            .Include(r => r.Assessment)
                .ThenInclude(a => a.Course)
            .FirstOrDefaultAsync(r => r.AssessmentID == assessmentId && r.EmployeeID == employeeId);
    }

    public async Task UpdateResultAsync(Result result)
    {
        context.Results.Update(result);
        await context.SaveChangesAsync();
    }
}
