using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using DataverseAttributeExporter.Models;

namespace DataverseAttributeExporter.Services;

public class CsvExportService : ICsvExportService
{
    private readonly ILogger<CsvExportService> _logger;

    public CsvExportService(ILogger<CsvExportService> logger)
    {
        _logger = logger;
    }

    public async Task ExportAsync(List<Models.AttributeMetadata> attributeMetadata, string filePath)
    {
        try
        {
            _logger.LogInformation("Exporting {Count} attribute metadata records to {FilePath}", attributeMetadata.Count, filePath);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
            };

            using var writer = new StreamWriter(filePath);
            using var csv = new CsvWriter(writer, config);

            // Write headers manually to control the order
            csv.WriteField("Entity Schema Name");
            csv.WriteField("Entity Display Name");
            csv.WriteField("Attribute Schema Name");
            csv.WriteField("Attribute Display Name");
            csv.WriteField("Attribute Type");
            csv.WriteField("Dataverse Format");
            csv.WriteField("Format Details");
            csv.WriteField("Attribute Description");
            csv.WriteField("Publisher Prefix");
            await csv.NextRecordAsync();

            // Write data
            foreach (var metadata in attributeMetadata.OrderBy(x => x.EntitySchemaName).ThenBy(x => x.AttributeSchemaName))
            {
                csv.WriteField(metadata.EntitySchemaName);
                csv.WriteField(metadata.EntityDisplayName);
                csv.WriteField(metadata.AttributeSchemaName);
                csv.WriteField(metadata.AttributeDisplayName);
                csv.WriteField(metadata.AttributeType);
                csv.WriteField(metadata.DataverseFormat);
                csv.WriteField(metadata.FormatDetails);
                csv.WriteField(metadata.AttributeDescription);
                csv.WriteField(metadata.PublisherPrefix);
                await csv.NextRecordAsync();
            }

            await writer.FlushAsync();
            _logger.LogInformation("Successfully exported attribute metadata to {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting attribute metadata to CSV");
            throw;
        }
    }
}
