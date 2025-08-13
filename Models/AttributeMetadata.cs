namespace DataverseAttributeExporter.Models;

public class EntityAttributeMetadata
{
    public string TableSchemaName { get; set; } = string.Empty;
    public string TableDisplayName { get; set; } = string.Empty;
    public string AttributeSchemaName { get; set; } = string.Empty;
    public string AttributeDisplayName { get; set; } = string.Empty;
    public string AttributeType { get; set; } = string.Empty;
    public string AttributeDescription { get; set; } = string.Empty;
}
