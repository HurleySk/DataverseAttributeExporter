# Dataverse Attribute Exporter

A .NET console application that extracts attribute metadata for all entities with a given publisher prefix from a Microsoft Dataverse environment and exports the data to a CSV file.

## Features

- Connects to Microsoft Dataverse environments
- Extracts attribute metadata for entities with a specified publisher prefix
- Exports comprehensive attribute information including:
  - Entity Schema Name
  - Entity Display Name
  - Attribute Schema Name
  - Attribute Display Name
  - Attribute Type
  - Attribute Description
  - Publisher Prefix
- Outputs data to CSV format for easy analysis
- Configurable to include or exclude system entities
- Comprehensive logging

## Prerequisites

- .NET 8.0 SDK or later
- Access to a Microsoft Dataverse environment
- Appropriate permissions to read entity and attribute metadata

## Configuration

1. Copy `appsettings.example.json` to `appsettings.json`
2. Update the configuration values:

```json
{
  "DataverseAttributeExporter": {
    "ConnectionString": "AuthType=OAuth;Username=your-username@yourdomain.com;Password=your-password;Url=https://yourorg.crm.dynamics.com;AppId=51f81489-12ee-4a9e-aaae-a2591f45987d;RedirectUri=app://58145B91-0C36-4500-8554-080854F2AC97;LoginPrompt=Auto",
    "PublisherPrefix": "new",
    "OutputFilePath": "attribute_metadata.csv",
    "IncludeSystemEntities": false
  }
}
```

### Configuration Parameters

- **ConnectionString**: Dataverse connection string with authentication details
- **PublisherPrefix**: The publisher prefix to filter entities and attributes (e.g., "new", "contoso")
- **OutputFilePath**: Path where the CSV file will be saved (default: "attribute_metadata.csv")
- **IncludeSystemEntities**: Whether to include system entities in the export (default: false)

### Connection String Options

#### OAuth with Username/Password
```
AuthType=OAuth;Username=your-username@yourdomain.com;Password=your-password;Url=https://yourorg.crm.dynamics.com;AppId=51f81489-12ee-4a9e-aaae-a2591f45987d;RedirectUri=app://58145B91-0C36-4500-8554-080854F2AC97;LoginPrompt=Auto
```

#### OAuth with Interactive Login
```
AuthType=OAuth;Url=https://yourorg.crm.dynamics.com;AppId=51f81489-12ee-4a9e-aaae-a2591f45987d;RedirectUri=app://58145B91-0C36-4500-8554-080854F2AC97;LoginPrompt=Always
```

#### Client Secret Authentication
```
AuthType=ClientSecret;Url=https://yourorg.crm.dynamics.com;ClientId=your-app-id;ClientSecret=your-client-secret
```

## Usage

### Windows
```bash
run.bat
```

### Linux/macOS
```bash
./run.sh
```

### Manual Execution
```bash
# Build the project
dotnet build

# Run the application
dotnet run
```

### Command Line Arguments

You can override configuration values using command line arguments:

```bash
dotnet run --DataverseAttributeExporter:PublisherPrefix="contoso" --DataverseAttributeExporter:OutputFilePath="contoso_attributes.csv"
```

## Output

The application generates a CSV file with the following columns:

- **Entity Schema Name**: The logical name of the entity
- **Entity Display Name**: The display name of the entity
- **Attribute Schema Name**: The logical name of the attribute
- **Attribute Display Name**: The display name of the attribute
- **Attribute Type**: The data type of the attribute (String, Integer, DateTime, etc.)
- **Attribute Description**: The description of the attribute
- **Publisher Prefix**: The publisher prefix used for filtering

## Filtering Logic

The application uses the following logic to determine which entities and attributes to include:

1. **Entities**: Includes entities whose schema name starts with `{PublisherPrefix}_`
2. **Attributes**: Includes attributes that either:
   - Have a schema name starting with `{PublisherPrefix}_`, OR
   - Belong to an entity that matches the publisher prefix
3. **System Entities**: Optionally includes system entities based on the `IncludeSystemEntities` setting

## Error Handling

- Connection failures are logged with detailed error messages
- Invalid configuration values are validated at startup
- Metadata retrieval errors are logged and handled gracefully
- CSV export errors are logged with stack traces

## Logging

The application uses structured logging with configurable levels:

- **Information**: General application flow and progress
- **Warning**: Non-critical issues (e.g., no attributes found)
- **Error**: Critical errors that prevent operation
- **Debug**: Detailed information for troubleshooting

Log levels can be configured in the `appsettings.json` file under the `Logging` section.

## Troubleshooting

### Connection Issues
- Verify your Dataverse URL is correct
- Ensure your credentials have sufficient permissions
- Check that your organization allows the specified authentication method

### No Data Exported
- Verify the publisher prefix exists in your environment
- Check that entities with the specified prefix contain attributes
- Ensure `IncludeSystemEntities` is set appropriately

### Permission Issues
- The user/app registration needs `Read` permissions on entity metadata
- System Administrator or System Customizer security roles typically have the required permissions

## Dependencies

- Microsoft.PowerPlatform.Dataverse.Client
- Microsoft.Extensions.Configuration
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Hosting
- Microsoft.Extensions.Logging
- CsvHelper

## License

This project is provided as-is for educational and development purposes.
