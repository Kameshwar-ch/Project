using Skillforge.Domain;

namespace Skillforge.Repository;

public interface IAttendanceRepository
{
    Task<(Attendance attendance, bool isNew)> UpsertAttendanceAsync(Attendance attendance);
    Task<List<Attendance>> GetByEnrollmentAsync(int enrollmentId);
}
 