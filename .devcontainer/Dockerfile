# Development container for C# MCP Server
FROM mcr.microsoft.com/devcontainers/dotnet:9.0

# Install additional tools for development
RUN apt-get update && apt-get install -y \
    curl \
    git \
    vim \
    && rm -rf /var/lib/apt/lists/*

# Set working directory
WORKDIR /workspace

# Copy project files for dependency restoration
COPY csharp-entra-mcp-server.sln ./
COPY src/McpServer/McpServer.csproj src/McpServer/
COPY src/ToolsLibrary/ToolsLibrary.csproj src/ToolsLibrary/

# Change ownership to vscode user and restore dependencies
RUN chown -R vscode:vscode /workspace

# Switch to vscode user for dependency restoration
USER vscode

# Restore dependencies
RUN dotnet restore

# The source code will be mounted as a volume, so we don't copy it here
# This allows for live editing during development