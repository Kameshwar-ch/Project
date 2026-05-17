using Skillforge.Repository;

namespace Skillforge.Service;

public class ModuleProgressService : IModuleProgressService
{
    private readonly IModuleProgressRepository _progressRepo;
    private readonly ICertificationService _certificationService;
    private readonly INotificationService _notificationService;

    public ModuleProgressService(
        IModuleProgressRepository progressRepo,
        ICertificationService certificationService,
        INotificationService notificationService)
    {
        _progressRepo         = progressRepo;
        _certificationService = certificationService;
        _notificationService  = notificationService;
    }

    public async Task<(bool Success, string Message, bool CourseJustCompleted)> MarkModuleCompleteAsync(
        int enrollmentId, int moduleId, int employeeId)
    {
        // 1. Verify enrollment belongs to employee
        var enrollment = await _progressRepo.GetEnrollmentAsync(enrollmentId, employeeId);
        if (enrollment == null)
            return (false, "Enrollment not found.", false);

        // 2. Already fully completed — no-op
        if (enrollment.CompletedDate.HasValue)
            return (true, "Course already completed.", false);

        // 3. Verify module belongs to the enrollment's course
        var module = await _progressRepo.GetModuleAsync(moduleId, enrollment.CourseID);
        if (module == null)
            return (false, "Module not found in this course.", false);

        // 4. Ensure all assessments for this module are passed (skip if already marked complete)
        var alreadyDone = await _progressRepo.IsCompletedAsync(enrollmentId, moduleId);
        if (!alreadyDone)
        {
            var (allPassed, errorMessage) = await _progressRepo.CheckModuleAssessmentsAsync(moduleId, employeeId);
            if (!allPassed)
                return (false, errorMessage!, false);
        }

        // 5. Mark module complete (idempotent)
        await _progressRepo.MarkCompleteAsync(enrollmentId, moduleId);

        // 6. Check if all modules are now done
        var totalModules = await _progressRepo.GetCourseModuleCountAsync(enrollment.CourseID);
        if (totalModules == 0)
            return (true, "Module marked complete.", false);

        var completedCount = await _progressRepo.GetCompletedModuleCountAsync(enrollmentId, enrollment.CourseID);
        if (completedCount < totalModules)
            return (true, "Module marked complete.", false);

        // 7. All modules done — mark course as completed
        await _progressRepo.SetEnrollmentCompletedAsync(enrollmentId);

        var courseTitle = enrollment.CourseIdNavigation?.Title ?? "the course";

        // 8. Auto-issue certification (idempotent)
        await _certificationService.AutoIssueCertificationAsync(employeeId, enrollment.CourseID, courseTitle);

        // 9. Notify employee
        await _notificationService.NotifyCourseCompletedAsync(employeeId, enrollment.CourseID, courseTitle);

        return (true, "Course completed! Your certificate is now available.", true);
    }

    public async Task<List<int>> GetProgressAsync(int enrollmentId, int employeeId)
    {
        var enrollment = await _progressRepo.GetEnrollmentAsync(enrollmentId, employeeId);
        if (enrollment == null) return new();
        return await _progressRepo.GetCompletedModuleIdsAsync(enrollmentId);
    }
}
