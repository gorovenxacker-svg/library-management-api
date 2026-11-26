using Library.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Library.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLibraryInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Library")
            ?? "Data Source=library.db";

        services.AddDbContext<LibraryDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<ICatalogRepository, EfCatalogRepository>();

        return services;
    }
}
