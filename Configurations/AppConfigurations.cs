namespace ThingsBoardPublisher.Configurations;

public class AppConfigurations
{
    private static IConfiguration? _configuration;
    public static void SetConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public static IConfiguration GetConfiguration()
    {
        if (_configuration == null)
        {
            _configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                      .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                                      .AddJsonFile($"appsettings.development.json", optional: true, reloadOnChange: true)
                                      .AddJsonFile($"appsettings.production.json", optional: true, reloadOnChange: true)
                                      .Build();
        }
        return _configuration;
    }

    public static string GetSectionValue(string key)
    {
        return _configuration.GetSection(key)?.Value ?? "";
    }
}
