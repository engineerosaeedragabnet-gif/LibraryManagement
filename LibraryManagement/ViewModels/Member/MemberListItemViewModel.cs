namespace LibraryManagement.ViewModels.Member
{
    public class MemberListItemViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int ActiveLoansCount { get; set; }
    }
}
