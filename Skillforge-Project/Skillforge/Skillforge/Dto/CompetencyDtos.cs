using Skillforge.Domain;

namespace Skillforge.Dto
{
    public class CompetencyResponseDto
    {
        public int CompetencyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
    }

    public class CreateCompetencyDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public CompetencyLevel Level { get; set; }
    }

    public class UpdateCompetencyDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public CompetencyLevel Level { get; set; }
    }

    public class CreateSkillGapDto
    {
        public int EmployeeId { get; set; }
        public int CompetencyId { get; set; }
        public SkillGapLevel GapLevel { get; set; }
        public DateTime DateIdentified { get; set; }
    }
}
