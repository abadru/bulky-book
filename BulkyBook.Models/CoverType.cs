using System.ComponentModel.DataAnnotations;

namespace BulkyBook.Models;

public class CoverType
{
    public int Id { get; set; }
    [Required]
    [Display(Name = "Cover Type")]
    [MaxLength(50)]
    public string Name { get; set; }
}