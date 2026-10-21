using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.ViewModels
{
    public class BookFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "ISBN is required")]
        [RegularExpression(@"^(\d{10}|\d{13})$", ErrorMessage = "ISBN must be 10 or 13 digits")]
        public string ISBN { get; set; } = string.Empty;

        [Range(1000, 2100, ErrorMessage = "Enter a valid year")]
        [Display(Name = "Published Year")]
        public int PublishedYear { get; set; }

        [Range(1, 1000, ErrorMessage = "Copies must be between 1 and 1000")]
        [Display(Name = "Total Copies")]
        public int TotalCopies { get; set; } = 1;

        [Range(1, int.MaxValue, ErrorMessage = "Please select an author")]
        [Display(Name = "Author")]
        public int AuthorId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please select a category")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Display(Name = "Cover Image")]
        public IFormFile? CoverImage { get; set; }

        public string? ExistingCoverPath { get; set; }
    }
}