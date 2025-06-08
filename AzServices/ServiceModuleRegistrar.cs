using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AzServices;

public static class ServiceModuleRegistrar
{
    private static IConfigurationRoot? config = null;

    public static IServiceCollection AddConfigurationModule(this IServiceCollection services, string fileName)
    {
        config = new ConfigurationBuilder()
            .AddJsonFile(fileName, optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build()
            ?? throw new ArgumentException("Cannot bind the Root configuration section.");

        AddServiceBus(services);

        return services; 
    }

    private static void AddServiceBus(IServiceCollection services)
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
}
