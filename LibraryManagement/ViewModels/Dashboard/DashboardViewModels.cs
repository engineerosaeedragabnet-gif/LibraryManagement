namespace LibraryManagement.ViewModels
{
    public class TopBookViewModel
    {
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public int LoansCount { get; set; }
    }

    public class RecentLoanViewModel
    {
        public int LoanId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public DateTime LoanDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public bool IsOverdue { get; set; }
    }

    public class DashboardViewModel
    {
        public int TotalBooks { get; set; }
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }

        public int TotalMembers { get; set; }
        public int ActiveMembers { get; set; }

        public int ActiveLoans { get; set; }
        public int OverdueLoans { get; set; }
        public decimal TotalFines { get; set; }

        public List<TopBookViewModel> TopBooks { get; set; } = new();
        public List<RecentLoanViewModel> RecentLoans { get; set; } = new();
        public List<RecentLoanViewModel> OverdueList { get; set; } = new();
    }
}