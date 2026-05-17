using Skillforge.Domain;
using Skillforge.Dto;
using Skillforge.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Skillforge.Service
{
    public class SkillGapService : ISkillGapService
    {
        private readonly ISkillGapRepository _repository;
        private readonly ICompetencyRepository _competencyRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationService _notificationService;

        public SkillGapService(
            ISkillGapRepository repository,
            ICompetencyRepository competencyRepository,
            IUserRepository userRepository,
            INotificationService notificationService)
        {
            _repository            = repository;
            _competencyRepository  = competencyRepository;
            _userRepository        = userRepository;
            _notificationService   = notificationService;
        }

        public async Task<List<SkillGapResponseDto>> GetFilteredGapsAsync(
            DateTime? startDate,
            DateTime? endDate,
            int? employeeId,
            int? competencyId,
            int? gapLevel)
        {
            var gaps = await _repository.GetAllGapsAsync(startDate, endDate, employeeId, competencyId, gapLevel);
            return gaps.Select(g => new SkillGapResponseDto
            {
                SkillGapID     = g.SkillGapID,
                EmployeeId     = g.EmployeeID,
                EmployeeName   = g.Employee?.Name ?? "N/A",
                CompetencyName = g.Competency?.Name ?? "N/A",
                GapLevel       = g.GapLevel.ToString(),
                DateIdentified = g.DateIdentified
            }).ToList();
        }

        public async Task<List<SkillGapResponseDto>> GetGapsByEmployeeAsync(int employeeId)
        {
            var gaps = await _repository.GetGapsByEmployeeAsync(employeeId);
            return gaps.Select(g => new SkillGapResponseDto
            {
                SkillGapID     = g.SkillGapID,
                EmployeeId     = g.EmployeeID,
                EmployeeName   = g.Employee?.Name ?? "N/A",
                CompetencyName = g.Competency?.Name ?? "N/A",
                GapLevel       = g.GapLevel.ToString(),
                DateIdentified = g.DateIdentified
            }).ToList();
        }

        public async Task<SkillGapResponseDto> CreateSkillGapAsync(CreateSkillGapDto dto)
        {
            if (dto.EmployeeId <= 0)
                throw new ArgumentException("Valid employee ID is required.");
            if (dto.CompetencyId <= 0)
                throw new ArgumentException("Valid competency ID is required.");
            if (dto.DateIdentified > DateTime.UtcNow.Date.AddDays(1))
                throw new ArgumentException("Date identified cannot be in the future.");

            var competency = await _competencyRepository.GetByIdAsync(dto.CompetencyId)
                ?? throw new KeyNotFoundException("Competency not found.");

            var employee = await _userRepository.GetUserByIdAsync(dto.EmployeeId)
                ?? throw new KeyNotFoundException("Employee not found.");

            var skillGap = new SkillGap
            {
                EmployeeID     = dto.EmployeeId,
                CompetencyID   = dto.CompetencyId,
                GapLevel       = dto.GapLevel,
                DateIdentified = dto.DateIdentified
            };
            await _repository.AddAsync(skillGap);
            await _repository.SaveAsync();

            await _notificationService.NotifySkillGapIdentifiedAsync(
                dto.EmployeeId,
                competency.Name,
                dto.GapLevel.ToString());

            return new SkillGapResponseDto
            {
                SkillGapID     = skillGap.SkillGapID,
                EmployeeId     = skillGap.EmployeeID,
                EmployeeName   = employee.Name,
                CompetencyName = competency.Name,
                GapLevel       = skillGap.GapLevel.ToString(),
                DateIdentified = skillGap.DateIdentified
            };
        }

        public async Task<bool> DeleteSkillGapAsync(int id)
        {
            bool deleted = await _repository.DeleteAsync(id);
            if (!deleted) return false;
            await _repository.SaveAsync();
            return true;
        }
    }
}
