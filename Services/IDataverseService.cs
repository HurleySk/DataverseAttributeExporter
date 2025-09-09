using DataverseAttributeExporter.Models;

namespace DataverseAttributeExporter.Services;

public interface IDataverseService
{
    Task<bool> ConnectAsync();
    Task<List<Models.AttributeMetadata>> GetAttributeMetadataAsync(string publisherPrefix, bool includeSystemEntities = false, bool excludeOotbAttributes = true);
    void Disconnect();
}
