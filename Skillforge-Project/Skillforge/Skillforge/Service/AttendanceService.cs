using Microsoft.EntityFrameworkCore;
using Skillforge.Data;
using Skillforge.Domain;
using Skillforge.Dto;
using Skillforge.Repository;

namespace Skillforge.Service;

public class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository        _attendanceRepository;
    private readonly IAttendanceRequestRepository _requestRepository;
    private readonly SkillForgeDB                 _context;
    private readonly INotificationService         _notificationService;

    private const string CourseAccessedAction   = "CourseAccessed";
    private const string AttendanceMarkedAction = "AttendanceMarked";

    public AttendanceService(
        IAttendanceRepository attendanceRepository,
        IAttendanceRequestRepository requestRepository,
        SkillForgeDB context,
        INotificationService notificationService)
    {
        _attendanceRepository = attendanceRepository;
        _requestRepository    = requestRepository;
        _context              = context;
        _notificationService  = notificationService;
    }

    /// <summary>
    /// Marks attendance for single or bulk enrollments in a course.
    /// Single → send one record with EnrollmentID and Status.
    /// Bulk   → send multiple records, system checks AuditLog per employee.
    /// Upserts each record and logs trainer action in AuditLog.
    /// </summary>
    public async Task<AttendanceResponseDto> MarkAttendanceAsync(MarkAttendanceDto dto, int trainerID)
    {
        if (dto.CourseID <= 0)
            throw new InvalidOperationException("Invalid CourseID.");

        if (dto.AttendanceDate == default)
            throw new InvalidOperationException("AttendanceDate is required.");

        if (dto.AttendanceDate.Date > DateTime.Now.Date)
            throw new InvalidOperationException($"Invalid date. {dto.AttendanceDate:yyyy-MM-dd} is a future date.");

        if (dto.Records == null || !dto.Records.Any())
            throw new InvalidOperationException("At least one attendance record is required.");

        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.CourseID == dto.CourseID);

        if (course == null)
            throw new KeyNotFoundException($"Course {dto.CourseID} not found.");

        if (course.TrainerID != trainerID)
            throw new UnauthorizedAccessException("You are not authorized to mark attendance for this course.");

        // Batch fetch AuditLog CourseAccessed for all enrollments on this date
        var enrollmentIDs    = dto.Records.Select(r => r.EnrollmentID).ToList();
        var enrollmentList   = await _context.Enrollments
            .Include(e => e.EmployeeIdNavigation)
            .Where(e => enrollmentIDs.Contains(e.EnrollmentID))
            .ToListAsync();

        var employeeIDList   = enrollmentList.Select(e => e.EmployeeID).ToList();

        var rawLogs = await _context.AuditLogs
            .Where(a =>
                a.UserID    != null                           &&
                employeeIDList.Contains(a.UserID.Value)       &&
                a.Action    == CourseAccessedAction            &&
                a.Resource  == $"Course/{dto.CourseID}"       &&
                a.Timestamp >= dto.AttendanceDate.Date         &&
                a.Timestamp <  dto.AttendanceDate.Date.AddDays(1))
            .Select(a => new { UserID = a.UserID!.Value, a.Timestamp })
            .ToListAsync();

        var firstAccessPerEmployee = rawLogs
            .GroupBy(a => a.UserID)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(a => a.Timestamp).First().Timestamp
            );

        var records = new List<AttendanceRecordResultDto>();

        foreach (var record in dto.Records)
        {
            if (record.EnrollmentID <= 0)
                throw new InvalidOperationException("Invalid EnrollmentID.");

            var enrollment = enrollmentList
                .FirstOrDefault(e => e.EnrollmentID == record.EnrollmentID);

            if (enrollment == null)
                throw new KeyNotFoundException($"Enrollment {record.EnrollmentID} not found.");

            if (enrollment.CourseID != dto.CourseID)
                throw new InvalidOperationException($"Enrollment {record.EnrollmentID} does not belong to Course {dto.CourseID}.");

            if (!enrollment.Status)
                throw new InvalidOperationException($"Cannot mark attendance. Enrollment {record.EnrollmentID} is not in progress.");

            var isAccessed = firstAccessPerEmployee.ContainsKey(enrollment.EmployeeID);

            var attendance = new Attendance
            {
                EnrollmentID   = record.EnrollmentID,
                AttendanceDate = isAccessed
                                 ? firstAccessPerEmployee[enrollment.EmployeeID]
                                 : dto.AttendanceDate.Date,
                Status         = record.Status
            };

            await _attendanceRepository.UpsertAttendanceAsync(attendance);

            records.Add(new AttendanceRecordResultDto
            {
                EnrollmentID = enrollment.EnrollmentID,
                EmployeeName = enrollment.EmployeeIdNavigation.Name,
                Status       = record.Status.ToString()
            });
        }

        _context.AuditLogs.Add(new AuditLog
        {
            UserID    = trainerID,
            Action    = AttendanceMarkedAction,
            Resource  = $"Course/{dto.CourseID}",
            Timestamp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var presentCount = records.Count(r => r.Status == "Present");
        var absentCount  = records.Count(r => r.Status == "Absent");

        return new AttendanceResponseDto
        {
            CourseID       = dto.CourseID,
            AttendanceDate = dto.AttendanceDate,
            TotalMarked    = records.Count,
            PresentCount   = presentCount,
            AbsentCount    = absentCount,
            Records        = records,
            Message        = "Attendance marked successfully."
        };
    }

    /// <summary>
    /// Returns all active enrollments with CourseStatus and LoginDate for a course on a given date.
    /// </summary>
    public async Task<GetCourseAttendanceResponseDto> GetCourseAttendanceAsync(int courseID, DateTime date, int trainerID)
    {
        if (courseID <= 0)
            throw new InvalidOperationException("Invalid CourseID.");

        if (date == default)
            throw new InvalidOperationException("Date is required.");

        if (date.Date > DateTime.Now.Date)
            throw new InvalidOperationException($"Invalid date. {date:yyyy-MM-dd} is a future date.");

        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.CourseID == courseID);

        if (course == null)
            throw new KeyNotFoundException($"Course {courseID} not found.");

        if (course.TrainerID != trainerID)
            throw new UnauthorizedAccessException("You are not authorized to access this course.");

		var enrollments = await _context.Enrollments
			  .Include(e => e.EmployeeIdNavigation)
			  .Where(e => e.CourseID == courseID && e.Status == true && !e.CompletedDate.HasValue)
			  .ToListAsync();

		if (!enrollments.Any())
            throw new KeyNotFoundException("No active enrollments found for this course.");

        var enrolledEmployeeIDList = enrollments
            .Select(e => e.EmployeeID)
            .ToList();

        var rawLogs = await _context.AuditLogs
            .Where(a =>
                a.UserID    != null                              &&
                enrolledEmployeeIDList.Contains(a.UserID.Value)  &&
                a.Action    == CourseAccessedAction               &&
                a.Resource  == $"Course/{courseID}"              &&
                a.Timestamp >= date.Date                         &&
                a.Timestamp <  date.Date.AddDays(1))
            .Select(a => new { UserID = a.UserID!.Value, a.Timestamp })
            .ToListAsync();

        var firstAccessPerEmployee = rawLogs
            .GroupBy(a => a.UserID)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(a => a.Timestamp).First().Timestamp
            );

        var records = enrollments.Select(e => new CourseAttendanceDto
        {
            EnrollmentID = e.EnrollmentID,
            EmployeeName = e.EmployeeIdNavigation.Name,
            CourseStatus = firstAccessPerEmployee.ContainsKey(e.EmployeeID)
                           ? "Accessed" : "Not Accessed",
            LoginDate    = firstAccessPerEmployee.TryGetValue(e.EmployeeID, out var t)
                           ? t
                           : date.Date
        }).ToList();

        return new GetCourseAttendanceResponseDto
        {
            CourseID       = courseID,
            AttendanceDate = date,
            Records        = records
        };
    }

    // ── Attendance History ───────────────────────────────────────────────────

    public async Task<EnrollmentAttendanceHistoryDto> GetAttendanceHistoryAsync(
        int enrollmentId, int userId, string userRole)
    {
        var enrollment = await _context.Enrollments
            .Include(e => e.CourseIdNavigation)
            .Include(e => e.EmployeeIdNavigation)
            .FirstOrDefaultAsync(e => e.EnrollmentID == enrollmentId);

        if (enrollment == null)
            throw new KeyNotFoundException($"Enrollment {enrollmentId} not found.");

        // Authorization: Employee can only see their own; Trainer must own the course
        if (userRole == "Employee" && enrollment.EmployeeID != userId)
            throw new UnauthorizedAccessException("You can only view attendance for your own enrollment.");

        if (userRole == "Trainer")
        {
            if (enrollment.CourseIdNavigation.TrainerID != userId)
                throw new UnauthorizedAccessException("You are not authorized to view this enrollment's attendance.");
        }

        var records = await _attendanceRepository.GetByEnrollmentAsync(enrollmentId);

        return new EnrollmentAttendanceHistoryDto
        {
            EnrollmentID = enrollmentId,
            EmployeeName = enrollment.EmployeeIdNavigation.Name,
            CourseName   = enrollment.CourseIdNavigation.Title,
            PresentCount = records.Count(r => r.Status == AttendanceStatus.Present),
            AbsentCount  = records.Count(r => r.Status == AttendanceStatus.Absent),
            Records      = records.Select(r => new AttendanceHistoryItemDto
            {
                AttendanceID   = r.AttendanceID,
                AttendanceDate = r.AttendanceDate,
                Status         = r.Status.ToString()
            }).ToList()
        };
    }

    // ── Attendance Request Workflow ──────────────────────────────────────────

    public async Task<AttendanceRequestResponseDto> CreateAttendanceRequestAsync(
        CreateAttendanceRequestDto dto, int employeeId)
    {
        if (dto.EnrollmentID <= 0)
            throw new InvalidOperationException("Invalid EnrollmentID.");

        if (dto.RequestDate == default)
            throw new InvalidOperationException("RequestDate is required.");

        if (dto.RequestDate.Date > DateTime.UtcNow.Date)
            throw new InvalidOperationException("Request date cannot be in the future.");

        if (dto.RequestDate.Date < DateTime.UtcNow.Date.AddDays(-7))
            throw new InvalidOperationException("Attendance can only be requested for dates within the last 7 days.");

        var enrollment = await _context.Enrollments
            .Include(e => e.CourseIdNavigation)
            .Include(e => e.EmployeeIdNavigation)
            .FirstOrDefaultAsync(e => e.EnrollmentID == dto.EnrollmentID);

        if (enrollment == null)
            throw new KeyNotFoundException($"Enrollment {dto.EnrollmentID} not found.");

        if (enrollment.EmployeeID != employeeId)
            throw new UnauthorizedAccessException("You can only request attendance for your own enrollment.");

        if (!enrollment.Status)
            throw new InvalidOperationException("Cannot request attendance for a waitlisted enrollment.");

        if (dto.RequestDate.Date < enrollment.EnrollmentDate.Date)
            throw new InvalidOperationException($"Request date cannot be before your enrollment date ({enrollment.EnrollmentDate:yyyy-MM-dd}).");

        // Check for duplicate pending/approved request on the same date
        var dateOnly = dto.RequestDate.Date;
        var duplicate = await _context.AttendanceRequests
            .AnyAsync(r =>
                r.EnrollmentID  == dto.EnrollmentID &&
                r.RequestDate   >= dateOnly          &&
                r.RequestDate   <  dateOnly.AddDays(1) &&
                (r.Status == AttendanceRequestStatus.Pending ||
                 r.Status == AttendanceRequestStatus.Approved));

        if (duplicate)
            throw new InvalidOperationException("An attendance request for this date already exists.");

        var request = new AttendanceRequest
        {
            EnrollmentID = dto.EnrollmentID,
            RequestDate  = dto.RequestDate.Date,
            Status       = AttendanceRequestStatus.Pending,
            CreatedAt    = DateTime.UtcNow
        };

        await _requestRepository.CreateAsync(request);

        return MapToDto(request, enrollment.EmployeeIdNavigation.Name,
                        enrollment.CourseIdNavigation.Title);
    }

    public async Task<List<AttendanceRequestResponseDto>> GetMyAttendanceRequestsAsync(int employeeId)
    {
        var requests = await _requestRepository.GetByEmployeeAsync(employeeId);
        return requests.Select(r => MapToDto(
            r,
            r.EnrollmentIdNavigation.EmployeeIdNavigation.Name,
            r.EnrollmentIdNavigation.CourseIdNavigation.Title)).ToList();
    }

    public async Task<List<AttendanceRequestResponseDto>> GetPendingRequestsForCourseAsync(
        int courseId, int trainerId)
    {
        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.CourseID == courseId);

        if (course == null)
            throw new KeyNotFoundException($"Course {courseId} not found.");

        if (course.TrainerID != trainerId)
            throw new UnauthorizedAccessException("You are not authorized to view requests for this course.");

        var requests = await _requestRepository.GetPendingByCourseAsync(courseId);
        return requests.Select(r => MapToDto(
            r,
            r.EnrollmentIdNavigation.EmployeeIdNavigation.Name,
            r.EnrollmentIdNavigation.CourseIdNavigation.Title)).ToList();
    }

    public async Task<AttendanceRequestResponseDto> ReviewAttendanceRequestAsync(
        int requestId, ReviewAttendanceRequestDto dto, int trainerId)
    {
        var request = await _requestRepository.GetByIdAsync(requestId);

        if (request == null)
            throw new KeyNotFoundException($"Attendance request {requestId} not found.");

        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.CourseID == request.EnrollmentIdNavigation.CourseID);

        if (course == null || course.TrainerID != trainerId)
            throw new UnauthorizedAccessException("You are not authorized to review this request.");

        if (request.Status != AttendanceRequestStatus.Pending)
            throw new InvalidOperationException("Only pending requests can be reviewed.");

        request.Status      = dto.Approved
            ? AttendanceRequestStatus.Approved
            : AttendanceRequestStatus.Rejected;
        request.TrainerNote = dto.Note;

        if (dto.Approved)
        {
            // Automatically mark attendance as Present when trainer approves
            var attendance = new Attendance
            {
                EnrollmentID   = request.EnrollmentID,
                AttendanceDate = request.RequestDate,
                Status         = AttendanceStatus.Present
            };
            await _attendanceRepository.UpsertAttendanceAsync(attendance);
        }

        await _requestRepository.SaveAsync();

        // Notify the employee of the decision
        var employeeId  = request.EnrollmentIdNavigation.EmployeeID;
        var courseId    = request.EnrollmentIdNavigation.CourseID;
        var courseTitle = request.EnrollmentIdNavigation.CourseIdNavigation.Title;

        await _notificationService.NotifyAttendanceRequestReviewedAsync(
            employeeId, courseId, courseTitle, dto.Approved, dto.Note);

        return MapToDto(
            request,
            request.EnrollmentIdNavigation.EmployeeIdNavigation.Name,
            request.EnrollmentIdNavigation.CourseIdNavigation.Title);
    }

    private static AttendanceRequestResponseDto MapToDto(
        AttendanceRequest r, string employeeName, string courseName)
    {
        return new AttendanceRequestResponseDto
        {
            RequestID    = r.RequestID,
            EnrollmentID = r.EnrollmentID,
            EmployeeName = employeeName,
            CourseName   = courseName,
            RequestDate  = r.RequestDate,
            Status       = r.Status.ToString(),
            TrainerNote  = r.TrainerNote,
            CreatedAt    = r.CreatedAt
        };
    }
}