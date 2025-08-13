using DataverseAttributeExporter.Models;

namespace DataverseAttributeExporter.Services;

public interface IDataverseService
{
    Task<List<EntityAttributeMetadata>> GetAttributeMetadataAsync(string publisherPrefix, bool includeSystemAttributes = false);
}
