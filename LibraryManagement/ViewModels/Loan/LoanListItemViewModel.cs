namespace LibraryManagement.ViewModels
{
    public class LoanListItemViewModel
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public int MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public DateTime LoanDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public decimal FineAmount { get; set; }
        public bool IsOverdue { get; set; }
    }

    public class LoanListViewModel
    {
        public List<LoanListItemViewModel> Loans { get; set; } = new();

        public string? SearchTerm { get; set; }
        public string Filter { get; set; } = "all";

        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}