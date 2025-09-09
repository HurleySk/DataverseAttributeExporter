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

    public async Task<List<Models.AttributeMetadata>> GetAttributeMetadataAsync(string publisherPrefix, bool includeSystemEntities = false, bool excludeOotbAttributes = true)
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
                var entitySchemaName = entityMetadata.SchemaName;
                bool includeEntity = false;

                // Determine if we should include this entity
                if (string.IsNullOrEmpty(publisherPrefix))
                {
                    // Blank prefix means OOTB entities only (no prefixed entities)
                    includeEntity = entityMetadata.IsCustomEntity == false;
                }
                else
                {
                    // Non-blank prefix means entities with that specific prefix only
                    includeEntity = entitySchemaName?.StartsWith(publisherPrefix + "_", StringComparison.OrdinalIgnoreCase) == true;
                }

                // Apply system entities filter
                if (!includeSystemEntities && entityMetadata.IsCustomEntity == false)
                {
                    includeEntity = false;
                }

                if (!includeEntity)
                {
                    continue;
                }

                var entityDisplayName = entityMetadata.DisplayName?.UserLocalizedLabel?.Label ?? entitySchemaName ?? "Unknown";

                _logger.LogDebug("Processing entity: {EntitySchemaName} ({EntityDisplayName})", entitySchemaName, entityDisplayName);

                if (entityMetadata.Attributes != null)
                {
                    foreach (var attributeMetadata in entityMetadata.Attributes)
                    {
                        var attributeSchemaName = attributeMetadata.SchemaName;
                        bool includeAttribute = true;

                        // Apply attribute filtering based on excludeOotbAttributes setting
                        if (excludeOotbAttributes)
                        {
                            if (string.IsNullOrEmpty(publisherPrefix))
                            {
                                // For blank prefix (OOTB entities), exclude prefixed attributes
                                includeAttribute = !HasPrefix(attributeSchemaName);
                            }
                            else
                            {
                                // For specific prefix, only include attributes with that prefix
                                includeAttribute = attributeSchemaName?.StartsWith(publisherPrefix + "_", StringComparison.OrdinalIgnoreCase) == true;
                            }
                        }
                        // If excludeOotbAttributes is false, include all attributes from the included entities

                        if (!includeAttribute)
                        {
                            continue;
                        }

                        var attributeDisplayName = attributeMetadata.DisplayName?.UserLocalizedLabel?.Label ?? attributeSchemaName ?? "Unknown";
                        var attributeType = attributeMetadata.AttributeType?.ToString() ?? "Unknown";
                        var attributeDescription = attributeMetadata.Description?.UserLocalizedLabel?.Label ?? string.Empty;
                        
                        var (dataverseFormat, formatDetails) = GetDataverseFormatInfo(attributeMetadata);

                        var metadata = new Models.AttributeMetadata
                        {
                            EntitySchemaName = entitySchemaName ?? string.Empty,
                            EntityDisplayName = entityDisplayName,
                            AttributeSchemaName = attributeSchemaName ?? string.Empty,
                            AttributeDisplayName = attributeDisplayName,
                            AttributeType = attributeType,
                            DataverseFormat = dataverseFormat,
                            FormatDetails = formatDetails,
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

    private static bool HasPrefix(string? attributeName)
    {
        if (string.IsNullOrEmpty(attributeName))
            return false;

        // Check if the attribute name contains an underscore (indicating a prefix)
        // and doesn't start with common system prefixes
        var systemPrefixes = new[] { "ownerid", "owningbusinessunit", "owningteam", "owninguser", "statecode", "statuscode" };
        
        if (systemPrefixes.Any(prefix => attributeName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            return false;

        return attributeName.Contains('_');
    }

    private static (string DataverseFormat, string FormatDetails) GetDataverseFormatInfo(Microsoft.Xrm.Sdk.Metadata.AttributeMetadata attributeMetadata)
    {
        var dataverseFormat = string.Empty;
        var formatDetails = string.Empty;

        switch (attributeMetadata)
        {
            case StringAttributeMetadata stringAttr:
                if (stringAttr.Format != null)
                {
                    dataverseFormat = stringAttr.Format.ToString();
                    switch (stringAttr.Format.Value)
                    {
                        case StringFormat.Email:
                            formatDetails = "Email";
                            break;
                        case StringFormat.Url:
                            formatDetails = "URL";
                            break;
                        case StringFormat.Phone:
                            formatDetails = "Phone";
                            break;
                        case StringFormat.Text:
                            formatDetails = stringAttr.MaxLength > 1 ? "Single Line of Text" : "Single Line of Text";
                            break;
                        case StringFormat.TextArea:
                            formatDetails = "Multiple Lines of Text";
                            break;
                        case StringFormat.RichText:
                            formatDetails = "Rich Text";
                            break;
                        case StringFormat.Json:
                            formatDetails = "JSON";
                            break;
                        default:
                            formatDetails = stringAttr.Format.ToString();
                            break;
                    }
                }
                else
                {
                    dataverseFormat = "String";
                    formatDetails = stringAttr.MaxLength > 100 ? "Multiple Lines of Text" : "Single Line of Text";
                }
                break;

            case MemoAttributeMetadata memoAttr:
                dataverseFormat = "Memo";
                formatDetails = "Multiple Lines of Text";
                break;

            case IntegerAttributeMetadata intAttr:
                dataverseFormat = "Integer";
                if (intAttr.Format != null)
                {
                    switch (intAttr.Format.Value)
                    {
                        case IntegerFormat.Duration:
                            formatDetails = "Duration";
                            break;
                        case IntegerFormat.TimeZone:
                            formatDetails = "Time Zone";
                            break;
                        case IntegerFormat.Language:
                            formatDetails = "Language";
                            break;
                        case IntegerFormat.Locale:
                            formatDetails = "Locale";
                            break;
                        default:
                            formatDetails = "Whole Number";
                            break;
                    }
                }
                else
                {
                    formatDetails = "Whole Number";
                }
                break;

            case BigIntAttributeMetadata:
                dataverseFormat = "BigInt";
                formatDetails = "Whole Number (Big Integer)";
                break;

            case DecimalAttributeMetadata:
                dataverseFormat = "Decimal";
                formatDetails = "Decimal Number";
                break;

            case DoubleAttributeMetadata:
                dataverseFormat = "Double";
                formatDetails = "Floating Point Number";
                break;

            case MoneyAttributeMetadata:
                dataverseFormat = "Money";
                formatDetails = "Currency";
                break;

            case BooleanAttributeMetadata:
                dataverseFormat = "Boolean";
                formatDetails = "Yes/No";
                break;

            case DateTimeAttributeMetadata dateTimeAttr:
                dataverseFormat = "DateTime";
                if (dateTimeAttr.Format != null)
                {
                    switch (dateTimeAttr.Format.Value)
                    {
                        case DateTimeFormat.DateAndTime:
                            formatDetails = "Date and Time";
                            break;
                        case DateTimeFormat.DateOnly:
                            formatDetails = "Date Only";
                            break;
                        default:
                            formatDetails = "Date and Time";
                            break;
                    }
                }
                else
                {
                    formatDetails = "Date and Time";
                }
                break;

            case PicklistAttributeMetadata:
                dataverseFormat = "Picklist";
                formatDetails = "Choice";
                break;

            case MultiSelectPicklistAttributeMetadata:
                dataverseFormat = "MultiSelectPicklist";
                formatDetails = "Choices";
                break;

            case LookupAttributeMetadata lookupAttr:
                dataverseFormat = "Lookup";
                var targets = lookupAttr.Targets?.FirstOrDefault() ?? "Unknown";
                formatDetails = $"Lookup ({targets})";
                break;

            case ImageAttributeMetadata:
                dataverseFormat = "Image";
                formatDetails = "Image";
                break;

            case FileAttributeMetadata:
                dataverseFormat = "File";
                formatDetails = "File";
                break;

            case UniqueIdentifierAttributeMetadata:
                dataverseFormat = "UniqueIdentifier";
                formatDetails = "Unique Identifier";
                break;

            case EntityNameAttributeMetadata:
                dataverseFormat = "EntityName";
                formatDetails = "Entity Name";
                break;

            default:
                dataverseFormat = attributeMetadata.AttributeType?.ToString() ?? "Unknown";
                formatDetails = attributeMetadata.AttributeTypeName?.Value ?? "Unknown";
                break;
        }

        return (dataverseFormat, formatDetails);
    }
}
