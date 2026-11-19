using System.ComponentModel.DataAnnotations;

namespace ProductManagementAPI.DTOs;

public record CreateProductDto(
    [Required, StringLength(100, MinimumLength = 1)] string Name,
    [MaxLength(500)] string? Description,
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")] decimal Price,
    [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative.")] int Stock
);

public record UpdateProductDto(
    [Required, StringLength(100, MinimumLength = 1)] string Name,
    [MaxLength(500)] string? Description,
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")] decimal Price,
    [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative.")] int Stock
);

public record ProductDto(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    int Stock
);
