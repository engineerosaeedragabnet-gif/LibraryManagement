# 📚 Library Management System

A library management web application built with **ASP.NET Core MVC**, **Entity Framework Core**, and **SQL Server**. It manages books, authors, members, loans, returns, and late fines, with real business rules enforced on the server inside transactions.

---

## 📸 Screenshots

| Dashboard | Books |
|-----------|-------|
| ![Dashboard](docs/screenshots/dashboard.png) | ![Books](docs/screenshots/books.png) |

| New Loan (rule violation) | Loans |
|---------------------------|-------|
| ![New Loan](docs/screenshots/new-loan.png) | ![Loans](docs/screenshots/loans.png) |

---

## ✨ Features

- **Books**: CRUD, cover image upload (type and size validation), search, filters, sorting, and pagination
- **Authors and Categories**: CRUD with duplicate prevention and delete protection
- **Members**: CRUD, unique email, activate/deactivate instead of deleting members who have history
- **Loans**: borrowing with business rules enforced inside a transaction
- **Returns**: automatic stock update and late fine calculation
- **Concurrency control**: optimistic concurrency using `RowVersion`
- **Dashboard**: statistics, top borrowed books, recent loans, and overdue alerts
- **Custom validation attribute**: `[NotInFuture]`
- **Custom 404 page**

---

## 📏 Business Rules

| Rule | Description |
|------|-------------|
| Active member | Only active members can borrow |
| Availability | The book must have at least one available copy |
| Max loans | A member can have at most 3 active loans |
| Overdue block | A member with an overdue loan cannot borrow |
| No duplicates | A member cannot borrow the same book twice at the same time |
| Loan period | 7, 14, or 21 days |
| Late fine | 5 per full day late, calculated on return |
| Atomicity | Creating the loan and updating stock succeed or fail together |
| Delete protection | Authors, categories, books, and members with related records cannot be deleted |

---

## 🛠️ Tech Stack

| Layer | Technology |
|-------|------------|
| Framework | ASP.NET Core MVC |
| ORM | Entity Framework Core (Code First) |
| Database | SQL Server |
| Frontend | Razor Views, Bootstrap 5, jQuery Validation |
| Tools | Visual Studio, Git, GitHub |

---

## 🧩 Database Design

- **Author** 1 ──── * **Book** * ──── 1 **Category**
- **Member** 1 ──── * **Loan** * ──── 1 **Book**

Delete behavior is `Restrict` on all relationships to protect history.

---

## 🗂️ Project Structure

```
LibraryManagement/
├── Controllers/    # Home, Books, Authors, Categories, Members, Loans
├── Models/         # Author, Category, Book, Member, Loan
├── ViewModels/     # Form, list, and dashboard view models
├── Validation/     # Custom validation attributes
├── Enums/          # LoanStatus
├── Data/           # AppDbContext
├── Views/          # Razor views
├── Migrations/     # EF Core migrations
├── Database/       # seed.sql (sample data)
└── wwwroot/        # Static files and uploaded covers
```

---

## 🚀 Getting Started

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (the version used by the project)
- SQL Server or SQL Server Express
- Visual Studio 2022 or later (or VS Code)

### Installation

1. **Clone the repository**
```bash
   git clone https://github.com/YOUR_USERNAME/LibraryManagement.git
   cd LibraryManagement
```

2. **Update the connection string** in `appsettings.json`
```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=LibraryManagement;Trusted_Connection=True;TrustServerCertificate=True;"
   }
```

3. **Apply the migrations**

   Package Manager Console:
```
   Update-Database
```
   or CLI:
```bash
   dotnet ef database update
```

4. **(Optional) Load sample data**

   Run `Database/seed.sql` in SQL Server Management Studio to load sample authors, books, members, and loans (including overdue and fined loans).

5. **Run the project**
```bash
   dotnet run
```

6. Open the URL shown in the terminal (for example `https://localhost:5001`).

---

## 🧠 What I Learned

- Enforcing business rules on the server and never trusting the client
- Transactions and optimistic concurrency (`RowVersion`)
- Aggregate queries (`Count`, `Sum`, `GroupBy`) for a dashboard
- Delete protection and `DeleteBehavior.Restrict`
- Building queries with `IQueryable` (search, filter, sort, pagination)
- ViewModels, custom validation attributes, and the Post/Redirect/Get pattern

---

## 🔮 Future Improvements

- Authentication and roles with ASP.NET Identity (Admin / Librarian)
- Background job for overdue reminders
- Unit tests for the fine calculation and loan rules
- REST API with JWT authentication

---

## 👤 Author

**Saeed Ragab**
- GitHub: [@YOUR_USERNAME](https://github.com/YOUR_USERNAME)
- LinkedIn: https://www.linkedin.com/in/saeed-ragab-b600a3329/?isSelfProfile=true
---

## 📄 License

This project is open source and available under the [MIT License](LICENSE).
