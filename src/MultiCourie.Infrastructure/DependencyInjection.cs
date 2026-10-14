using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MultiCourie.Application.Couriers;
using MultiCourie.Application.Orders;
using MultiCourie.Infrastructure.Background;
using MultiCourie.Infrastructure.Couriers.UrbaneBolt;
using MultiCourie.Infrastructure.Options;
using MultiCourie.Infrastructure.Persistence;
using Polly;
using Polly.Extensions.Http;
using System.Net;

namespace MultiCourie.Infrastructure;
/// <summary>
/// Provides extension methods for registering infrastructure services and configurations in the dependency injection container.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CourierHttpOptions>(configuration.GetSection(CourierHttpOptions.SectionName));
        services.Configure<UrbaneBoltOptions>(configuration.GetSection(UrbaneBoltOptions.SectionName));
        services.Configure<BulkProcessingOptions>(configuration.GetSection(BulkProcessingOptions.SectionName));

        var connectionString = configuration.GetConnectionString("MultiCourierConnection")
            ?? "Data Source=Multicourie.db";

        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

        services.AddScoped<Application.Abstractions.Persistence.IShipmentOrderRepository, ShipmentOrderRepository>();
        services.AddScoped<Application.Abstractions.Persistence.IBulkBatchRepository, BulkBatchRepository>();

        services.AddSingleton<BulkOrderChannelQueue>();
        services.AddSingleton<IBulkOrderQueue>(sp => sp.GetRequiredService<BulkOrderChannelQueue>());
        services.AddHostedService<BulkBatchProcessingService>();

        services.AddSingleton<UrbaneBoltTokenProvider>();
        services.AddTransient<UrbaneBoltAuthHandler>();
        services.AddSingleton<UrbaneBoltOrderMapper>();

        var urbaneBolt = configuration.GetSection(UrbaneBoltOptions.SectionName).Get<UrbaneBoltOptions>() ?? new UrbaneBoltOptions();
        if (urbaneBolt.Enabled)
        {
            services.AddTransient<ICourierPartner, UrbaneBoltCourierPartner>();
        }

        var httpOptions = configuration.GetSection(CourierHttpOptions.SectionName).Get<CourierHttpOptions>() ?? new CourierHttpOptions();
        ConfigureUrbaneBoltHttpClients(services, urbaneBolt.BaseUrl, httpOptions);

        return services;
    }

    private static void ConfigureUrbaneBoltHttpClients(IServiceCollection services, string baseUrl, CourierHttpOptions httpOptions)
    {

        IAsyncPolicy<HttpResponseMessage> retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => r.StatusCode == HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(
                httpOptions.RetryCount,
                attempt => TimeSpan.FromMilliseconds(httpOptions.RetryBaseDelayMilliseconds * Math.Pow(2, attempt - 1)));

        services.AddHttpClient("UrbaneBoltAuth", client =>
            {
                client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(httpOptions.TimeoutSeconds);
            })
            .AddPolicyHandler(retryPolicy);

        services.AddHttpClient("UrbaneBolt", client =>
            {
                client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(httpOptions.TimeoutSeconds);
            })
            .AddHttpMessageHandler<UrbaneBoltAuthHandler>()
            .AddPolicyHandler(retryPolicy);
    }
}
