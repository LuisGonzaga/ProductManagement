using System.Net;
using System.Net.Http.Json;
using ProductManagementAPI.DTOs;

namespace ProductManagementAPI.Tests;

public class ProductEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateProduct_GeneratesUnique6DigitIds_AcrossConcurrentRequests()
    {
        var tasks = Enumerable.Range(1, 10).Select(i =>
            _client.PostAsJsonAsync("/api/products",
                new CreateProductDto($"Concurrent Product {i}", "Desc", 19.99m, 10)));

        var responses = await Task.WhenAll(tasks);
        var products = new List<ProductDto>();

        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var product = await response.Content.ReadFromJsonAsync<ProductDto>();
            Assert.NotNull(product);
            Assert.InRange(product.Id, 100000, 999999);
            products.Add(product);
        }

        // Guarantee zero duplicate IDs under concurrent load
        Assert.Equal(10, products.Select(p => p.Id).Distinct().Count());
    }

    [Fact]
    public async Task CreateProduct_WithInvalidData_ReturnsBadRequest()
    {
        var invalidDto = new CreateProductDto("", "Desc", -10m, -5);

        var response = await _client.PostAsJsonAsync("/api/products", invalidDto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task StockEndpoints_IncrementAndDecrement_EnforcesNonNegativeStock()
    {
        var createRes = await _client.PostAsJsonAsync("/api/products",
            new CreateProductDto("Stock Test Item", null, 50m, 10));
        var created = await createRes.Content.ReadFromJsonAsync<ProductDto>();

        // Add 5 -> stock becomes 15
        var addRes = await _client.PostAsync($"/api/products/{created!.Id}/add-to-stock/5", null);
        Assert.Equal(HttpStatusCode.OK, addRes.StatusCode);

        // Decrement 12 -> stock becomes 3
        var decRes = await _client.PostAsync($"/api/products/{created.Id}/decrement-stock/12", null);
        Assert.Equal(HttpStatusCode.OK, decRes.StatusCode);

        // Attempt to decrement 10 when only 3 remain -> must fail
        var overDecRes = await _client.PostAsync($"/api/products/{created.Id}/decrement-stock/10", null);
        Assert.Equal(HttpStatusCode.BadRequest, overDecRes.StatusCode);

        // Verify final stock is 3
        var fetched = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{created.Id}");
        Assert.Equal(3, fetched!.Stock);
    }

    [Fact]
    public async Task SearchAndStockLevel_FiltersAccurately()
    {
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductDto("UltraWide Monitor", null, 499m, 42));

        var searchResults = await _client.GetFromJsonAsync<List<ProductDto>>("/api/products/search?name=ultrawide");
        Assert.Contains(searchResults!, p => p.Name == "UltraWide Monitor");

        var rangeResults = await _client.GetFromJsonAsync<List<ProductDto>>("/api/products/stock-level?min=40&max=45");
        Assert.Contains(rangeResults!, p => p.Name == "UltraWide Monitor");
    }
}