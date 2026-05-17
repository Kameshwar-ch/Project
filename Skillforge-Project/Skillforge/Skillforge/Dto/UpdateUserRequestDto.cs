using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Skillforge.Domain;
public class UpdateUserRequestDto
{
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; }
    public UserRole Role { get; set; }

    [Required(ErrorMessage = "Phone is required")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone must be exactly 10 digits")]
    public string Phone { get; set; }
    public bool Status { get; set; }
}