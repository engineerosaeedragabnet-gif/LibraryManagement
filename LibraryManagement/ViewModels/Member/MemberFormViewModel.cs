using LibraryManagement.Validation;
using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.ViewModels.Member
{
    public class MemberFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        [MaxLength(150, ErrorMessage = "Email cannot exceed 150 characters")]
        public string Email { get; set; } = string.Empty;

        [RegularExpression(@"^\+?\d{10,15}$",
            ErrorMessage = "Phone must be 10 to 15 digits and may start with +")]
        public string? Phone { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
        [NotInFuture]
        [Display(Name = "Birth Date")]
        public DateTime? BirthDate { get; set; }

    }
}
