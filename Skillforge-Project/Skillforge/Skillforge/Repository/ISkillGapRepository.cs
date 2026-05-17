using Skillforge.Domain;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Skillforge.Repository
{
    public interface ISkillGapRepository
    {
        Task<IEnumerable<SkillGap>> GetAllGapsAsync(
            DateTime? startDate,
            DateTime? endDate,
            int? employeeId = null,
            int? competencyId = null,
            int? gapLevel = null);

        Task<IEnumerable<SkillGap>> GetGapsByEmployeeAsync(int employeeId);
        Task<SkillGap?> GetByIdAsync(int id);
        Task<SkillGap> AddAsync(SkillGap skillGap);
        Task<bool> DeleteAsync(int id);
        Task SaveAsync();
    }
}