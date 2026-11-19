using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProductManagementAPI.Data;

namespace ProductManagementAPI.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=ProductManagementTestDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(TestConnectionString));

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.Migrate();
        });
    }
}