namespace Skillforge.Dto
{
    public class SkillGapResponseDto
    {
        public int SkillGapID { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string CompetencyName { get; set; }
        public string GapLevel { get; set; } // String version of the Enum
        public DateTime DateIdentified { get; set; }
    }
}