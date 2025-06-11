using AzServices.Entities;
using AzServices.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace AzServices.Extensions;

public static class ServiceModuleRegistrar
{
    private static IConfigurationRoot? config = null;

    public static IServiceCollection AddConfigurationModule(this IServiceCollection services, string fileName, bool forApis = true)
    {
        config = new ConfigurationBuilder()
            .AddJsonFile(fileName, optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build()
            ?? throw new ArgumentException("Cannot bind the Root configuration section.");

        if (forApis)
        {
            AddServiceBusService(services);
        }
        else
        {
            AddBookingsService(services);
        }

        return services;
    }

    private static void AddServiceBusService(IServiceCollection services)
    {
        var azureSection = config!.GetSection("Azure")
            ?? throw new ArgumentException("Cannot bind the Azure configuration section.");

        AzureSettings azureSettings = new();
        azureSection.Bind(azureSettings);

        var error = azureSettings switch
        {
            { ServiceBusCnString: null or "" } => $"Service Bus connection string is required",
            { SqlCnString: null or "" } => $"SQL connection string is required",
            _ => null
        };

        if (error != null)
        {
            throw new ArgumentException($"Azure: {error}");
        }

        services.Configure<AzureSettings>(options =>
        {
            options.ServiceBusCnString = azureSettings.ServiceBusCnString!;
            options.SqlCnString = azureSettings.SqlCnString!;
        });

        services.AddScoped<IServiceBusService, ServiceBusService>();
    }

    public static void AddBookingsService(IServiceCollection services)
    {
        var apisSection = config!.GetSection("Apis")
            ?? throw new ArgumentException("Cannot bind the Apis configuration section.");

        ApisSettings apisSettings = new();
        apisSection.Bind(apisSettings);

        var error = apisSettings switch
        {
            { BookingsBaseUrl: null or "" } => $"The base URL of the Bookins API is required",
            _ => null
        };

        if (error != null)
        {
            throw new ArgumentException($"Apis: {error}");
        }

        services.AddScoped<IRestService, RestService>();
        services.Configure<ApisSettings>(options =>
        {
            options.BookingsBaseUrl = options.BookingsBaseUrl;
        });
        services.AddHttpClient<IRestService, RestService>(
        client =>
        {
            client.BaseAddress = new Uri(apisSettings.BookingsBaseUrl!);
        });
    }
}
