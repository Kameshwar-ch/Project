using Skillforge.Domain;
using Skillforge.Dto;
using Skillforge.Repository;

namespace Skillforge.Service;

/// <summary>
/// Implements the business logic for assessment management.
/// Validates course existence and live status before persisting a new assessment via the repository.
/// </summary>
public class AssessmentService : IAssessmentService
{
    private readonly IAssessmentRepository _assessmentRepository;

    public AssessmentService(IAssessmentRepository assessmentRepository)
    {
        _assessmentRepository = assessmentRepository;
    }

    /// <summary>
    /// Validates the target course and creates a new assessment if all rules pass.
    /// Returns a failure result if the course does not exist or is not currently live.
    /// </summary>
    /// <param name="dto">The assessment creation request containing CourseId, Type, and MaxScore.</param>
    /// <returns>
    /// A tuple with Success set to true and the new AssessmentId on success,
    /// or Success set to false with an ErrorMessage describing the validation failure.
    /// </returns>
    public async Task<(bool Success, string ErrorMessage, int AssessmentId)> CreateAssessmentAsync(CreateAssessmentRequestDto dto)
    {
        var course = await _assessmentRepository.GetCourseByIdAsync(dto.CourseId);

        if (course == null)
            return (false, "Course not found.", 0);

        if (course.Status)
            return (false, "Course is not live.", 0);

        if (dto.PassingScore > dto.MaxScore)
            return (false, "Passing score cannot exceed max score.", 0);

        // If a ModuleId is provided, validate it belongs to the course
        if (dto.ModuleId.HasValue)
        {
            var module = await _assessmentRepository.GetModuleByIdAsync(dto.ModuleId.Value);
            if (module == null || module.CourseID != dto.CourseId)
                return (false, "Module not found in this course.", 0);
        }

        var assessment = new Assessment
        {
            CourseID     = dto.CourseId,
            ModuleID     = dto.ModuleId,
            Type         = dto.Type,
            MaxScore     = dto.MaxScore,
            PassingScore = dto.PassingScore,
            Date         = DateTime.Now
        };

        int assessmentId = await _assessmentRepository.CreateAssessmentAsync(assessment);
        return (true, null!, assessmentId);
    }

    public async Task<List<AssessmentResponseDto>> GetAllAssessmentsAsync()
    {
        var assessments = await _assessmentRepository.GetAllAssessmentsAsync();
        return assessments.Select(a => new AssessmentResponseDto
        {
            AssessmentId = a.AssessmentID,
            CourseId     = a.CourseID,
            CourseName   = a.Course?.Title ?? string.Empty,
            ModuleId     = a.ModuleID,
            ModuleName   = a.Module?.Title,
            Type         = a.Type.ToString(),
            MaxScore     = a.MaxScore,
            PassingScore = a.PassingScore,
            Date         = a.Date,
			CourseStatus = a.Course?.Status ?? false,
			TrainerID = a.Course?.TrainerID ?? 0
		}).ToList();
    }

	public async Task<List<EmployeeAssessmentDto>> GetAssessmentsForEmployeeAsync(int employeeId)
	{
		var assessments = await _assessmentRepository.GetAssessmentsForEnrolledCoursesAsync(employeeId);
		var results = await _assessmentRepository.GetResultsByEmployeeAsync(employeeId);
		var resultDict = results.ToDictionary(r => r.AssessmentID);

		// Fetch previous scores from audit logs for best-score calculation
		// Fetch previous scores from audit logs for best-score calculation
		var previousScoreLogs = await _assessmentRepository.GetPreviousScoreLogsAsync(employeeId);
		var bestPrevious = new Dictionary<int, (decimal Score, string Status)>();
		foreach (var log in previousScoreLogs)
		{
			// Resource format: "Assessment/{id}/Score/{score}/Status/{status}"
			var parts = log.Resource.Split('/');
			if (parts.Length >= 4 && int.TryParse(parts[1], out var aId) && decimal.TryParse(parts[3], out var prevScore))
			{
				var prevStatus = parts.Length >= 6 ? parts[5] : "Fail";
				if (!bestPrevious.ContainsKey(aId) || prevScore > bestPrevious[aId].Score)
					bestPrevious[aId] = (prevScore, prevStatus);
			}
		}

		return assessments.Select(a =>
		{
			resultDict.TryGetValue(a.AssessmentID, out var result);
			decimal? displayScore = result?.Score;
			string? displayStatus = result?.Status.ToString();

			if (bestPrevious.TryGetValue(a.AssessmentID, out var prev))
			{
				// Use best score between current and previous
				if (displayScore.HasValue)
				{
					if (prev.Score > displayScore.Value)
					{
						displayScore = prev.Score;
						displayStatus = prev.Status;
					}
				}
				else
				{
					displayScore = prev.Score;
					displayStatus = prev.Status;
				}
			}

			return new EmployeeAssessmentDto
			{
				AssessmentId = a.AssessmentID,
				CourseId = a.CourseID,
				CourseName = a.Course?.Title ?? string.Empty,
				ModuleId = a.ModuleID,
				ModuleName = a.Module?.Title,
				Type = a.Type.ToString(),
				MaxScore = a.MaxScore,
				Date = a.Date,
				IsDone = result != null && result.Status == ResultStatus.Pending,
				ResultId = result?.ResultID,
				Score = displayScore,
				ResultStatus = displayStatus
			};
		}).ToList();
	
	}

	public async Task<List<EmployeeAssessmentDto>> GetAssessmentsByModuleAsync(int moduleId, int employeeId)
	{
		var assessments = await _assessmentRepository.GetAssessmentsForModuleAsync(moduleId);
		var results = await _assessmentRepository.GetResultsByEmployeeAsync(employeeId);
		var resultDict = results.ToDictionary(r => r.AssessmentID);

		// Fetch previous scores from audit logs for best-score calculation
		var previousScoreLogs = await _assessmentRepository.GetPreviousScoreLogsAsync(employeeId);
		var bestPrevious = new Dictionary<int, (decimal Score, string Status)>();
		foreach (var log in previousScoreLogs)
		{
			var parts = log.Resource.Split('/');
			if (parts.Length >= 4 && int.TryParse(parts[1], out var aId) && decimal.TryParse(parts[3], out var prevScore))
			{
				var prevStatus = parts.Length >= 6 ? parts[5] : "Fail";
				if (!bestPrevious.ContainsKey(aId) || prevScore > bestPrevious[aId].Score)
					bestPrevious[aId] = (prevScore, prevStatus);
			}
		}

		return assessments.Select(a =>
		{
			resultDict.TryGetValue(a.AssessmentID, out var result);
			decimal? displayScore = result?.Score;
			string? displayStatus = result?.Status.ToString();

			if (bestPrevious.TryGetValue(a.AssessmentID, out var prev))
			{
				if (displayScore.HasValue)
				{
					if (prev.Score > displayScore.Value)
					{
						displayScore = prev.Score;
						displayStatus = prev.Status;
					}
				}
				else
				{
					displayScore = prev.Score;
					displayStatus = prev.Status;
				}
			}

			return new EmployeeAssessmentDto
			{
				AssessmentId = a.AssessmentID,
				CourseId = a.CourseID,
				CourseName = a.Course?.Title ?? string.Empty,
				ModuleId = a.ModuleID,
				ModuleName = a.Module?.Title,
				Type = a.Type.ToString(),
				MaxScore = a.MaxScore,
				Date = a.Date,
				IsDone = result != null && result.Status == ResultStatus.Pending,
				ResultId = result?.ResultID,
				Score = displayScore,
				ResultStatus = displayStatus
			};
		}).ToList();
	}
}
