using System;
using Skillforge.Dto;

namespace Skillforge.Service;

public interface IResultService
{
    Task SubmitResultAsync(SubmitAssessmentResultDto request, int reviewerId);
    Task SelfSubmitAsync(int assessmentId, decimal score, int employeeId);
    Task<List<PendingResultDto>> GetPendingResultsAsync(int trainerId);
    Task EvaluateResultAsync(int assessmentId, int employeeId, int trainerId, bool pass);
}
