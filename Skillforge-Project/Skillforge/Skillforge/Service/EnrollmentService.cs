using System;

namespace Skillforge.Service;

using Microsoft.EntityFrameworkCore;
using Skillforge.Domain;
using Skillforge.Dto;
using Skillforge.Repository;
using Skillforge.Utility;

public class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository enrollmentRepository;
    private readonly IAuditService         auditService;
    private readonly INotificationService  _notificationService;

    public EnrollmentService(
        IEnrollmentRepository _enrollmentRepository,
        IAuditService _auditService,
        INotificationService notificationService)
    {
        enrollmentRepository  = _enrollmentRepository;
        auditService          = _auditService;
        _notificationService  = notificationService;
    }

    public async Task<long> EnrollAsync(int courseId, int employeeId)
    {
        // 1. Get course
        Course course = await enrollmentRepository.GetByIdAsync(courseId);

        if (course == null)
        {
            throw new KeyNotFoundException(EnrollmentMessages.notfound);
        }

        // 2. Course closed → 400
        if (course.Status == false)
        {
            var auditLog = new AuditLog
            {
            UserID = employeeId,
            Action = $"FAILED- {courseId} COURSE CLOSED",
            Resource = "Enrollment",
            Timestamp = DateTime.UtcNow
            };
            await enrollmentRepository.AddAuditLog(auditLog);
            throw new BadHttpRequestException(EnrollmentMessages.closed);
        }

        // 3. Duplicate enroll → 409
        var exists = await enrollmentRepository.ExistsAsync(courseId, employeeId);

        if (exists)
        {
            var auditLog = new AuditLog
            {
            UserID = employeeId,
            Action = $"FAILED- {courseId} DUPLICATE",
            Resource = "Enrollment",
            Timestamp = DateTime.UtcNow
            };
            await enrollmentRepository.AddAuditLog(auditLog);
            throw new InvalidOperationException(EnrollmentMessages.enrolled);
        }

        // 4. Create enrollment
        var enrollment = new Enrollment
        {
            CourseID = courseId,
            EmployeeID = employeeId,
            EnrollmentDate = DateTime.UtcNow
        };
        await enrollmentRepository.AddAsync(enrollment);

        // 5. Audit success
        var AuditLog = new AuditLog
        {
            UserID = employeeId,
            Action = $"SUCCESS - {courseId} Enrolled",
            Resource = "Enrollment",
            Timestamp = DateTime.UtcNow
        };
        await enrollmentRepository.AddAuditLog(AuditLog);

        // Notify employee and trainer
        await _notificationService.NotifyEnrollmentConfirmedAsync(employeeId, courseId, course.Title);
        if (course.TrainerID > 0)
        {
            var employee = await enrollmentRepository.GetEmployeeByIdAsync(employeeId);
            await _notificationService.NotifyTrainerNewEnrollmentAsync(
                course.TrainerID, courseId, course.Title, employee?.Name ?? $"Employee #{employeeId}");
        }

        return enrollment.EnrollmentID;
    }

    public async Task<List<EnrollmentResponseDto>> GetAllEnrollmentsAsync()
    {
        var enrollments = await enrollmentRepository.GetAllEnrollmentsAsync();
        return MapToDto(enrollments);
    }

    public async Task<List<EnrollmentResponseDto>> GetEnrollmentsByEmployeeAsync(int employeeId)
    {
        var enrollments = await enrollmentRepository.GetEnrollmentsByEmployeeAsync(employeeId);
        return MapToDto(enrollments);
    }

    public async Task<(bool Success, string Message)> UpdateEnrollmentStatusAsync(int enrollmentId, bool status)
    {
        var enrollment = await enrollmentRepository.GetByEnrollmentIdAsync(enrollmentId);
        if (enrollment == null)
            return (false, "Enrollment not found.");

        await enrollmentRepository.UpdateEnrollmentStatusAsync(enrollmentId, status);

        var statusText = status ? "Enrolled" : "Waitlisted";
        return (true, $"Enrollment status updated to {statusText}.");
    }

    private static List<EnrollmentResponseDto> MapToDto(List<Domain.Enrollment> enrollments)
    {
        return enrollments.Select(e =>
        {
            var lastAttendance = e.Attendances
                .OrderByDescending(a => a.AttendanceDate)
                .FirstOrDefault();

            return new EnrollmentResponseDto
            {
                EnrollmentId = e.EnrollmentID,
                EmployeeId = e.EmployeeID,
                EmployeeName = e.EmployeeIdNavigation?.Name ?? string.Empty,
                CourseId = e.CourseID,
                CourseName = e.CourseIdNavigation?.Title ?? string.Empty,
                EnrollmentDate = e.EnrollmentDate,
                Status = e.CompletedDate.HasValue ? "Completed" : (e.Status ? "Enrolled" : "Waitlist"),
                LastAttendance = lastAttendance != null ? lastAttendance.Status.ToString() : "—"
            };
        }).ToList();
    }

    // Bulk enrollment: Manager assigns a course to multiple employees at once
    // Supports partial success - valid employees are enrolled, invalid ones are skipped with reason
    // Audits the entire bulk operation as a single ManagerAssign action
    public async Task<BulkEnrollmentResponseDto> BulkEnrollAsync(BulkEnrollmentRequestDto request, int managerId)
    {
        var response = new BulkEnrollmentResponseDto
        {
            TotalRequested = request.EmployeeIds.Count
        };

        // 1. Validate course exists
        Course course = await enrollmentRepository.GetByIdAsync(request.CourseId);

        if (course == null)
        {
            throw new KeyNotFoundException(EnrollmentMessages.notfound);
        }

        // 2. Validate course is open for enrollment
        if (course.Status == false)
        {
            throw new BadHttpRequestException(EnrollmentMessages.closed);
        }

        // 3. Process each employee - partial success: skip failures, continue with valid ones
        foreach (var employeeId in request.EmployeeIds)
        {
            var resultItem = new EnrollmentResultItem { EmployeeId = employeeId };

            // Check if employee exists and is active
            var employeeExists = await enrollmentRepository.EmployeeExistsAsync(employeeId);
            if (!employeeExists)
            {
                resultItem.Status = "Failed";
                resultItem.Reason = EnrollmentMessages.EmployeeNotFound;
                response.Failed++;
                response.Results.Add(resultItem);
                continue;
            }

            // Check for duplicate enrollment
            var alreadyEnrolled = await enrollmentRepository.ExistsAsync(request.CourseId, employeeId);
            if (alreadyEnrolled)
            {
                resultItem.Status = "Failed";
                resultItem.Reason = EnrollmentMessages.enrolled;
                response.Failed++;
                response.Results.Add(resultItem);
                continue;
            }

            // Create enrollment for valid employee
            var enrollment = new Enrollment
            {
                CourseID = request.CourseId,
                EmployeeID = employeeId,
                EnrollmentDate = DateTime.UtcNow
            };
            await enrollmentRepository.AddAsync(enrollment);

            resultItem.EnrollmentId = enrollment.EnrollmentID;
            resultItem.Status = "Success";
            response.Succeeded++;
            response.Results.Add(resultItem);

            // Notify the enrolled employee
            await _notificationService.NotifyEnrollmentConfirmedAsync(employeeId, request.CourseId, course.Title);
        }

        // 4. Audit: Log the ManagerAssign action with bulk operation summary
        await auditService.LogAsync(
            managerId,
            $"ManagerAssign - BulkEnroll CourseId:{request.CourseId} Total:{response.TotalRequested} Succeeded:{response.Succeeded} Failed:{response.Failed}",
            "Enrollment"
        );

        return response;
    }
	public async Task<List<EnrollmentResponseDto>> GetEnrollmentsByTrainerAsync(int trainerId)
	{
		var enrollments = await enrollmentRepository.GetEnrollmentsByTrainerAsync(trainerId);
		return MapToDto(enrollments);
	}
}
