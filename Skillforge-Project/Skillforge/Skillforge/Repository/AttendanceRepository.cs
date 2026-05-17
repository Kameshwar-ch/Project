using Microsoft.EntityFrameworkCore;
using Skillforge.Data;
using Skillforge.Domain;

namespace Skillforge.Repository;

public class AttendanceRepository : IAttendanceRepository
{
    private readonly SkillForgeDB _context;

    /// <summary>
    /// Initializes AttendanceRepository with database context.
    /// </summary>
    public AttendanceRepository(SkillForgeDB context)
    {
        _context = context;
    }

    public async Task<List<Attendance>> GetByEnrollmentAsync(int enrollmentId)
    {
        return await _context.Attendances
            .Where(a => a.EnrollmentID == enrollmentId)
            .OrderByDescending(a => a.AttendanceDate)
            .ToListAsync();
    }

    /// <summary>
    /// Upserts attendance for a given enrollment + date.
    /// New record → inserts. Existing record → updates Status and AttendanceDate.
    /// </summary>
    public async Task<(Attendance attendance, bool isNew)> UpsertAttendanceAsync(Attendance attendance)
    {
        var dateOnly = attendance.AttendanceDate.Date;

        var existing = await _context.Attendances
            .FirstOrDefaultAsync(a =>
                a.EnrollmentID   == attendance.EnrollmentID &&
                a.AttendanceDate >= dateOnly                &&
                a.AttendanceDate <  dateOnly.AddDays(1));

        if (existing == null)
        {
            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();
            return (attendance, true);
        }
        else
        {
            existing.Status         = attendance.Status;
            existing.AttendanceDate = attendance.AttendanceDate;
            await _context.SaveChangesAsync();
            return (existing, false);
        }
    }
}