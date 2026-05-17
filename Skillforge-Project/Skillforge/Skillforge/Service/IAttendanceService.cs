using Skillforge.Dto;

namespace Skillforge.Service;

public interface IAttendanceService
{
    Task<AttendanceResponseDto> MarkAttendanceAsync(MarkAttendanceDto dto, int trainerID);
    Task<GetCourseAttendanceResponseDto> GetCourseAttendanceAsync(int courseID, DateTime date, int trainerID);

    // Attendance history
    Task<EnrollmentAttendanceHistoryDto> GetAttendanceHistoryAsync(int enrollmentId, int userId, string userRole);

    // Attendance request workflow
    Task<AttendanceRequestResponseDto> CreateAttendanceRequestAsync(CreateAttendanceRequestDto dto, int employeeId);
    Task<List<AttendanceRequestResponseDto>> GetMyAttendanceRequestsAsync(int employeeId);
    Task<List<AttendanceRequestResponseDto>> GetPendingRequestsForCourseAsync(int courseId, int trainerId);
    Task<AttendanceRequestResponseDto> ReviewAttendanceRequestAsync(int requestId, ReviewAttendanceRequestDto dto, int trainerId);
}