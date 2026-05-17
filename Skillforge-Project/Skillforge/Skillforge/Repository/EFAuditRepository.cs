using System;
using Skillforge.Data;
using Skillforge.Domain;
using Skillforge.Service;
// using SkillForgeLibrary.Models;

namespace Skillforge.Repository;

/// <summary>
/// A repository implementation of the IAuditService that uses Entity Framework Core 
/// to persist system activities and security events into the SQL Server database.
/// </summary>

public class EFAuditRepository : IAuditService
{

    /// <summary>
    /// Asynchronously records a specific action or event into the AuditLog table.
    /// Captures the associated User ID, the type of action performed, the affected resource, 
    /// and a UTC timestamp for precise chronological tracking.
    /// </summary>
    /// <param name="userid">The unique identifier of the user performing the action (null for anonymous/system events).</param>
    /// <param name="action">A short description of the activity (e.g., "Login Success", "Record Deleted").</param>
    /// <param name="resource">The specific endpoint or entity being accessed (e.g., "Auth/Login", "Course/101").</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    /// <exception cref="Exception">Thrown if there is a database connectivity issue or a constraint violation during save.</exception>
    
    private readonly SkillForgeDB _context;

    public EFAuditRepository(SkillForgeDB context)
    {
        _context = context;
    }
    public async Task LogAsync(int? userid, string action, string resource)
    {
        var log = new AuditLog
        {
            UserID = userid,
            Action = action,
            Resource = resource,
            Timestamp = DateTime.UtcNow
        };

        try
        {
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (System.Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
}
