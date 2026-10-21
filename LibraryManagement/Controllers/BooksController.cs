using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers
{
    public class BooksController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxFileSize = 2 * 1024 * 1024; // 2 MB

        public BooksController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // ============ 1) Index ============
        public async Task<IActionResult> Index(
            string? searchTerm,
            int? categoryId,
            int? authorId,
            bool availableOnly = false,
            string sortOrder = "title",
            int page = 1)
        {
            const int pageSize = 8;

            IQueryable<Book> query = _context.Books
                .AsNoTracking()
                .Include(b => b.Author)
                .Include(b => b.Category);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(b => b.Title.Contains(term)
                                      || b.ISBN.Contains(term)
                                      || b.Author.FullName.Contains(term));
            }

            if (categoryId.HasValue)
                query = query.Where(b => b.CategoryId == categoryId.Value);

            if (authorId.HasValue)
                query = query.Where(b => b.AuthorId == authorId.Value);

            if (availableOnly)
                query = query.Where(b => b.AvailableCopies > 0);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            page = Math.Clamp(page, 1, Math.Max(1, totalPages));

            query = sortOrder switch
            {
                "newest" => query.OrderByDescending(b => b.Id),
                _ => query.OrderBy(b => b.Title)
            };

            var books = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new BookListViewModel
            {
                Books = books,
                SearchTerm = searchTerm,
                CategoryId = categoryId,
                AuthorId = authorId,
                AvailableOnly = availableOnly,
                SortOrder = sortOrder,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };

            await PopulateDropdowns(authorId, categoryId);
            return View(vm);
        }

        // ============ 2) Create ============
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View(new BookFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookFormViewModel vm)
        {
            vm.Title = vm.Title?.Trim() ?? string.Empty;
            vm.ISBN = vm.ISBN?.Replace("-", "").Trim() ?? string.Empty;

            ValidateImage(vm.CoverImage, nameof(vm.CoverImage));

            if (ModelState.IsValid && await IsbnExistsAsync(vm.ISBN))
                ModelState.AddModelError(nameof(vm.ISBN), "A book with this ISBN already exists");

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(vm.AuthorId, vm.CategoryId);
                return View(vm);
            }

            var book = new Book
            {
                Title = vm.Title,
                ISBN = vm.ISBN,
                PublishedYear = vm.PublishedYear,
                TotalCopies = vm.TotalCopies,
                AvailableCopies = vm.TotalCopies,
                AuthorId = vm.AuthorId,
                CategoryId = vm.CategoryId
            };

            if (vm.CoverImage != null)
                book.CoverImagePath = await SaveImageAsync(vm.CoverImage);

            _context.Books.Add(book);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                DeleteImage(book.CoverImagePath);
                ModelState.AddModelError(nameof(vm.ISBN), "A book with this ISBN already exists");
                await PopulateDropdowns(vm.AuthorId, vm.CategoryId);
                return View(vm);
            }

            TempData["Success"] = "Book created successfully";
            return RedirectToAction(nameof(Index));
        }

        // ============ 3) Details ============
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var book = await _context.Books
                .AsNoTracking()
                .Include(b => b.Author)
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null) return NotFound();
            return View(book);
        }

        // ============ 4) Edit ============
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();

            var vm = new BookFormViewModel
            {
                Id = book.Id,
                Title = book.Title,
                ISBN = book.ISBN,
                PublishedYear = book.PublishedYear,
                TotalCopies = book.TotalCopies,
                AuthorId = book.AuthorId,
                CategoryId = book.CategoryId,
                ExistingCoverPath = book.CoverImagePath
            };

            await PopulateDropdowns(book.AuthorId, book.CategoryId);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BookFormViewModel vm)
        {
            if (id != vm.Id) return NotFound();

            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();

            vm.Title = vm.Title?.Trim() ?? string.Empty;
            vm.ISBN = vm.ISBN?.Replace("-", "").Trim() ?? string.Empty;

            ValidateImage(vm.CoverImage, nameof(vm.CoverImage));

            if (ModelState.IsValid && await IsbnExistsAsync(vm.ISBN, excludeId: id))
                ModelState.AddModelError(nameof(vm.ISBN), "A book with this ISBN already exists");

            // المستعار من القيم القديمة (قبل أي تعديل)
            var borrowed = book.TotalCopies - book.AvailableCopies;
            if (vm.TotalCopies < borrowed)
                ModelState.AddModelError(nameof(vm.TotalCopies),
                    $"Cannot set total copies below the number currently borrowed ({borrowed})");

            if (!ModelState.IsValid)
            {
                vm.ExistingCoverPath = book.CoverImagePath;
                await PopulateDropdowns(vm.AuthorId, vm.CategoryId);
                return View(vm);
            }

            book.Title = vm.Title;
            book.ISBN = vm.ISBN;
            book.PublishedYear = vm.PublishedYear;
            book.AuthorId = vm.AuthorId;
            book.CategoryId = vm.CategoryId;
            book.TotalCopies = vm.TotalCopies;
            book.AvailableCopies = vm.TotalCopies - borrowed;

            var oldCover = book.CoverImagePath;
            string? newCover = null;

            if (vm.CoverImage != null)
            {
                newCover = await SaveImageAsync(vm.CoverImage);
                book.CoverImagePath = newCover;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                DeleteImage(newCover); // امسح الغلاف الجديد اللي اتحفظ
                ModelState.AddModelError(nameof(vm.ISBN), "A book with this ISBN already exists");
                vm.ExistingCoverPath = oldCover;
                await PopulateDropdowns(vm.AuthorId, vm.CategoryId);
                return View(vm);
            }

            // امسح القديم بعد نجاح الحفظ بس
            if (newCover != null)
                DeleteImage(oldCover);

            TempData["Success"] = "Book updated successfully";
            return RedirectToAction(nameof(Index));
        }

        // ============ 5) Delete ============
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var book = await _context.Books
                .AsNoTracking()
                .Include(b => b.Author)
                .Include(b => b.Category)
                .Include(b => b.Loans)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null) return NotFound();
            return View(book);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .Include(b => b.Loans)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null) return NotFound();

            if (book.Loans.Any())
            {
                ModelState.AddModelError(string.Empty, "Cannot delete a book that has loan history.");
                return View(book);
            }

            var coverPath = book.CoverImagePath;
            _context.Books.Remove(book);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty,
                    "The book could not be deleted because it is referenced by other records.");
                return View(book);
            }

            DeleteImage(coverPath);

            TempData["Success"] = "Book deleted";
            return RedirectToAction(nameof(Index));
        }

        // ============ Helpers ============
        private async Task PopulateDropdowns(int? authorId = null, int? categoryId = null)
        {
            var authors = await _context.Authors.AsNoTracking().OrderBy(a => a.FullName).ToListAsync();
            var categories = await _context.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();

            ViewBag.Authors = new SelectList(authors, "Id", "FullName", authorId);
            ViewBag.Categories = new SelectList(categories, "Id", "Name", categoryId);
        }

        private Task<bool> IsbnExistsAsync(string isbn, int? excludeId = null)
        {
            return _context.Books.AnyAsync(b =>
                b.ISBN == isbn && (excludeId == null || b.Id != excludeId));
        }

        private void ValidateImage(IFormFile? file, string fieldName)
        {
            if (file == null || file.Length == 0) return;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!AllowedExtensions.Contains(extension))
                ModelState.AddModelError(fieldName, "Allowed formats: jpg, jpeg, png, webp");

            if (file.Length > MaxFileSize)
                ModelState.AddModelError(fieldName, "Image size must be less than 2 MB");
        }

        private async Task<string> SaveImageAsync(IFormFile file)
        {
            var folder = Path.Combine(_environment.WebRootPath, "covers");
            Directory.CreateDirectory(folder);

            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName).ToLowerInvariant();
            var fullPath = Path.Combine(folder, fileName);

            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return "/covers/" + fileName;
        }

        private void DeleteImage(string? imagePath)
        {
            if (string.IsNullOrEmpty(imagePath)) return;

            var fullPath = Path.Combine(_environment.WebRootPath, imagePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }
    }
}