namespace LibraryManagement.ViewModels.Member
{
    public class MemberListViewModel
    {
        public List<MemberListItemViewModel> Members { get; set; } = new();

        public string? SearchTerm { get; set; }
        public string? Status { get; set; }          // "active" / "inactive" / null = All

        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

    }
}
