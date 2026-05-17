using Microsoft.EntityFrameworkCore;
using Skillforge.Data;
using Skillforge.Domain;

namespace Skillforge.Repository
{
	public class CompetencyRepository : ICompetencyRepository
	{
		private readonly SkillForgeDB _context;
		public CompetencyRepository(SkillForgeDB context)
		{
			_context = context;
		}

		public async Task<List<User>> GetEmployeesWithSkillsAsync()
		{
			return await _context.Users
				.Where(u => u.Role == UserRole.Employee)
				.Include(u => u.SkillGaps)
					.ThenInclude(sg => sg.Competency)
				.ToListAsync();
		}

		public async Task<List<Competency>> GetAllAsync()
		{
			return await _context.Competencies.OrderBy(c => c.Name).ToListAsync();
		}

		public async Task<Competency?> GetByIdAsync(int id)
		{
			return await _context.Competencies.FindAsync(id);
		}

		public async Task AddAsync(Competency competency)
		{
			await _context.Competencies.AddAsync(competency);
		}

		public async Task<bool> UpdateAsync(int id, string name, string description, CompetencyLevel level)
		{
			var competency = await _context.Competencies.FindAsync(id);
			if (competency == null) return false;
			competency.Name        = name;
			competency.Description = description;
			competency.Level       = level;
			return true;
		}

		public async Task<bool> DeleteAsync(int id)
		{
			var competency = await _context.Competencies.FindAsync(id);
			if (competency == null) return false;
			_context.Competencies.Remove(competency);
			return true;
		}

		public async Task SaveAsync() => await _context.SaveChangesAsync();
	}
}
