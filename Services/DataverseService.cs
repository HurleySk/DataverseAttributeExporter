using DataverseAttributeExporter.Models;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Extensions.Logging;

namespace DataverseAttributeExporter.Services;

public class DataverseService : IDataverseService
{
    private readonly ILogger<DataverseService> _logger;
    private readonly ServiceClient _serviceClient;

    public DataverseService(ILogger<DataverseService> logger, ServiceClient serviceClient)
    {
        _logger = logger;
        _serviceClient = serviceClient;
    }

    public async Task<List<EntityAttributeMetadata>> GetAttributeMetadataAsync(string publisherPrefix, bool includeSystemAttributes = false)
    {
        var attributes = new List<EntityAttributeMetadata>();

        try
        {
            _logger.LogInformation("Retrieving entity metadata for publisher prefix: {PublisherPrefix}", publisherPrefix);

            // Get all entities
            var retrieveAllEntitiesRequest = new RetrieveAllEntitiesRequest
            {
                EntityFilters = EntityFilters.Entity | EntityFilters.Attributes,
                RetrieveAsIfPublished = true
            };

            var retrieveAllEntitiesResponse = await Task.Run(() => 
                (RetrieveAllEntitiesResponse)_serviceClient.Execute(retrieveAllEntitiesRequest));

            // Filter entities by publisher prefix
            var filteredEntities = retrieveAllEntitiesResponse.EntityMetadata
                .Where(e => e.LogicalName != null && e.LogicalName.StartsWith(publisherPrefix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            _logger.LogInformation("Found {EntityCount} entities with publisher prefix {PublisherPrefix}", 
                filteredEntities.Count, publisherPrefix);

            foreach (var entity in filteredEntities)
            {
                if (entity.Attributes == null) continue;

                foreach (var attribute in entity.Attributes)
                {
                    // Skip system attributes if not requested
                    if (!includeSystemAttributes && IsSystemAttribute(attribute))
                        continue;

                    var attributeMetadata = new EntityAttributeMetadata
                    {
                        TableSchemaName = entity.LogicalName ?? string.Empty,
                        TableDisplayName = GetLocalizedLabel(entity.DisplayName) ?? string.Empty,
                        AttributeSchemaName = attribute.LogicalName ?? string.Empty,
                        AttributeDisplayName = GetLocalizedLabel(attribute.DisplayName) ?? string.Empty,
                        AttributeType = attribute.AttributeType.ToString() ?? string.Empty,
                        AttributeDescription = GetLocalizedLabel(attribute.Description) ?? string.Empty
                    };

                    attributes.Add(attributeMetadata);
                }
            }

            _logger.LogInformation("Retrieved {AttributeCount} attributes from {EntityCount} entities", 
                attributes.Count, filteredEntities.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attribute metadata");
            throw;
        }

        return attributes;
    }

    private static string GetLocalizedLabel(Label? label)
    {
        if (label?.UserLocalizedLabel?.Label == null)
            return string.Empty;

        return label.UserLocalizedLabel.Label;
    }

    private static bool IsSystemAttribute(AttributeMetadata attribute)
    {
        // Common system attribute names
        var systemAttributeNames = new[]
        {
            "createdon", "createdby", "modifiedon", "modifiedby",
            "ownerid", "statecode", "statuscode", "versionnumber",
            "importsequencenumber", "overriddencreatedon", "timezoneruleversionnumber",
            "utcconversiontimezonecode", "createdonbehalfby", "modifiedonbehalfby"
        };

        return systemAttributeNames.Contains(attribute.LogicalName.ToLowerInvariant());
    }
}
