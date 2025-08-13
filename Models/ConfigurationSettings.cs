namespace DataverseAttributeExporter.Models;

public class ConfigurationSettings
{
    public const string SectionName = "DataverseAttributeExporter";
    
    public string ConnectionString { get; set; } = string.Empty;
    public string PublisherPrefix { get; set; } = string.Empty;
    public string OutputFilePath { get; set; } = "attribute_metadata.csv";
    public bool IncludeSystemEntities { get; set; } = false;
}
