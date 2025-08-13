#!/bin/bash

echo "Building and running Dataverse Attribute Exporter..."
dotnet build

if [ $? -ne 0 ]; then
    echo "Build failed!"
    exit 1
fi

echo ""
echo "Running the application..."
dotnet run
