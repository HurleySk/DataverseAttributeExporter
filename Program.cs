using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DataverseAttributeExporter.Models;
using DataverseAttributeExporter.Services;

namespace DataverseAttributeExporter;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            // Create host
            var host = CreateHostBuilder(args, configuration).Build();

            // Get services
            using var scope = host.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            var config = scope.ServiceProvider.GetRequiredService<ConfigurationSettings>();

            logger.LogInformation("Starting Dataverse Attribute Exporter");

            // Validate configuration
            if (string.IsNullOrWhiteSpace(config.ConnectionString))
            {
                logger.LogError("ConnectionString is required in configuration");
                return 1;
            }

            // PublisherPrefix can be empty (for OOTB entities) but not null
            if (config.PublisherPrefix == null)
            {
                logger.LogError("PublisherPrefix must be specified in configuration (use empty string for OOTB entities)");
                return 1;
            }

            // Create services
            var dataverseService = new DataverseService(
                scope.ServiceProvider.GetRequiredService<ILogger<DataverseService>>(),
                config.ConnectionString);
            
            var csvExportService = scope.ServiceProvider.GetRequiredService<ICsvExportService>();

            // Connect to Dataverse
            var connected = await dataverseService.ConnectAsync();
            if (!connected)
            {
                logger.LogError("Failed to connect to Dataverse");
                return 1;
            }

            try
            {
                // Extract attribute metadata
                logger.LogInformation("Extracting attribute metadata for publisher prefix: {PublisherPrefix}", config.PublisherPrefix);
                var attributeMetadata = await dataverseService.GetAttributeMetadataAsync(
                    config.PublisherPrefix, 
                    config.IncludeSystemEntities,
                    config.ExcludeOotbAttributes);

                if (attributeMetadata.Count == 0)
                {
                    logger.LogWarning("No attributes found for publisher prefix: {PublisherPrefix}", config.PublisherPrefix);
                    return 0;
                }

                // Export to CSV
                await csvExportService.ExportAsync(attributeMetadata, config.OutputFilePath);

                logger.LogInformation("Export completed successfully. {Count} attributes exported to {FilePath}", 
                    attributeMetadata.Count, config.OutputFilePath);

                return 0;
            }
            finally
            {
                dataverseService.Disconnect();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return 1;
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args, IConfiguration configuration) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Bind configuration
                var config = new ConfigurationSettings();
                configuration.GetSection(ConfigurationSettings.SectionName).Bind(config);
                services.AddSingleton(config);

                // Register services
                services.AddTransient<ICsvExportService, CsvExportService>();

                // Configure logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.AddConfiguration(configuration.GetSection("Logging"));
                });
            });
}
