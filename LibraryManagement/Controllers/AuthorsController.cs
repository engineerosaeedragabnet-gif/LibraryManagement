using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers
{
    public class AuthorsController : Controller
    {
        private readonly AppDbContext _context;

        public AuthorsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Authors
        public async Task<IActionResult> Index()
        {
            var authors = await _context.Authors
                .AsNoTracking()
                .OrderBy(a => a.FullName)
                .Select(a => new AuthorListItemViewModel
                {
                    Id = a.Id,
                    FullName = a.FullName,
                    BooksCount = a.Books.Count
                })
                .ToListAsync();

            return View(authors);
        }

        // GET: Authors/Create
        public IActionResult Create() => View(new AuthorFormViewModel());

        // POST: Authors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AuthorFormViewModel model)
        {
            model.FullName = model.FullName?.Trim() ?? string.Empty;

            if (ModelState.IsValid && await NameExistsAsync(model.FullName))
                ModelState.AddModelError(nameof(model.FullName), "An author with this name already exists");

            if (!ModelState.IsValid) return View(model);

            var author = new Author
            {
                FullName = model.FullName,
                Bio = model.Bio?.Trim()
            };

            _context.Authors.Add(author);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Author created successfully";
            return RedirectToAction(nameof(Index));
        }

        // GET: Authors/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var author = await _context.Authors.FindAsync(id);
            if (author == null) return NotFound();

            return View(new AuthorFormViewModel
            {
                Id = author.Id,
                FullName = author.FullName,
                Bio = author.Bio
            });
        }

        // POST: Authors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AuthorFormViewModel model)
        {
            if (model.Id != id) return NotFound();

            var author = await _context.Authors.FindAsync(id);
            if (author == null) return NotFound();

            model.FullName = model.FullName?.Trim() ?? string.Empty;

            if (ModelState.IsValid && await NameExistsAsync(model.FullName, excludeId: id))
                ModelState.AddModelError(nameof(model.FullName), "An author with this name already exists");

            if (!ModelState.IsValid) return View(model);

            author.FullName = model.FullName;
            author.Bio = model.Bio?.Trim();

            await _context.SaveChangesAsync();

            TempData["Success"] = "Author updated successfully";
            return RedirectToAction(nameof(Index));
        }

        // GET: Authors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var author = await _context.Authors
                .AsNoTracking()
                .Include(a => a.Books)
                    .ThenInclude(b => b.Category)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (author == null) return NotFound();
            return View(author);
        }

        // GET: Authors/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var author = await _context.Authors
                .AsNoTracking()
                .Include(a => a.Books)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (author == null) return NotFound();
            return View(author);
        }

        // POST: Authors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var author = await _context.Authors
                .Include(a => a.Books)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (author == null) return NotFound();

            if (author.Books.Any())
            {
                ModelState.AddModelError(string.Empty,
                    "Cannot delete an author who has books. Delete or reassign the books first.");
                return View(author);
            }

            _context.Authors.Remove(author);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Author deleted";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Helpers ----------

        private Task<bool> NameExistsAsync(string name, int? excludeId = null)
        {
            return _context.Authors.AnyAsync(a =>
                a.FullName == name && (excludeId == null || a.Id != excludeId));
        }
    }
}