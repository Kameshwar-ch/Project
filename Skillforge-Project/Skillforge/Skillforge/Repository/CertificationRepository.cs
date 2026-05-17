using Microsoft.EntityFrameworkCore;
using Skillforge.Data;
using Skillforge.Domain;

namespace Skillforge.Repository;

/// <summary>
/// Entity Framework Core implementation of ICertificationRepository.
/// </summary>
public class CertificationRepository : ICertificationRepository
{
    private readonly SkillForgeDB _context;

    public CertificationRepository(SkillForgeDB context)
    {
        _context = context;
    }

    public async Task<List<Certification>> GetAllCertifications()
        => await _context.Certifications.ToListAsync();

    public async Task<User?> GetUserByIdAsync(int userId)
        => await _context.Users.FindAsync(userId);

    public async Task<Course?> GetCourseByIdAsync(int courseId)
        => await _context.Courses.FindAsync(courseId);

    public async Task<bool> HasPassedAssessmentForCourseAsync(int employeeId, int courseId)
        => await _context.Results
            .AnyAsync(r =>
                r.EmployeeID == employeeId &&
                r.Status == ResultStatus.Pass &&
                r.Assessment.CourseID == courseId);

    public async Task<Certification?> GetActiveCertificationAsync(int employeeId, int courseId)
        => await _context.Certifications
            .FirstOrDefaultAsync(c =>
                c.EmployeeID == employeeId &&
                c.CourseID == courseId &&
                c.Status == "Active");

    public async Task<int> IssueCertificationAsync(Certification certification)
    {
        _context.Certifications.Add(certification);
        await _context.SaveChangesAsync();
        return certification.CertificationID;
    }

    public async Task<List<Certification>> GetExpiringCertificationsAsync(int daysAhead)
    {
        var targetDate = DateTime.UtcNow.Date.AddDays(daysAhead);
        var nextDay    = targetDate.AddDays(1);

        return await _context.Certifications
            .Include(c => c.Course)
            .Where(c => c.Status == "Active"
                     && c.ExpiryDate >= targetDate
                     && c.ExpiryDate <  nextDay)
            .ToListAsync();
    }

    public async Task<List<Certification>> GetExpiredActiveCertificationsAsync()
        => await _context.Certifications
            .Include(c => c.Course)
            .Where(c => c.Status == "Active" && c.ExpiryDate < DateTime.UtcNow)
            .ToListAsync();

    public async Task UpdateStatusAsync(int certificationId, string status)
    {
        var cert = await _context.Certifications.FindAsync(certificationId);
        if (cert is null) return;
        cert.Status = status;
        await _context.SaveChangesAsync();
    }
}
