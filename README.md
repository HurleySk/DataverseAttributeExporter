# Dataverse Attribute Exporter

A C# console application that exports Dataverse entity attribute metadata to CSV format for entities with a specific publisher prefix.

## Features

- Connects to Dataverse using OAuth authentication
- Filters entities by publisher prefix
- Exports comprehensive attribute metadata including:
  - Table Schema Name
  - Table Display Name
  - Attribute Schema Name
  - Attribute Display Name
  - Attribute Type
  - Attribute Description
- Configurable system attribute inclusion
- CSV export with proper formatting
- Comprehensive logging

## Prerequisites

- .NET 8.0 SDK or later
- Access to a Dataverse environment
- Azure App Registration (for OAuth authentication)

## Setup

### 1. Azure App Registration

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to Azure Active Directory > App registrations
3. Create a new registration
4. Note down the Application (client) ID
5. Add a redirect URI (e.g., `http://localhost:8080`)
6. Grant appropriate API permissions for Dataverse

### 2. Configuration

Update the `appsettings.json` file with your Dataverse connection details:

```json
{
  "DataverseSettings": {
    "ConnectionString": "AuthType=OAuth;Url=https://your-org.crm.dynamics.com;AppId=your-app-id;RedirectUri=your-redirect-uri;LoginPrompt=Auto",
    "PublisherPrefix": "your_prefix"
  },
  "ExportSettings": {
    "OutputPath": "DataverseAttributes.csv",
    "IncludeSystemAttributes": false
  }
}
```

**Connection String Parameters:**
- `Url`: Your Dataverse environment URL
- `AppId`: The Application (client) ID from your Azure App Registration
- `RedirectUri`: The redirect URI you configured in Azure App Registration

**Publisher Prefix:**
- The prefix used by your solution publisher (e.g., "new_" for "new_entity")

### 3. Build and Run

```bash
# Restore packages
dotnet restore

# Build the application
dotnet build

# Run the application
dotnet run
```

## Output

The application will generate a CSV file with the following columns:

| Column | Description |
|--------|-------------|
| Table Schema Name | Logical name of the entity |
| Table Display Name | User-friendly display name of the entity |
| Attribute Schema Name | Logical name of the attribute |
| Attribute Display Name | User-friendly display name of the attribute |
| Attribute Type | Data type of the attribute |
| Attribute Description | Description of the attribute |

## Configuration Options

### ExportSettings

- `OutputPath`: Path and filename for the CSV output file
- `IncludeSystemAttributes`: Whether to include system attributes (default: false)

### DataverseSettings

- `ConnectionString`: OAuth connection string for Dataverse
- `PublisherPrefix`: Prefix to filter entities (e.g., "new_", "contoso_")

## Troubleshooting

### Common Issues

1. **Authentication Errors**
   - Verify your Azure App Registration has correct permissions
   - Check that the redirect URI matches exactly
   - Ensure the App ID is correct in the connection string

2. **Connection Issues**
   - Verify your Dataverse environment URL is correct
   - Check network connectivity to your Dataverse environment

3. **No Data Found**
   - Verify the publisher prefix is correct
   - Check that entities with the specified prefix exist
   - Ensure you have appropriate permissions to read entity metadata

### Logging

The application provides detailed logging to help diagnose issues. Check the console output for:
- Connection status
- Entity count found
- Attribute count retrieved
- Export progress
- Any errors or warnings

## Dependencies

- **Microsoft.PowerPlatform.Dataverse.Client**: Dataverse SDK for .NET
- **CsvHelper**: CSV generation library
- **Microsoft.Extensions.Configuration**: Configuration management
- **Microsoft.Extensions.DependencyInjection**: Dependency injection container
- **Microsoft.Extensions.Logging**: Logging framework

## License

This project is provided as-is for educational and development purposes.
