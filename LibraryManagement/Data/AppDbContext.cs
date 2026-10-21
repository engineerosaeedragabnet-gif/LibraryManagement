using LibraryManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Author> Authors { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<Loan> Loans { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---------- Delete Behavior: Restrict (حماية التاريخ) ----------
            modelBuilder.Entity<Book>()
                .HasOne(b => b.Author)
                .WithMany(a => a.Books)
                .HasForeignKey(b => b.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Book>()
                .HasOne(b => b.Category)
                .WithMany(c => c.Books)
                .HasForeignKey(b => b.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Loan>()
                .HasOne(l => l.Book)
                .WithMany(b => b.Loans)
                .HasForeignKey(l => l.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Loan>()
                .HasOne(l => l.Member)
                .WithMany(m => m.Loans)
                .HasForeignKey(l => l.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---------- Seed Data ----------
            modelBuilder.Entity<Author>().HasData(
                new Author { Id = 1, FullName = "Naguib Mahfouz", Bio = "Egyptian Nobel laureate novelist." },
                new Author { Id = 2, FullName = "George Orwell", Bio = "English novelist and essayist." },
                new Author { Id = 3, FullName = "Robert C. Martin", Bio = "Software engineer and author of Clean Code." }
            );

            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Fiction" },
                new Category { Id = 2, Name = "Programming" },
                new Category { Id = 3, Name = "Science" },
                new Category { Id = 4, Name = "History" }
            );

            modelBuilder.Entity<Book>().HasData(
                new Book
                {
                    Id = 1,
                    Title = "1984",
                    ISBN = "9780451524935",
                    PublishedYear = 1949,
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    AuthorId = 2,
                    CategoryId = 1
                },
                new Book
                {
                    Id = 2,
                    Title = "Clean Code",
                    ISBN = "9780132350884",
                    PublishedYear = 2008,
                    TotalCopies = 2,
                    AvailableCopies = 2,
                    AuthorId = 3,
                    CategoryId = 2
                },
                new Book
                {
                    Id = 3,
                    Title = "Children of the Alley",
                    ISBN = "9780385264631",
                    PublishedYear = 1959,
                    TotalCopies = 2,
                    AvailableCopies = 2,
                    AuthorId = 1,
                    CategoryId = 1
                }
            );
        }
    }
}