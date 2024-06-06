using System.ComponentModel.DataAnnotations;

namespace BulkyWeb.Validators;

public class CompareNameAndOrderAttribute : ValidationAttribute
{
    private string DisplayOrder { get; }

    public CompareNameAndOrderAttribute(string displayOrder)
    {
        DisplayOrder = displayOrder;
    }

    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        var displayOrderProperty = validationContext.ObjectType.GetProperty(DisplayOrder);

        if (displayOrderProperty != null && value != null)
        {
            var displayOrderUsed = (int) displayOrderProperty.GetValue(validationContext.ObjectInstance)!;
            
            if (value.ToString().Equals(displayOrderUsed.ToString()))
            {
                return new ValidationResult("Name and Order can not be equal");
            }
        }

        return ValidationResult.Success!;
    }
}