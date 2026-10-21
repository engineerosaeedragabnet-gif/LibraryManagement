using System.ComponentModel.DataAnnotations.Schema;
using LibraryManagement.Enums;

namespace LibraryManagement.Models
{
    public class Loan
    {
        public int Id { get; set; }

        public int BookId { get; set; }
        public Book Book { get; set; } = null!;

        public int MemberId { get; set; }
        public Member Member { get; set; } = null!;

        public DateTime LoanDate { get; set; } = DateTime.Now;
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }

        public LoanStatus Status { get; set; } = LoanStatus.Borrowed;

        [Column(TypeName = "decimal(10,2)")]
        public decimal FineAmount { get; set; }
    }
}