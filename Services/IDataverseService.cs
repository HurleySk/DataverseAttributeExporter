using DataverseAttributeExporter.Models;

namespace DataverseAttributeExporter.Services;

public interface IDataverseService
{
    Task<bool> ConnectAsync();
    Task<List<Models.AttributeMetadata>> GetAttributeMetadataAsync(string publisherPrefix, bool includeSystemEntities = false);
    void Disconnect();
}
