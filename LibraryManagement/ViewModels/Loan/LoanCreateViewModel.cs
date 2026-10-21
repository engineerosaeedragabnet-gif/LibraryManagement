using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.ViewModels
{
    public class LoanCreateViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Please select a member")]
        [Display(Name = "Member")]
        public int MemberId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please select a book")]
        [Display(Name = "Book")]
        public int BookId { get; set; }

        [Range(1, 30, ErrorMessage = "Loan period must be between 1 and 30 days")]
        [Display(Name = "Loan Period (days)")]
        public int LoanDays { get; set; } = 14;
    }
}