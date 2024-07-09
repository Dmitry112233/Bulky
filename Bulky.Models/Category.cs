using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Bulky.Models;

public class Category : IValidatableObject
{
    [Key] 
    public int Id { get; set; }

    [Required]
    [DisplayName("Category Name")]
    [MaxLength(30)]
    //[CompareNameAndOrder("DisplayOrder")]
    public string Name { get; set; }

    [DisplayName("Display Order")]
    [Range(1, 100, ErrorMessage = "*** Display Order must be between 1-100 ***")]
    public int DisplayOrder { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Name.Equals(DisplayOrder.ToString()))
        {
            yield return new ValidationResult($"Category Name and Display Order can not be the same");
        }
    }
}