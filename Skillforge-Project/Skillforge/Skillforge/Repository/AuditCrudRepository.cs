using Skillforge.Data;
using Skillforge.Domain;
using Microsoft.EntityFrameworkCore;

namespace Skillforge.Repository
{
	public class AuditCrudRepository:IAuditCrudRepository

	{
		private readonly SkillForgeDB _context;

		public AuditCrudRepository(SkillForgeDB context)
		{
			_context = context;
		}

		public async Task<List<Audit>> GetAllAsync()
		{
			return await _context.Audits
				.Include(a => a.HRUser)
				.OrderByDescending(a => a.Date)
				.ToListAsync();
		}

		public async Task<Audit> CreateAsync(Audit audit)
		{
			await _context.Audits.AddAsync(audit);
			await _context.SaveChangesAsync();
			return audit;
		}

		public async Task<bool> DeleteAsync(int auditId)
		{
			var audit = await _context.Audits.FindAsync(auditId);
			if (audit == null) return false;
			_context.Audits.Remove(audit);
			await _context.SaveChangesAsync();
			return true;
		}
	}
}
