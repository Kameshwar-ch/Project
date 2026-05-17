namespace Skillforge.Dto
{
    public class CourseResponseDto
    {
        public int CourseID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int TrainerID { get; set; }
        public string TrainerName { get; set; }
        public decimal Duration { get; set; }
        public bool Status { get; set; }
        public int EnrollmentCount { get; set; }
    }
}