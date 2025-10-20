# Use the official .NET 9.0 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY XeroDotnetSampleApp/*.csproj XeroDotnetSampleApp/
RUN dotnet restore XeroDotnetSampleApp/XeroDotnetSampleApp.csproj

# Copy the rest of the source code
COPY . .

# Build the application
WORKDIR /src/XeroDotnetSampleApp
RUN dotnet build XeroDotnetSampleApp.csproj -c Release -o /app/build

# Publish the application
RUN dotnet publish XeroDotnetSampleApp.csproj -c Release -o /app/publish

# Use the official .NET 9.0 runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy the published application from the build stage
COPY --from=build /app/publish .

# Expose the port the app runs on
EXPOSE 8080

# Set the entry point
ENTRYPOINT ["dotnet", "XeroDotnetSampleApp.dll"]