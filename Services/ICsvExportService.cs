using DataverseAttributeExporter.Models;

namespace DataverseAttributeExporter.Services;

public interface ICsvExportService
{
    Task ExportToCsvAsync(List<EntityAttributeMetadata> attributes, string filePath);
}
