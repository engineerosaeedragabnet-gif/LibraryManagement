using LibraryManagement.Data;
using LibraryManagement.Enums;
using LibraryManagement.Models;
using LibraryManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers
{
    public class LoansController : Controller
    {
        private const int MaxActiveLoans = 3;
        private const decimal FinePerDay = 5m;

        private readonly AppDbContext _context;

        public LoansController(AppDbContext context)
        {
            _context = context;
        }

        // ============ 1) Index ============
        public async Task<IActionResult> Index(string? searchTerm, string filter = "all", int page = 1)
        {
            const int pageSize = 10;
            var now = DateTime.Now;

            IQueryable<Loan> query = _context.Loans.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(l => l.Member.FullName.Contains(term)
                                      || l.Book.Title.Contains(term));
            }

            query = filter switch
            {
                "active" => query.Where(l => l.ReturnDate == null),
                "overdue" => query.Where(l => l.ReturnDate == null && l.DueDate < now),
                "returned" => query.Where(l => l.ReturnDate != null),
                _ => query
            };

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            page = Math.Clamp(page, 1, Math.Max(1, totalPages));

            var loans = await query
                .OrderByDescending(l => l.LoanDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new LoanListItemViewModel
                {
                    Id = l.Id,
                    BookId = l.BookId,
                    BookTitle = l.Book.Title,
                    MemberId = l.MemberId,
                    MemberName = l.Member.FullName,
                    LoanDate = l.LoanDate,
                    DueDate = l.DueDate,
                    ReturnDate = l.ReturnDate,
                    FineAmount = l.FineAmount,
                    IsOverdue = l.ReturnDate == null && l.DueDate < now
                })
                .ToListAsync();

            return View(new LoanListViewModel
            {
                Loans = loans,
                SearchTerm = searchTerm,
                Filter = filter,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            });
        }

        // ============ 2) Create ============
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View(new LoanCreateViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LoanCreateViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(vm.MemberId, vm.BookId);
                return View(vm);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var member = await _context.Members.FindAsync(vm.MemberId);
            var book = await _context.Books.FindAsync(vm.BookId);

            string? error = null;
            var now = DateTime.Now;

            if (member == null || !member.IsActive)
            {
                error = "The selected member does not exist or is not active.";
            }
            else if (book == null || book.AvailableCopies <= 0)
            {
                error = "The selected book does not exist or has no available copies.";
            }
            else
            {
                var activeCount = await _context.Loans
                    .CountAsync(l => l.MemberId == vm.MemberId && l.ReturnDate == null);

                var hasOverdue = await _context.Loans.AnyAsync(l =>
                    l.MemberId == vm.MemberId && l.ReturnDate == null && l.DueDate < now);

                var alreadyBorrowed = await _context.Loans.AnyAsync(l =>
                    l.MemberId == vm.MemberId && l.BookId == vm.BookId && l.ReturnDate == null);

                if (activeCount >= MaxActiveLoans)
                    error = $"This member already has {MaxActiveLoans} active loans (the maximum).";
                else if (hasOverdue)
                    error = "This member has an overdue loan and must return it first.";
                else if (alreadyBorrowed)
                    error = "This member has already borrowed this book and has not returned it.";
            }

            if (error != null)
            {
                ModelState.AddModelError(string.Empty, error);
                await PopulateDropdowns(vm.MemberId, vm.BookId);
                return View(vm);
            }

            var loan = new Loan
            {
                MemberId = member!.Id,
                BookId = book!.Id,
                LoanDate = now,
                DueDate = now.AddDays(vm.LoanDays),
                Status = LoanStatus.Borrowed,
                FineAmount = 0
            };

            book.AvailableCopies--;
            _context.Loans.Add(loan);

            try
            {
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty,
                    "The book was just borrowed by someone else. Please try again.");
                await PopulateDropdowns(vm.MemberId, vm.BookId);
                return View(vm);
            }

            TempData["Success"] =
                $"\"{book.Title}\" was borrowed by {member.FullName}. Due date: {loan.DueDate:yyyy/MM/dd}.";
            return RedirectToAction(nameof(Index));
        }

        // ============ 3) Details ============
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var loan = await _context.Loans
                .AsNoTracking()
                .Include(l => l.Book)
                .Include(l => l.Member)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (loan == null) return NotFound();
            return View(loan);
        }

        // ============ 4) Return ============
        public async Task<IActionResult> Return(int? id)
        {
            if (id == null) return NotFound();

            var loan = await _context.Loans
                .AsNoTracking()
                .Include(l => l.Book)
                .Include(l => l.Member)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (loan == null) return NotFound();

            if (loan.ReturnDate != null)
            {
                TempData["Error"] = "This loan has already been returned.";
                return RedirectToAction(nameof(Details), new { id });
            }

            ViewBag.ExpectedFine = CalculateFine(loan.DueDate, DateTime.Now);
            return View(loan);
        }

        [HttpPost, ActionName("Return")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnConfirmed(int id)
        {
            var loan = await _context.Loans
                .Include(l => l.Book)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (loan == null) return NotFound();

            if (loan.ReturnDate != null)
            {
                TempData["Error"] = "This loan has already been returned.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var now = DateTime.Now;

            loan.ReturnDate = now;
            loan.FineAmount = CalculateFine(loan.DueDate, now);
            loan.Status = LoanStatus.Returned;

            if (loan.Book.AvailableCopies < loan.Book.TotalCopies)
                loan.Book.AvailableCopies++;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "The loan was modified by someone else. Please try again.";
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["Success"] = loan.FineAmount > 0
                ? $"Book returned. Late fine: {loan.FineAmount:0.00}"
                : "Book returned successfully.";

            return RedirectToAction(nameof(Index));
        }

        // ============ Helpers ============
        private async Task PopulateDropdowns(int? memberId = null, int? bookId = null)
        {
            var members = await _context.Members
                .AsNoTracking()
                .Where(m => m.IsActive)
                .OrderBy(m => m.FullName)
                .Select(m => new { m.Id, Text = m.FullName + " (" + m.Email + ")" })
                .ToListAsync();

            var books = await _context.Books
                .AsNoTracking()
                .Where(b => b.AvailableCopies > 0)
                .OrderBy(b => b.Title)
                .Select(b => new { b.Id, Text = b.Title + " (" + b.ISBN + ")" })
                .ToListAsync();

            ViewBag.Members = new SelectList(members, "Id", "Text", memberId);
            ViewBag.Books = new SelectList(books, "Id", "Text", bookId);
        }

        private static decimal CalculateFine(DateTime dueDate, DateTime returnDate)
        {
            if (returnDate <= dueDate) return 0m;

            var lateDays = (int)(returnDate - dueDate).TotalDays;
            return lateDays * FinePerDay;
        }
    }
}