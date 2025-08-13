using DataverseAttributeExporter.Models;
using DataverseAttributeExporter.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace DataverseAttributeExporter;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            // Build configuration
            var basePath = GetApplicationBasePath();
            Console.WriteLine($"Looking for configuration files in: {basePath}");
            
            var configFilePath = Path.Combine(basePath, "appsettings.json");
            if (!File.Exists(configFilePath))
            {
                Console.WriteLine($"Configuration file not found at: {configFilePath}");
                Console.WriteLine("Please ensure appsettings.json exists in the application directory.");
                return;
            }
            
            Console.WriteLine($"Configuration file found at: {configFilePath}");
            
            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Setup dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services, configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Get logger
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Starting Dataverse Attribute Exporter");

            // Get configuration
            var settings = configuration.GetSection("ConfigurationSettings").Get<ConfigurationSettings>();
            if (settings == null)
            {
                logger.LogError("Failed to load configuration");
                return;
            }

            // Validate connection string
            if (string.IsNullOrEmpty(settings.DataverseSettings.ConnectionString))
            {
                logger.LogError("Connection string is not configured. Please update appsettings.json");
                return;
            }

            if (string.IsNullOrEmpty(settings.DataverseSettings.PublisherPrefix))
            {
                logger.LogError("Publisher prefix is not configured. Please update appsettings.json");
                return;
            }

            // Get services
            var dataverseService = serviceProvider.GetRequiredService<IDataverseService>();
            var csvExportService = serviceProvider.GetRequiredService<ICsvExportService>();

            // Retrieve attribute metadata
            logger.LogInformation("Retrieving attribute metadata for publisher prefix: {PublisherPrefix}", 
                settings.DataverseSettings.PublisherPrefix);

            var attributes = await dataverseService.GetAttributeMetadataAsync(
                settings.DataverseSettings.PublisherPrefix,
                settings.ExportSettings.IncludeSystemAttributes);

            if (attributes.Count == 0)
            {
                logger.LogWarning("No attributes found for publisher prefix: {PublisherPrefix}", 
                    settings.DataverseSettings.PublisherPrefix);
                return;
            }

            // Export to CSV
            logger.LogInformation("Exporting {AttributeCount} attributes to CSV", attributes.Count);
            await csvExportService.ExportToCsvAsync(attributes, settings.ExportSettings.OutputPath);

            logger.LogInformation("Export completed successfully. Output file: {OutputPath}", 
                settings.ExportSettings.OutputPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        }
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Add configuration
        services.Configure<ConfigurationSettings>(configuration.GetSection("ConfigurationSettings"));

        // Add Dataverse client
        var connectionString = configuration["DataverseSettings:ConnectionString"];
        if (!string.IsNullOrEmpty(connectionString))
        {
            var serviceClient = new ServiceClient(connectionString);
            if (serviceClient.IsReady)
            {
                services.AddSingleton(serviceClient);
            }
            else
            {
                throw new InvalidOperationException("Failed to connect to Dataverse. Please check your connection string.");
            }
        }

        // Add services
        services.AddScoped<IDataverseService, DataverseService>();
        services.AddScoped<ICsvExportService, CsvExportService>();
    }

    private static string GetApplicationBasePath()
    {
        // Try to get the directory where the executable is located
        var executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        if (!string.IsNullOrEmpty(executablePath))
        {
            var executableDir = Path.GetDirectoryName(executablePath);
            if (!string.IsNullOrEmpty(executableDir))
            {
                // If we're in a build output directory, try to go up to the project root
                if (executableDir.Contains("bin") || executableDir.Contains("obj"))
                {
                    var projectRoot = FindProjectRoot(executableDir);
                    if (!string.IsNullOrEmpty(projectRoot))
                    {
                        return projectRoot;
                    }
                }
                return executableDir;
            }
        }

        // Fallback to current directory
        return Directory.GetCurrentDirectory();
    }

    private static string? FindProjectRoot(string startPath)
    {
        var current = startPath;
        while (!string.IsNullOrEmpty(current))
        {
            // Look for project file (.csproj) or appsettings.json
            if (File.Exists(Path.Combine(current, "appsettings.json")) ||
                Directory.GetFiles(current, "*.csproj").Any())
            {
                return current;
            }
            
            var parent = Directory.GetParent(current);
            if (parent == null)
                break;
                
            current = parent.FullName;
        }
        return null;
    }
}
