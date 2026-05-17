using Microsoft.EntityFrameworkCore;
using Skillforge.Data;
using Skillforge.Domain;

namespace Skillforge.Repository
{
    public class SkillGapRepository : ISkillGapRepository
    {
        private readonly SkillForgeDB _context;

        public SkillGapRepository(SkillForgeDB context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SkillGap>> GetAllGapsAsync(
            DateTime? startDate, 
            DateTime? endDate, 
            int? employeeId = null, 
            int? competencyId = null, 
            int? gapLevel = null)
        {
            var query = _context.SkillGaps
                .Include(g => g.Employee)
                .Include(g => g.Competency)
                .AsQueryable();

            // Date Filters (Jira Requirement)
            if (startDate.HasValue)
                query = query.Where(g => g.DateIdentified >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(g => g.DateIdentified <= endDate.Value);

            // New Functional Filters
            if (employeeId.HasValue)
                query = query.Where(g => g.EmployeeID == employeeId.Value);

            if (competencyId.HasValue)
                query = query.Where(g => g.CompetencyID == competencyId.Value);

          if (gapLevel.HasValue)
            {
    // Cast g.GapLevel to (int) to allow comparison with gapLevel.Value
                query = query.Where(g => (int)g.GapLevel == gapLevel.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<SkillGap>> GetGapsByEmployeeAsync(int employeeId)
        {
            return await _context.SkillGaps
                .Include(g => g.Competency)
                .Where(g => g.EmployeeID == employeeId)
                .ToListAsync();
        }

        public async Task<SkillGap?> GetByIdAsync(int id)
        {
            return await _context.SkillGaps.FindAsync(id);
        }

        public async Task<SkillGap> AddAsync(SkillGap skillGap)
        {
            await _context.SkillGaps.AddAsync(skillGap);
            return skillGap;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var gap = await _context.SkillGaps.FindAsync(id);
            if (gap == null) return false;
            _context.SkillGaps.Remove(gap);
            return true;
        }

        public async Task SaveAsync() => await _context.SaveChangesAsync();
    }
}