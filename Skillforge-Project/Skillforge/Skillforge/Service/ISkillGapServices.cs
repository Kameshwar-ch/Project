using Skillforge.Dto;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Skillforge.Service
{
    public interface ISkillGapService
    {
        Task<List<SkillGapResponseDto>> GetFilteredGapsAsync(
            DateTime? startDate,
            DateTime? endDate,
            int? employeeId,
            int? competencyId,
            int? gapLevel);

        Task<List<SkillGapResponseDto>> GetGapsByEmployeeAsync(int employeeId);
        Task<SkillGapResponseDto> CreateSkillGapAsync(CreateSkillGapDto dto);
        Task<bool> DeleteSkillGapAsync(int id);
    }
}