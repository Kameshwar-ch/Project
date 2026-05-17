using Skillforge.Domain;

namespace Skillforge.Repository;

public interface IAttendanceRequestRepository
{
    Task<AttendanceRequest> CreateAsync(AttendanceRequest request);
    Task<List<AttendanceRequest>> GetByEmployeeAsync(int employeeId);
    Task<List<AttendanceRequest>> GetPendingByCourseAsync(int courseId);
    Task<AttendanceRequest?> GetByIdAsync(int requestId);
    Task SaveAsync();
}
