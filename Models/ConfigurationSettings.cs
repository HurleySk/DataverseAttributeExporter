namespace DataverseAttributeExporter.Models;

public class ConfigurationSettings
{
    public DataverseSettings DataverseSettings { get; set; } = new();
    public ExportSettings ExportSettings { get; set; } = new();
}

public class DataverseSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string PublisherPrefix { get; set; } = string.Empty;
}

public class ExportSettings
{
    public string OutputPath { get; set; } = string.Empty;
    public bool IncludeSystemAttributes { get; set; }
}
