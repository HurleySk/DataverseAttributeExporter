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

    public Task<bool> ConnectAsync()
    {
        try
        {
            _logger.LogInformation("Connecting to Dataverse...");
            _serviceClient = new ServiceClient(_connectionString);
            
            if (!_serviceClient.IsReady)
            {
                _logger.LogError("Failed to connect to Dataverse. Connection details: {LastError}", _serviceClient.LastError);
                return Task.FromResult(false);
            }

            _logger.LogInformation("Successfully connected to Dataverse environment: {OrgDetail}", _serviceClient.ConnectedOrgFriendlyName);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to Dataverse");
            return Task.FromResult(false);
        }
    }

    public async Task<List<Models.AttributeMetadata>> GetAttributeMetadataAsync(string[] publisherPrefixes, bool includeSystemEntities = false, bool excludeOotbAttributes = true)
    {
        if (_serviceClient == null || !_serviceClient.IsReady)
        {
            throw new InvalidOperationException("Service client is not connected. Call ConnectAsync first.");
        }

        var attributeMetadataList = new List<Models.AttributeMetadata>();

        try
        {
            _logger.LogInformation("Retrieving entity metadata for publisher prefixes: [{PublisherPrefixes}]", string.Join(", ", publisherPrefixes));

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

                // Determine if we should include this entity based on any of the prefixes
                foreach (var prefix in publisherPrefixes)
                {
                    if (string.IsNullOrEmpty(prefix))
                    {
                        // Blank prefix means OOTB entities only (no prefixed entities)
                        if (entityMetadata.IsCustomEntity == false)
                        {
                            includeEntity = true;
                            break;
                        }
                    }
                    else
                    {
                        // Non-blank prefix means entities with that specific prefix only
                        if (entitySchemaName?.StartsWith(prefix + "_", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            includeEntity = true;
                            break;
                        }
                    }
                }

                // Apply system entities filter
                if (!includeSystemEntities && entityMetadata.IsCustomEntity == false && !publisherPrefixes.Contains(""))
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
                    // Find which prefix caused this entity to be included (for display purposes)
                    string entityMatchingPrefix = string.Empty;
                    foreach (var prefix in publisherPrefixes)
                    {
                        if (string.IsNullOrEmpty(prefix))
                        {
                            if (entityMetadata.IsCustomEntity == false)
                            {
                                entityMatchingPrefix = prefix;
                                break;
                            }
                        }
                        else
                        {
                            if (entitySchemaName?.StartsWith(prefix + "_", StringComparison.OrdinalIgnoreCase) == true)
                            {
                                entityMatchingPrefix = prefix;
                                break;
                            }
                        }
                    }

                    // Include ALL attributes from this entity (table-level filtering only)
                    foreach (var attributeMetadata in entityMetadata.Attributes)
                    {
                        var attributeSchemaName = attributeMetadata.SchemaName;
                        
                        // Determine the best prefix to display for this attribute
                        string attributeDisplayPrefix = entityMatchingPrefix;
                        
                        // If the attribute itself matches a specific prefix, show that instead
                        foreach (var prefix in publisherPrefixes)
                        {
                            if (!string.IsNullOrEmpty(prefix) && 
                                attributeSchemaName?.StartsWith(prefix + "_", StringComparison.OrdinalIgnoreCase) == true)
                            {
                                attributeDisplayPrefix = prefix;
                                break;
                            }
                            else if (string.IsNullOrEmpty(prefix) && !HasPrefix(attributeSchemaName))
                            {
                                attributeDisplayPrefix = prefix;
                                break;
                            }
                        }

                        var attributeDisplayName = attributeMetadata.DisplayName?.UserLocalizedLabel?.Label ?? attributeSchemaName ?? "Unknown";
                        var attributeType = attributeMetadata.AttributeType?.ToString() ?? "Unknown";
                        var attributeDescription = attributeMetadata.Description?.UserLocalizedLabel?.Label ?? string.Empty;
                        var picklistValues = GetPicklistValues(attributeMetadata);
                        
                        var (dataverseType, dataverseFormat) = GetDataverseFormatInfo(attributeMetadata);

                        var metadata = new Models.AttributeMetadata
                        {
                            EntitySchemaName = entitySchemaName ?? string.Empty,
                            EntityDisplayName = entityDisplayName,
                            AttributeSchemaName = attributeSchemaName ?? string.Empty,
                            AttributeDisplayName = attributeDisplayName,
                            AttributeType = attributeType,
                            DataverseType = dataverseType,
                            DataverseFormat = dataverseFormat,
                            AttributeDescription = attributeDescription,
                            PicklistValues = picklistValues,
                            PublisherPrefix = attributeDisplayPrefix
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

    private static (string DataverseType, string DataverseFormat) GetDataverseFormatInfo(Microsoft.Xrm.Sdk.Metadata.AttributeMetadata attributeMetadata)
    {
        string dataverseType = string.Empty;
        string dataverseFormat = string.Empty;

        switch (attributeMetadata)
        {
            case StringAttributeMetadata stringAttr:
                if (stringAttr.Format != null)
                {
                    switch (stringAttr.Format.Value)
                    {
                        case StringFormat.Email:
                            dataverseType = "Single Line of Text";
                            dataverseFormat = "Email";
                            break;
                        case StringFormat.Url:
                            dataverseType = "Single Line of Text";
                            dataverseFormat = "URL";
                            break;
                        case StringFormat.Phone:
                            dataverseType = "Single Line of Text";
                            dataverseFormat = "Phone";
                            break;
                        case StringFormat.Text:
                            dataverseType = "Single Line of Text";
                            dataverseFormat = "Text";
                            break;
                        case StringFormat.TextArea:
                            dataverseType = "Multiple Lines of Text";
                            dataverseFormat = "Text Area";
                            break;
                        case StringFormat.RichText:
                            dataverseType = "Multiple Lines of Text";
                            dataverseFormat = "Rich Text";
                            break;
                        case StringFormat.Json:
                            dataverseType = "Single Line of Text";
                            dataverseFormat = "JSON";
                            break;
                        default:
                            dataverseType = "Single Line of Text";
                            dataverseFormat = stringAttr.Format?.ToString() ?? "Text";
                            break;
                    }
                }
                else
                {
                    dataverseType = stringAttr.MaxLength > 100 ? "Multiple Lines of Text" : "Single Line of Text";
                    dataverseFormat = "Text";
                }
                break;

            case MemoAttributeMetadata memoAttr:
                dataverseType = "Multiple Lines of Text";
                dataverseFormat = "Memo";
                break;

            case IntegerAttributeMetadata intAttr:
                dataverseType = "Whole Number";
                if (intAttr.Format != null)
                {
                    switch (intAttr.Format.Value)
                    {
                        case IntegerFormat.Duration:
                            dataverseFormat = "Duration";
                            break;
                        case IntegerFormat.TimeZone:
                            dataverseFormat = "Time Zone";
                            break;
                        case IntegerFormat.Language:
                            dataverseFormat = "Language";
                            break;
                        case IntegerFormat.Locale:
                            dataverseFormat = "Locale";
                            break;
                        default:
                            dataverseFormat = "Integer";
                            break;
                    }
                }
                else
                {
                    dataverseFormat = "Integer";
                }
                break;

            case BigIntAttributeMetadata:
                dataverseType = "Whole Number";
                dataverseFormat = "Big Integer";
                break;

            case DecimalAttributeMetadata:
                dataverseType = "Decimal Number";
                dataverseFormat = "Decimal";
                break;

            case DoubleAttributeMetadata:
                dataverseType = "Floating Point Number";
                dataverseFormat = "Double";
                break;

            case MoneyAttributeMetadata:
                dataverseType = "Currency";
                dataverseFormat = "Money";
                break;

            case BooleanAttributeMetadata:
                dataverseType = "Yes/No";
                dataverseFormat = "Boolean";
                break;

            case DateTimeAttributeMetadata dateTimeAttr:
                if (dateTimeAttr.Format != null)
                {
                    switch (dateTimeAttr.Format.Value)
                    {
                        case DateTimeFormat.DateAndTime:
                            dataverseType = "Date and Time";
                            dataverseFormat = "Date and Time";
                            break;
                        case DateTimeFormat.DateOnly:
                            dataverseType = "Date Only";
                            dataverseFormat = "Date Only";
                            break;
                        default:
                            dataverseType = "Date and Time";
                            dataverseFormat = "Date and Time";
                            break;
                    }
                }
                else
                {
                    dataverseType = "Date and Time";
                    dataverseFormat = "Date and Time";
                }
                break;

            case PicklistAttributeMetadata:
                dataverseType = "Choice";
                dataverseFormat = "Picklist";
                break;

            case MultiSelectPicklistAttributeMetadata:
                dataverseType = "Choices";
                dataverseFormat = "MultiSelectPicklist";
                break;

            case LookupAttributeMetadata lookupAttr:
                var targets = lookupAttr.Targets?.FirstOrDefault() ?? "Unknown";
                dataverseType = $"Lookup ({targets})";
                dataverseFormat = "Lookup";
                break;

            case ImageAttributeMetadata:
                dataverseType = "Image";
                dataverseFormat = "Image";
                break;

            case FileAttributeMetadata:
                dataverseType = "File";
                dataverseFormat = "File";
                break;

            case UniqueIdentifierAttributeMetadata:
                dataverseType = "Unique Identifier";
                dataverseFormat = "UniqueIdentifier";
                break;

            case EntityNameAttributeMetadata:
                dataverseType = "Entity Name";
                dataverseFormat = "EntityName";
                break;

            default:
                dataverseType = attributeMetadata.AttributeTypeName?.Value ?? "Unknown";
                dataverseFormat = attributeMetadata.AttributeType?.ToString() ?? "Unknown";
                break;
        }

        return (dataverseType, dataverseFormat);
    }

    private static string GetPicklistValues(Microsoft.Xrm.Sdk.Metadata.AttributeMetadata attributeMetadata)
    {
        var picklistValues = new List<string>();

        switch (attributeMetadata)
        {
            case PicklistAttributeMetadata picklistAttr:
                if (picklistAttr.OptionSet?.Options != null)
                {
                    foreach (var option in picklistAttr.OptionSet.Options)
                    {
                        var label = option.Label?.UserLocalizedLabel?.Label ?? "Unknown";
                        var value = option.Value?.ToString() ?? "Unknown";
                        picklistValues.Add($"{label} ({value})");
                    }
                }
                break;

            case MultiSelectPicklistAttributeMetadata multiSelectAttr:
                if (multiSelectAttr.OptionSet?.Options != null)
                {
                    foreach (var option in multiSelectAttr.OptionSet.Options)
                    {
                        var label = option.Label?.UserLocalizedLabel?.Label ?? "Unknown";
                        var value = option.Value?.ToString() ?? "Unknown";
                        picklistValues.Add($"{label} ({value})");
                    }
                }
                break;

            case BooleanAttributeMetadata boolAttr:
                if (boolAttr.OptionSet?.TrueOption != null)
                {
                    var trueLabel = boolAttr.OptionSet.TrueOption.Label?.UserLocalizedLabel?.Label ?? "True";
                    var trueValue = boolAttr.OptionSet.TrueOption.Value?.ToString() ?? "1";
                    picklistValues.Add($"{trueLabel} ({trueValue})");
                }
                if (boolAttr.OptionSet?.FalseOption != null)
                {
                    var falseLabel = boolAttr.OptionSet.FalseOption.Label?.UserLocalizedLabel?.Label ?? "False";
                    var falseValue = boolAttr.OptionSet.FalseOption.Value?.ToString() ?? "0";
                    picklistValues.Add($"{falseLabel} ({falseValue})");
                }
                break;

            case StateAttributeMetadata stateAttr:
                if (stateAttr.OptionSet?.Options != null)
                {
                    foreach (var option in stateAttr.OptionSet.Options)
                    {
                        var label = option.Label?.UserLocalizedLabel?.Label ?? "Unknown";
                        var value = option.Value?.ToString() ?? "Unknown";
                        picklistValues.Add($"{label} ({value})");
                    }
                }
                break;

            case StatusAttributeMetadata statusAttr:
                if (statusAttr.OptionSet?.Options != null)
                {
                    foreach (var option in statusAttr.OptionSet.Options)
                    {
                        var label = option.Label?.UserLocalizedLabel?.Label ?? "Unknown";
                        var value = option.Value?.ToString() ?? "Unknown";
                        picklistValues.Add($"{label} ({value})");
                    }
                }
                break;
        }

        return picklistValues.Any() ? string.Join("; ", picklistValues) : string.Empty;
    }
}
