using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SubTrack.EntityFramework;

public static class ServiceCollectionExtensions
{
    public const string ConnectionStringName = "SubTrack";

    public static IServiceCollection AddSubTrackDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");
        }

        return services.AddDbContext<SubTrackDbContext>(options => options.UseNpgsql(connectionString));
    }
}
