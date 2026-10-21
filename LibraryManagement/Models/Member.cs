using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Models
{
    [Index(nameof(Email), IsUnique = true)]
    public class Member
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        public DateTime MembershipDate { get; set; } = DateTime.Today;
        public bool IsActive { get; set; } = true;

        public List<Loan> Loans { get; set; } = new();
    }
}