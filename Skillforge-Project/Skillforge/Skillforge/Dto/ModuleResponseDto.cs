namespace Skillforge.Dto
{
    public class ModuleResponseDto
    {
        public int ModuleID { get; set; }
        public int CourseID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ContentURI { get; set; } = string.Empty;
        public decimal Duration { get; set; }
        public bool Status { get; set; }
    }
}
