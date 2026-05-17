using System;
using Skillforge.Dto;
using Skillforge.Domain;
using Skillforge.Data;
using Skillforge.Repository;
using System.Net.Mail;
using Skillforge.Utility;
using Microsoft.EntityFrameworkCore;

namespace Skillforge.Service;

public class ResultService : IResultService
{
    private readonly SkillForgeDB          _context;
    private readonly IResultRepository     _resultRepository;
    private readonly IConfiguration        _configuration;
    private readonly INotificationService  _notificationService;

    public ResultService(
        SkillForgeDB context,
        IResultRepository resultRepository,
        IConfiguration configuration,
        INotificationService notificationService)
    {
        _context              = context;
        _resultRepository     = resultRepository;
        _configuration        = configuration;
        _notificationService  = notificationService;
    }

    public async Task SubmitResultAsync(SubmitAssessmentResultDto request, int reviewerId)
    {
        
        bool exists = await _context.Results.AnyAsync(r => r.AssessmentID == request.AssessmentID 
                && r.EmployeeID == request.EmployeeID);

        if (exists)
            throw new Exception(ResultMessages.Duplicate);

        // Read Assessment with Course navigation for notification message
        var assessment = await _context.Assessments
            .Include(a => a.Course)
            .FirstOrDefaultAsync(a => a.AssessmentID == request.AssessmentID);

        if (assessment == null)
            throw new KeyNotFoundException(ResultMessages.NotFound);

         // Validate score <= max
        if (request.Score > assessment.MaxScore)
            throw new Exception(ResultMessages.exceeds);
        // Validate score <0
        if (request.Score <0)
            throw new Exception(ResultMessages.negative);
        // Compute pass / fail using the trainer-defined passing score
        var status = request.Score >= assessment.PassingScore ? ResultStatus.Pass : ResultStatus.Fail;

        // Create Result entity
        var result = new Result
        {
            AssessmentID = request.AssessmentID,
            EmployeeID = request.EmployeeID,
            Score = request.Score,
            Status = status
        };

        // Save Result (via repository)
        await _resultRepository.SubmitAssessmentResult(result);

        var AuditLog = new AuditLog
        {
            UserID = reviewerId,
            Action = "Submit Assessment Result",
            Resource = "Result",
            Timestamp = DateTime.Now
        };
        await _resultRepository.AddAuditLog(AuditLog);

        // Notify the employee of their result
        var courseTitle = assessment.Course?.Title ?? $"Course #{assessment.CourseID}";
        if (status == ResultStatus.Pass)
            await _notificationService.NotifyAssessmentPassedAsync(request.EmployeeID, assessment.CourseID, courseTitle, request.Score);
        else
            await _notificationService.NotifyAssessmentFailedAsync(request.EmployeeID, assessment.CourseID, courseTitle, request.Score);
    }

    public async Task SelfSubmitAsync(int assessmentId, decimal score, int employeeId)
    {
		var existingResult = await _context.Results
			.FirstOrDefaultAsync(r => r.AssessmentID == assessmentId && r.EmployeeID == employeeId);
		if (existingResult != null)
		{
			if (existingResult.Status == ResultStatus.Pending)
				throw new Exception("Your previous attempt is still pending review.");
			// Log previous score before overwriting
			// Log previous score and status before overwriting
			_context.AuditLogs.Add(new AuditLog
			{
				UserID = employeeId,
				Action = "AssessmentPreviousScore",
				Resource = $"Assessment/{assessmentId}/Score/{existingResult.Score}/Status/{existingResult.Status}",
				Timestamp = DateTime.Now
			});
			// Reattempt — store current attempt score for trainer to evaluate
			existingResult.Score = score;
			existingResult.Status = ResultStatus.Pending;
			await _context.SaveChangesAsync();
			return;
		}

		var assessment = await _context.Assessments.FindAsync(assessmentId);
        if (assessment == null)
            throw new KeyNotFoundException("Assessment not found.");

        bool isEnrolled = await _context.Enrollments
            .AnyAsync(e => e.CourseID == assessment.CourseID && e.EmployeeID == employeeId);
        if (!isEnrolled)
            throw new UnauthorizedAccessException("You are not enrolled in this course.");

        if (score > assessment.MaxScore)
            throw new Exception($"Score cannot exceed the maximum of {assessment.MaxScore}.");
        if (score < 0)
            throw new Exception("Score cannot be negative.");

        // Save as Pending — trainer must evaluate before Pass/Fail is determined
        var result = new Result
        {
            AssessmentID = assessmentId,
            EmployeeID   = employeeId,
            Score        = score,
            Status       = ResultStatus.Pending
        };

        await _resultRepository.SubmitAssessmentResult(result);
    }

    public async Task<List<PendingResultDto>> GetPendingResultsAsync(int trainerId)
    {
        var results = await _resultRepository.GetPendingResultsForTrainerAsync(trainerId);
        return results.Select(r => new PendingResultDto
        {
            AssessmentId   = r.AssessmentID,
            EmployeeId     = r.EmployeeID,
            CourseName     = r.Assessment?.Course?.Title ?? string.Empty,
            AssessmentType = r.Assessment?.Type.ToString() ?? string.Empty,
            EmployeeName   = r.UserRoleEmployee?.Name ?? string.Empty,
            Score          = r.Score,
            MaxScore       = r.Assessment?.MaxScore ?? 0,
            PassingScore   = r.Assessment?.PassingScore ?? 0
        }).ToList();
    }

    public async Task EvaluateResultAsync(int assessmentId, int employeeId, int trainerId, bool pass)
    {
        var result = await _resultRepository.GetResultByCompositeKeyAsync(assessmentId, employeeId);
        if (result == null)
            throw new KeyNotFoundException("Result not found.");

        if (result.Status != ResultStatus.Pending)
            throw new Exception("This result has already been evaluated.");

        var courseTrainerId = result.Assessment?.Course?.TrainerID;
        if (courseTrainerId != trainerId)
            throw new UnauthorizedAccessException("You are not the trainer for this course.");

        result.Status = pass ? ResultStatus.Pass : ResultStatus.Fail;

        await _resultRepository.UpdateResultAsync(result);

        await _resultRepository.AddAuditLog(new AuditLog
        {
            UserID    = trainerId,
            Action    = $"EvaluateResult:{result.Status}",
            Resource  = $"Result/{assessmentId}/{employeeId}",
            Timestamp = DateTime.Now
        });

        // Notify employee of evaluation outcome
        var courseTitle = result.Assessment?.Course?.Title ?? "your course";
        var courseId    = result.Assessment?.CourseID ?? 0;
        if (pass)
            await _notificationService.NotifyAssessmentPassedAsync(employeeId, courseId, courseTitle, result.Score);
        else
            await _notificationService.NotifyAssessmentFailedAsync(employeeId, courseId, courseTitle, result.Score);
    }
}
