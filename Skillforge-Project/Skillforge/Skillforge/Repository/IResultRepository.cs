using System;
using Skillforge.Domain;
namespace Skillforge.Repository;

public interface IResultRepository
{
    Task SubmitAssessmentResult(Result result);
    Task AddAuditLog(AuditLog auditLog);
    Task<List<Result>> GetPendingResultsForTrainerAsync(int trainerId);
    Task<Result?> GetResultByCompositeKeyAsync(int assessmentId, int employeeId);
    Task UpdateResultAsync(Result result);
}
