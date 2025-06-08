namespace AzServices;

public class AzureSettings
{
    public string ConfigurationKey = "Azure";
    public string ServiceBusCnString { get; set; } = default!;
    public string SqlCnString { get; set; } = default!;
}
