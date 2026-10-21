using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Categories
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new CategoryListItemViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    BooksCount = c.Books.Count
                })
                .ToListAsync();

            return View(categories);
        }

        // GET: Categories/Create
        public IActionResult Create() => View(new CategoryFormViewModel());

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryFormViewModel model)
        {
            model.Name = model.Name?.Trim() ?? string.Empty;

            if (ModelState.IsValid && await NameExistsAsync(model.Name))
                ModelState.AddModelError(nameof(model.Name), "A category with this name already exists");

            if (!ModelState.IsValid) return View(model);

            _context.Categories.Add(new Category { Name = model.Name });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // سباق (Race Condition): الـ Unique Index منع التكرار
                ModelState.AddModelError(nameof(model.Name), "A category with this name already exists");
                return View(model);
            }

            TempData["Success"] = "Category created successfully";
            return RedirectToAction(nameof(Index));
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            return View(new CategoryFormViewModel { Id = category.Id, Name = category.Name });
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoryFormViewModel model)
        {
            if (id != model.Id) return NotFound();

            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            model.Name = model.Name?.Trim() ?? string.Empty;

            if (ModelState.IsValid && await NameExistsAsync(model.Name, excludeId: id))
                ModelState.AddModelError(nameof(model.Name), "A category with this name already exists");

            if (!ModelState.IsValid) return View(model);

            category.Name = model.Name;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(nameof(model.Name), "A category with this name already exists");
                return View(model);
            }

            TempData["Success"] = "Category updated successfully";
            return RedirectToAction(nameof(Index)); category.Name = model.Name;

        }

        // GET: Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories
                .AsNoTracking()
                .Include(c => c.Books)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound();
            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Books)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound();

            if (category.Books.Any())
            {
                ModelState.AddModelError(string.Empty,
                    "Cannot delete a category that has books. Move or delete its books first.");
                return View(category);
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Category deleted";
            return RedirectToAction(nameof(Index));
        }

        private Task<bool> NameExistsAsync(string name, int? excludeId = null)
        {
            return _context.Categories.AnyAsync(c =>
                c.Name == name && (excludeId == null || c.Id != excludeId));
        }
    }
}