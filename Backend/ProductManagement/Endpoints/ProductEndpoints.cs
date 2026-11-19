using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductManagementAPI.Data;
using ProductManagementAPI.DTOs;
using ProductManagementAPI.Entities;

namespace ProductManagementAPI.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products").WithTags("Products");

        // GET /api/products
        group.MapGet("/", async (AppDbContext db) =>
            await db.Products.AsNoTracking()
                .Select(p => new ProductDto(p.Id, p.Name, p.Description, p.Price, p.Stock))
                .ToListAsync())
            .WithSummary("Get all products")
            .WithDescription("Retrieves a list of all products, including their unique 6-digit ID and available stock.")
            .Produces<List<ProductDto>>(StatusCodes.Status200OK);

        // GET /api/products/{id}
        group.MapGet("/{id:int}", async (int id, AppDbContext db) =>
            await db.Products.AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new ProductDto(p.Id, p.Name, p.Description, p.Price, p.Stock))
                .FirstOrDefaultAsync() is { } product
                    ? Results.Ok(product)
                    : Results.NotFound())
            .WithSummary("Get product by ID")
            .WithDescription("Retrieves a single product using its unique 6-digit identifier.")
            .Produces<ProductDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // POST /api/products
        group.MapPost("/", async (CreateProductDto dto, AppDbContext db) =>
        {
            var product = new Product
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                Price = dto.Price,
                Stock = dto.Stock
            };

            db.Products.Add(product);
            await db.SaveChangesAsync();

            var response = new ProductDto(product.Id, product.Name, product.Description, product.Price, product.Stock);
            return Results.Created($"/api/products/{product.Id}", response);
        })
        .WithSummary("Create a new product")
        .WithDescription("Creates a new product and auto-generates a unique 6-digit ID (100000–999999).")
        .Produces<ProductDto>(StatusCodes.Status201Created)
        .ProducesValidationProblem();

        // PUT /api/products/{id}
        group.MapPut("/{id:int}", async (int id, UpdateProductDto dto, AppDbContext db) =>
        {
            var product = await db.Products.FindAsync(id);
            if (product is null) return Results.NotFound();

            product.Name = dto.Name.Trim();
            product.Description = dto.Description?.Trim();
            product.Price = dto.Price;
            product.Stock = dto.Stock;

            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithSummary("Update an existing product")
        .WithDescription("Updates all fields of an existing product identified by its 6-digit ID.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesValidationProblem();

        // DELETE /api/products/{id}
        group.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
        {
            var deleted = await db.Products.Where(p => p.Id == id).ExecuteDeleteAsync();
            return deleted > 0 ? Results.NoContent() : Results.NotFound();
        })
        .WithSummary("Delete a product")
        .WithDescription("Deletes a product by its 6-digit ID.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        // POST /api/products/{id}/decrement-stock/{quantity}
        group.MapPost("/{id:int}/decrement-stock/{quantity:int}", async (int id, int quantity, AppDbContext db) =>
        {
            if (quantity <= 0) return Results.BadRequest("Quantity must be greater than zero.");

            var updated = await db.Products
                .Where(p => p.Id == id && p.Stock >= quantity)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - quantity));

            return updated > 0 ? Results.Ok() : Results.BadRequest("Product not found or insufficient stock.");
        })
        .WithSummary("Decrement product stock")
        .WithDescription("Atomically decrements available stock by the specified quantity if sufficient stock exists.")
        .Produces(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status400BadRequest);

        // POST /api/products/{id}/add-to-stock/{quantity}
        group.MapPost("/{id:int}/add-to-stock/{quantity:int}", async (int id, int quantity, AppDbContext db) =>
        {
            if (quantity <= 0) return Results.BadRequest("Quantity must be greater than zero.");

            var updated = await db.Products
                .Where(p => p.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock + quantity));

            return updated > 0 ? Results.Ok() : Results.NotFound();
        })
        .WithSummary("Add to product stock")
        .WithDescription("Atomically increments available stock by the specified quantity.")
        .Produces(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        // GET /api/products/search?name={name}
        group.MapGet("/search", async ([FromQuery] string? name, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(name)) return Results.BadRequest("Name query parameter is required.");

            var results = await db.Products.AsNoTracking()
                .Where(p => EF.Functions.Like(p.Name, $"%{name.Trim()}%"))
                .Select(p => new ProductDto(p.Id, p.Name, p.Description, p.Price, p.Stock))
                .ToListAsync();

            return Results.Ok(results);
        })
        .WithSummary("Search products by name")
        .WithDescription("Returns products matching the given name query (supports partial, case-insensitive matches).")
        .Produces<List<ProductDto>>(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status400BadRequest);

        // GET /api/products/stock-level?min={min}&max={max}
        group.MapGet("/stock-level", async ([FromQuery] int min, [FromQuery] int max, AppDbContext db) =>
        {
            if (min < 0 || max < min) return Results.BadRequest("Invalid min/max stock range.");

            var results = await db.Products.AsNoTracking()
                .Where(p => p.Stock >= min && p.Stock <= max)
                .Select(p => new ProductDto(p.Id, p.Name, p.Description, p.Price, p.Stock))
                .ToListAsync();

            return Results.Ok(results);
        })
        .WithSummary("Filter products by stock range")
        .WithDescription("Returns products whose available stock falls within the inclusive [min, max] range.")
        .Produces<List<ProductDto>>(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status400BadRequest);
    }
}