using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skillforge.Domain;
using Skillforge.Service;
using System.Security.Claims;

namespace Skillforge.Controller;

/// <summary>
/// Certifications are AUTO-issued when an employee completes a course
/// (all modules done + final assessment passed). Trainer evaluates Pass/Fail
/// and the cert is minted automatically. There is no manual "issue" path
/// from the UI anymore.
///
/// This controller exposes:
///   - GET  /my              : the caller's own certifications (Employee)
///   - GET  /certifications  : org-wide list (HR/Admin/Manager/Trainer)
///   - GET  /{id}/download   : downloads a single cert as a PDF
/// </summary>
[Route("api/v1/[controller]")]
[ApiController]
public class CertificationController : ControllerBase
{
    private readonly ICertificationService _certificationService;
    private readonly CertificatePdfGenerator _pdfGenerator;

    public CertificationController(
        ICertificationService certificationService,
        CertificatePdfGenerator pdfGenerator)
    {
        _certificationService = certificationService;
        _pdfGenerator         = pdfGenerator;
    }

    /// <summary>Returns the calling employee's own certifications.</summary>
    [HttpGet("my")]
    [Authorize(Roles = nameof(UserRole.Employee))]
    public async Task<IActionResult> GetMyCertifications()
    {
        var userIdClaim = User.FindFirstValue("id");
        int.TryParse(userIdClaim, out int userId);
        var result = await _certificationService.GetMyCertificationsAsync(userId);
        return Ok(result);
    }

    /// <summary>Downloads a certificate as PDF. Employees can only download their own.</summary>
    [HttpGet("{id}/download")]
    [Authorize]
    public async Task<IActionResult> DownloadCertificate(int id)
    {
        var userIdClaim = User.FindFirstValue("id");
        var roleClaim   = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
        int.TryParse(userIdClaim, out int userId);

        var all  = await _certificationService.GetAllCertificationsAsync();
        var cert = all.FirstOrDefault(c => c.CertificationId == id);

        if (cert == null)
            return NotFound(new { message = "Certificate not found." });

        if (roleClaim == nameof(UserRole.Employee) && cert.EmployeeId != userId)
            return Forbid();

        var pdf = _pdfGenerator.Generate(cert);
        return File(pdf, "application/pdf", $"certificate_{cert.CertificationId}.pdf");
    }

    /// <summary>
    /// Org-wide list of certifications. Employees use GET /my instead — listing
    /// the whole organization to them would leak other employees' cert metadata.
    /// </summary>
    [HttpGet("certifications")]
    [Authorize(Roles = nameof(UserRole.Admin) + "," + nameof(UserRole.HR) + "," + nameof(UserRole.Manager) + "," + nameof(UserRole.Trainer))]
    public async Task<IActionResult> GetCertifications()
    {
        try
        {
            var result = await _certificationService.GetAllCertificationsAsync();
            return Ok(result);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while fetching certifications." });
        }
    }
}
