using System;
using Skillforge.Domain;

namespace Skillforge.Repository;

public interface IEnrollmentRepository
{
    Task<Course> GetByIdAsync(int courseId);
    Task AddAuditLog(AuditLog auditLog);
    Task<bool> ExistsAsync(int courseId, int employeeId);
    Task AddAsync(Enrollment enrollment);
    Task<bool> EmployeeExistsAsync(int employeeId);
    Task<User?> GetEmployeeByIdAsync(int employeeId);
    Task<List<Enrollment>> GetAllEnrollmentsAsync();
    Task<List<Enrollment>> GetEnrollmentsByEmployeeAsync(int employeeId);
    Task<Enrollment?> GetByEnrollmentIdAsync(int enrollmentId);
    Task UpdateEnrollmentStatusAsync(int enrollmentId, bool status);
	Task<List<Enrollment>> GetEnrollmentsByTrainerAsync(int trainerId);
}
