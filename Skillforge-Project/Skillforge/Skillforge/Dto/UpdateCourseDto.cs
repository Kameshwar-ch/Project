using System.ComponentModel.DataAnnotations;

namespace Skillforge.Dto
{
    public class UpdateCourseDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int TrainerID { get; set; }

        [Range(1, 100)]
        public decimal Duration { get; set; }

        public bool Status { get; set; }
    }
}
