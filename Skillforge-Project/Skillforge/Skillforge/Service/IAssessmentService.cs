using Skillforge.Dto;

namespace Skillforge.Service;

/// <summary>
/// Defines the business logic contract for assessment management.
/// Handles validation rules and orchestrates assessment creation workflows.
/// </summary>
public interface IAssessmentService
{
    /// <summary>
    /// Creates a new assessment after validating that the target course exists and is live.
    /// </summary>
    /// <param name="dto">The assessment creation request containing CourseId, Type, and MaxScore.</param>
    /// <returns>
    /// A tuple with Success indicating the outcome, ErrorMessage describing any failure,
    /// and AssessmentId containing the generated ID on success.
    /// </returns>
    Task<(bool Success, string ErrorMessage, int AssessmentId)> CreateAssessmentAsync(CreateAssessmentRequestDto dto);
    Task<List<AssessmentResponseDto>> GetAllAssessmentsAsync();
    Task<List<EmployeeAssessmentDto>> GetAssessmentsForEmployeeAsync(int employeeId);
    Task<List<EmployeeAssessmentDto>> GetAssessmentsByModuleAsync(int moduleId, int employeeId);
}
