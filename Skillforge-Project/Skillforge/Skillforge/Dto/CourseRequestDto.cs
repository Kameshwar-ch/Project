using System.ComponentModel.DataAnnotations;

namespace Skillforge.Dto
{
    public class CourseRequestDto
    {
        [Required(ErrorMessage = "The Title field is required and cannot be empty.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "The Description field is required.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "A valid Trainer ID must be provided.")]
        public int TrainerID { get; set; }

        [Range(1, 100, ErrorMessage = "The field Duration must be between 1 and 100.")]
        public decimal Duration { get; set; }
    }
}