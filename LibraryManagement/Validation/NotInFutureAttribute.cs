using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Validation
{
    public class NotInFutureAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null) return ValidationResult.Success;

            if (value is DateTime date && date.Date > DateTime.Today)
                return new ValidationResult(ErrorMessage ?? "Date cannot be in the future");

            return ValidationResult.Success;
        }
    }
}