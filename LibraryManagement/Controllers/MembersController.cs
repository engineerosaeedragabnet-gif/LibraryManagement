using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.ViewModels.Member;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers
{
    public class MembersController : Controller
    {
        private readonly AppDbContext _context;

        public MembersController(AppDbContext context)
        {
            _context = context;
        }

        // ============ 1) Index ============
        public async Task<IActionResult> Index(string? searchTerm, string? status, int page = 1)
        {
            const int pageSize = 10;

            IQueryable<Member> query = _context.Members.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(m => m.FullName.Contains(term) || m.Email.Contains(term));
            }

            query = status switch
            {
                "active" => query.Where(m => m.IsActive),
                "inactive" => query.Where(m => !m.IsActive),
                _ => query
            };

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            page = Math.Clamp(page, 1, Math.Max(1, totalPages));

            var members = await query
                .OrderBy(m => m.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MemberListItemViewModel
                {
                    Id = m.Id,
                    FullName = m.FullName,
                    Email = m.Email,
                    IsActive = m.IsActive,
                    ActiveLoansCount = m.Loans.Count(l => l.ReturnDate == null)
                })
                .ToListAsync();

            return View(new MemberListViewModel
            {
                Members = members,
                SearchTerm = searchTerm,
                Status = status,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            });
        }

        // ============ 2) Create ============
        public IActionResult Create() => View(new MemberFormViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MemberFormViewModel model)
        {
            model.FullName = model.FullName?.Trim() ?? string.Empty;
            model.Email = model.Email?.Trim().ToLowerInvariant() ?? string.Empty;
            model.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();

            if (ModelState.IsValid && await EmailExistsAsync(model.Email))
                ModelState.AddModelError(nameof(model.Email), "This email has already been registered.");

            if (!ModelState.IsValid)
                return View(model);

            _context.Members.Add(new Member
            {
                FullName = model.FullName,
                Email = model.Email,
                Phone = model.Phone,
                MembershipDate = DateTime.Today,
                IsActive = true
            });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(nameof(model.Email), "This email has already been registered.");
                return View(model);
            }

            TempData["Success"] = "The member has been added successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ============ 3) Details ============
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var member = await _context.Members
                .AsNoTracking()
                .Include(m => m.Loans.OrderByDescending(l => l.LoanDate))
                    .ThenInclude(l => l.Book)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (member == null) return NotFound();
            return View(member);
        }

        // ============ 4) Edit ============
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var member = await _context.Members
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (member == null) return NotFound();

            return View(new MemberFormViewModel
            {
                Id = member.Id,
                FullName = member.FullName,
                Email = member.Email,
                Phone = member.Phone,
                IsActive = member.IsActive
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MemberFormViewModel model)
        {
            if (id != model.Id) return NotFound();

            // من غير AsNoTracking عشان هنعدّل
            var member = await _context.Members.FindAsync(id);
            if (member == null) return NotFound();

            // نضّف بيانات الفورم (model) مش بيانات الداتابيز
            model.FullName = model.FullName?.Trim() ?? string.Empty;
            model.Email = model.Email?.Trim().ToLowerInvariant() ?? string.Empty;
            model.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();

            if (ModelState.IsValid && await EmailExistsAsync(model.Email, excludeId: id))
                ModelState.AddModelError(nameof(model.Email), "This email has already been registered.");

            if (!ModelState.IsValid)
                return View(model);

            member.FullName = model.FullName;
            member.Email = model.Email;
            member.Phone = model.Phone;
            member.IsActive = model.IsActive;
            // MembershipDate ما بتتلمسش

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(nameof(model.Email), "This email has already been registered.");
                return View(model);
            }

            TempData["Success"] = "Member details have been updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ============ 5) ToggleActive ============
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var member = await _context.Members
                .Include(m => m.Loans)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (member == null) return NotFound();

            member.IsActive = !member.IsActive;
            await _context.SaveChangesAsync();

            var activeLoans = member.Loans.Count(l => l.ReturnDate == null);

            if (member.IsActive)
                TempData["Success"] = $"{member.FullName} has been activated.";
            else if (activeLoans > 0)
                TempData["Error"] = $"{member.FullName} was deactivated but still has {activeLoans} active loan(s).";
            else
                TempData["Success"] = $"{member.FullName} has been deactivated.";

            return RedirectToAction(nameof(Index));
        }

        // ============ 6) Delete ============
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var member = await _context.Members
                .AsNoTracking()
                .Include(m => m.Loans)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (member == null) return NotFound();
            return View(member);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var member = await _context.Members
                .Include(m => m.Loans)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (member == null) return NotFound();

            if (member.Loans.Any())
            {
                ModelState.AddModelError(string.Empty,
                    "Cannot delete a member who has loan history. Deactivate instead.");
                return View(member);
            }

            _context.Members.Remove(member);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "The member could not be deleted.");
                return View(member);
            }

            TempData["Success"] = "Member deleted";
            return RedirectToAction(nameof(Index));
        }

        // ============ Helpers ============
        private Task<bool> EmailExistsAsync(string email, int? excludeId = null)
        {
            return _context.Members.AnyAsync(m =>
                m.Email == email && (excludeId == null || m.Id != excludeId));
        }
    }
}