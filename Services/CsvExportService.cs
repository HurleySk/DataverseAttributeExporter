using CsvHelper;
using CsvHelper.Configuration;
using DataverseAttributeExporter.Models;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace DataverseAttributeExporter.Services;

public class CsvExportService : ICsvExportService
{
    private readonly ILogger<CsvExportService> _logger;

    public CsvExportService(ILogger<CsvExportService> logger)
    {
        _logger = logger;
    }

    public async Task ExportToCsvAsync(List<EntityAttributeMetadata> attributes, string filePath)
    {
        try
        {
            _logger.LogInformation("Exporting {AttributeCount} attributes to CSV file: {FilePath}", 
                attributes.Count, filePath);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ",",
                Quote = '"',
                Escape = '"'
            };

            using var writer = new StreamWriter(filePath);
            using var csv = new CsvWriter(writer, config);

            // Write headers
            csv.WriteField("Table Schema Name");
            csv.WriteField("Table Display Name");
            csv.WriteField("Attribute Schema Name");
            csv.WriteField("Attribute Display Name");
            csv.WriteField("Attribute Type");
            csv.WriteField("Attribute Description");
            csv.NextRecord();

            // Write data
            foreach (var attribute in attributes)
            {
                csv.WriteField(attribute.TableSchemaName);
                csv.WriteField(attribute.TableDisplayName);
                csv.WriteField(attribute.AttributeSchemaName);
                csv.WriteField(attribute.AttributeDisplayName);
                csv.WriteField(attribute.AttributeType);
                csv.WriteField(attribute.AttributeDescription);
                csv.NextRecord();
            }

            await writer.FlushAsync();
            _logger.LogInformation("Successfully exported {AttributeCount} attributes to {FilePath}", 
                attributes.Count, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting to CSV file: {FilePath}", filePath);
            throw;
        }
    }
}
