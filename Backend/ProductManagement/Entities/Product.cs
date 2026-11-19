using System.ComponentModel.DataAnnotations;

namespace ProductManagementAPI.Entities
{
    public class Product
    {
        public int Id { get; set; } // Auto-generated 6-digit sequence (100000 - 999999)

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative.")]
        public int Stock { get; set; }
    }
}
