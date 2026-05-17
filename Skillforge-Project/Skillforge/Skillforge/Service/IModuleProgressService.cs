namespace Skillforge.Service;

public interface IModuleProgressService
{
    /// <summary>
    /// Marks a module as complete for an employee's enrollment.
    /// When the final module is completed, triggers course completion:
    /// updates enrollment status, auto-issues certification, sends notification.
    /// </summary>
    Task<(bool Success, string Message, bool CourseJustCompleted)> MarkModuleCompleteAsync(
        int enrollmentId, int moduleId, int employeeId);

    /// <summary>Returns the list of module IDs completed by the employee for a given enrollment.</summary>
    Task<List<int>> GetProgressAsync(int enrollmentId, int employeeId);
}
