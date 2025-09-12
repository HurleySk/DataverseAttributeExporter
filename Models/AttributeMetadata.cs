namespace DataverseAttributeExporter.Models;

public class AttributeMetadata
{
    public string EntitySchemaName { get; set; } = string.Empty;
    public string EntityDisplayName { get; set; } = string.Empty;
    public string AttributeSchemaName { get; set; } = string.Empty;
    public string AttributeDisplayName { get; set; } = string.Empty;
    public string AttributeType { get; set; } = string.Empty;
    public string DataverseType { get; set; } = string.Empty;
    public string DataverseFormat { get; set; } = string.Empty;
    public string AttributeDescription { get; set; } = string.Empty;
    public string PicklistValues { get; set; } = string.Empty;
    public string PublisherPrefix { get; set; } = string.Empty;
}
