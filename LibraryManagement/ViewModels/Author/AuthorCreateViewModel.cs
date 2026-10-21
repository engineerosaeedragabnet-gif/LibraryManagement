using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.ViewModels.Auther
{
    public class AuthorCreateViewModel
    {
        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;
        [Required, MaxLength(1000)]
        public string? Bio {  get; set; }
           
    }
}
