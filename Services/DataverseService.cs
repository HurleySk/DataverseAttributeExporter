using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using DataverseAttributeExporter.Models;

namespace DataverseAttributeExporter.Services;

public class DataverseService : IDataverseService
{
    private readonly ILogger<DataverseService> _logger;
    private readonly string _connectionString;
    private ServiceClient? _serviceClient;

    public DataverseService(ILogger<DataverseService> logger, string connectionString)
    {
        _logger = logger;
        _connectionString = connectionString;
    }

    public async Task<bool> ConnectAsync()
    {
        try
        {
            _logger.LogInformation("Connecting to Dataverse...");
            _serviceClient = new ServiceClient(_connectionString);
            
            if (!_serviceClient.IsReady)
            {
                _logger.LogError("Failed to connect to Dataverse. Connection details: {LastError}", _serviceClient.LastError);
                return false;
            }

            _logger.LogInformation("Successfully connected to Dataverse environment: {OrgDetail}", _serviceClient.ConnectedOrgFriendlyName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to Dataverse");
            return false;
        }
    }

    public async Task<List<Models.AttributeMetadata>> GetAttributeMetadataAsync(string publisherPrefix, bool includeSystemEntities = false)
    {
        if (_serviceClient == null || !_serviceClient.IsReady)
        {
            throw new InvalidOperationException("Service client is not connected. Call ConnectAsync first.");
        }

        var attributeMetadataList = new List<Models.AttributeMetadata>();

        try
        {
            _logger.LogInformation("Retrieving entity metadata for publisher prefix: {PublisherPrefix}", publisherPrefix);

            // Get all entity metadata
            var retrieveAllEntitiesRequest = new RetrieveAllEntitiesRequest
            {
                EntityFilters = EntityFilters.Entity | EntityFilters.Attributes,
                RetrieveAsIfPublished = true
            };

            var response = (RetrieveAllEntitiesResponse)await _serviceClient.ExecuteAsync(retrieveAllEntitiesRequest);
            var entityMetadataCollection = response.EntityMetadata;

            _logger.LogInformation("Found {EntityCount} entities", entityMetadataCollection.Length);

            foreach (var entityMetadata in entityMetadataCollection)
            {
                // Skip system entities if not requested
                if (!includeSystemEntities && (entityMetadata.IsCustomEntity == false))
                {
                    continue;
                }

                // Check if entity has the specified publisher prefix or if we want all entities for this publisher
                var entitySchemaName = entityMetadata.SchemaName;
                if (!string.IsNullOrEmpty(publisherPrefix))
                {
                    var hasCustomAttributes = entityMetadata.Attributes?.Any(attr => 
                        attr.SchemaName?.StartsWith(publisherPrefix + "_", StringComparison.OrdinalIgnoreCase) == true) ?? false;
                    
                    var isCustomEntity = entitySchemaName?.StartsWith(publisherPrefix + "_", StringComparison.OrdinalIgnoreCase) == true;
                    
                    if (!hasCustomAttributes && !isCustomEntity)
                    {
                        continue;
                    }
                }

                var entityDisplayName = entityMetadata.DisplayName?.UserLocalizedLabel?.Label ?? entitySchemaName ?? "Unknown";

                _logger.LogDebug("Processing entity: {EntitySchemaName} ({EntityDisplayName})", entitySchemaName, entityDisplayName);

                if (entityMetadata.Attributes != null)
                {
                    foreach (var attributeMetadata in entityMetadata.Attributes)
                    {
                        var attributeSchemaName = attributeMetadata.SchemaName;
                        
                        // If publisher prefix is specified, filter attributes
                        if (!string.IsNullOrEmpty(publisherPrefix))
                        {
                            // Include attributes that match the publisher prefix or system attributes on matching entities
                            var isCustomAttribute = attributeSchemaName?.StartsWith(publisherPrefix + "_", StringComparison.OrdinalIgnoreCase) == true;
                            var isEntityMatch = entitySchemaName?.StartsWith(publisherPrefix + "_", StringComparison.OrdinalIgnoreCase) == true;
                            
                            if (!isCustomAttribute && !isEntityMatch)
                            {
                                continue;
                            }
                        }

                        var attributeDisplayName = attributeMetadata.DisplayName?.UserLocalizedLabel?.Label ?? attributeSchemaName ?? "Unknown";
                        var attributeType = attributeMetadata.AttributeType?.ToString() ?? "Unknown";
                        var attributeDescription = attributeMetadata.Description?.UserLocalizedLabel?.Label ?? string.Empty;

                        var metadata = new Models.AttributeMetadata
                        {
                            EntitySchemaName = entitySchemaName ?? string.Empty,
                            EntityDisplayName = entityDisplayName,
                            AttributeSchemaName = attributeSchemaName ?? string.Empty,
                            AttributeDisplayName = attributeDisplayName,
                            AttributeType = attributeType,
                            AttributeDescription = attributeDescription,
                            PublisherPrefix = publisherPrefix
                        };

                        attributeMetadataList.Add(metadata);
                    }
                }
            }

            _logger.LogInformation("Retrieved {AttributeCount} attributes from {EntityCount} entities", 
                attributeMetadataList.Count, 
                attributeMetadataList.Select(a => a.EntitySchemaName).Distinct().Count());

            return attributeMetadataList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attribute metadata");
            throw;
        }
    }

    public void Disconnect()
    {
        if (_serviceClient != null)
        {
            _logger.LogInformation("Disconnecting from Dataverse...");
            _serviceClient.Dispose();
            _serviceClient = null;
        }
    }

    public void Dispose()
    {
        Disconnect();
    }
}
