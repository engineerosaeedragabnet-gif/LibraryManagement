using System.Diagnostics;
using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;

            var totalBooks = await _context.Books.CountAsync();
            var totalMembers = await _context.Members.CountAsync();
            var activeMembers = await _context.Members.CountAsync(m => m.IsActive);
            var activeLoans = await _context.Loans.CountAsync(l => l.ReturnDate == null);
            var overdueLoans = await _context.Loans
                .CountAsync(l => l.ReturnDate == null && l.DueDate < now);

            var totalCopies = await _context.Books.SumAsync(b => b.TotalCopies);
            var availableCopies = await _context.Books.SumAsync(b => b.AvailableCopies);
            var totalFines = await _context.Loans.SumAsync(l => l.FineAmount);

            var topBooks = await _context.Loans
                .AsNoTracking()
                .GroupBy(l => new { l.BookId, l.Book.Title, AuthorName = l.Book.Author.FullName })
                .Select(g => new TopBookViewModel
                {
                    BookId = g.Key.BookId,
                    Title = g.Key.Title,
                    AuthorName = g.Key.AuthorName,
                    LoansCount = g.Count()
                })
                .OrderByDescending(x => x.LoansCount)
                .ThenBy(x => x.Title)
                .Take(5)
                .ToListAsync();

            var recentLoans = await _context.Loans
                .AsNoTracking()
                .OrderByDescending(l => l.LoanDate)
                .Take(5)
                .Select(l => new RecentLoanViewModel
                {
                    LoanId = l.Id,
                    BookTitle = l.Book.Title,
                    MemberName = l.Member.FullName,
                    LoanDate = l.LoanDate,
                    DueDate = l.DueDate,
                    ReturnDate = l.ReturnDate,
                    IsOverdue = l.ReturnDate == null && l.DueDate < now
                })
                .ToListAsync();

            var overdueList = await _context.Loans
                .AsNoTracking()
                .Where(l => l.ReturnDate == null && l.DueDate < now)
                .OrderBy(l => l.DueDate)
                .Take(5)
                .Select(l => new RecentLoanViewModel
                {
                    LoanId = l.Id,
                    BookTitle = l.Book.Title,
                    MemberName = l.Member.FullName,
                    LoanDate = l.LoanDate,
                    DueDate = l.DueDate,
                    ReturnDate = l.ReturnDate,
                    IsOverdue = true
                })
                .ToListAsync();

            return View(new DashboardViewModel
            {
                TotalBooks = totalBooks,
                TotalCopies = totalCopies,
                AvailableCopies = availableCopies,
                TotalMembers = totalMembers,
                ActiveMembers = activeMembers,
                ActiveLoans = activeLoans,
                OverdueLoans = overdueLoans,
                TotalFines = totalFines,
                TopBooks = topBooks,
                RecentLoans = recentLoans,
                OverdueList = overdueList
            });
        }

        [Route("Home/NotFound")]
        public IActionResult NotFound404()
        {
            Response.StatusCode = 404;
            return View("NotFound");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}