using LibraryManagement.Models;

namespace LibraryManagement.ViewModels
{
    public class BookListViewModel
    {
        public decimal FineAmount { get; set; }
        public List<Book> Books { get; set; } = new();

        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public int? AuthorId { get; set; }
        public bool AvailableOnly { get; set; }
        public string SortOrder { get; set; } = "title";

        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}