using System;
using Skillforge.Dto;

namespace Skillforge.Service;

public interface IEnrollmentService
{
    Task<long> EnrollAsync(int courseId, int employeeId);
    Task<BulkEnrollmentResponseDto> BulkEnrollAsync(BulkEnrollmentRequestDto request, int managerId);
    Task<List<EnrollmentResponseDto>> GetAllEnrollmentsAsync();
    Task<List<EnrollmentResponseDto>> GetEnrollmentsByEmployeeAsync(int employeeId);
    Task<(bool Success, string Message)> UpdateEnrollmentStatusAsync(int enrollmentId, bool status);
	Task<List<EnrollmentResponseDto>> GetEnrollmentsByTrainerAsync(int trainerId);
}
