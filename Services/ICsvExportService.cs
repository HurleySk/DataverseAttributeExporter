using DataverseAttributeExporter.Models;

namespace DataverseAttributeExporter.Services;

public interface ICsvExportService
{
    Task ExportAsync(List<Models.AttributeMetadata> attributeMetadata, string filePath);
}
