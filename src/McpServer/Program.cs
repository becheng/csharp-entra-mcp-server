using Microsoft.Identity.Web;
using ToolsLibrary.Prompts;
using ToolsLibrary.Resources;
using ToolsLibrary.Tools;

var builder = WebApplication.CreateBuilder(args);

// deployed to the app service, use the implicit hostname environment variable to build the URL, 
// otherwise use the configured URL in app settings
var httpMcpServerUrl = (builder.Configuration["WEBSITE_HOSTNAME"] is not null
    ? $"https://{builder.Configuration["WEBSITE_HOSTNAME"]}"
    : builder.Configuration["HttpMcpServerUrl"]) ?? throw new InvalidOperationException("MCP Server URL is not configured.");

// add the Microsoft Identity Web API authentication
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);
var authority = $"https://login.microsoftonline.com/{builder.Configuration["AzureAd:TenantId"]!}/v2.0";

// Add the authN
builder.Services.AddAuthentication()
    .AddMcp(options =>
    {
        options.ResourceMetadata = new()
        {
            ResourceName = "Entra protected MCP demo server",
            Resource = new Uri($"{httpMcpServerUrl!}/mcp"),
            AuthorizationServers = [new Uri(authority)],
            ResourceDocumentation = new Uri($"{httpMcpServerUrl!}/health"),
            ScopesSupported = builder.Configuration["McpScope"] is not null ? [builder.Configuration["McpScope"]!] : ["api://<client_id>/mcp:tools"],
        };
    });

// add authZ
builder.Services.AddAuthorization();

// add MCP tools
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithPrompts<PromptExamples>()
    .WithResources<DocumentationResource>()
    .WithTools<RandomNumberTools>()
    .WithTools<DateTools>();

// Add CORS for HTTP transport support in browsers
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add HttpClient for calling external services if needed by tools
builder.Services.AddHttpClient();

// change to scp or scope if not using magic namespaces from MS
// The scope must be validated as we want to force only delegated access tokens
// The scope is requires to only allow access tokens intended for this API
builder.Services.AddAuthorizationBuilder()
  .AddPolicy("mcp_tools", policy =>
        policy.RequireClaim("http://schemas.microsoft.com/identity/claims/scope", "mcp:tools"));

// Build the app
var app = builder.Build();

// Configure dev and non-dev configs
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();    
}
else
{
    app.UseHttpsRedirection();
}

// Enable CORS
app.UseCors();

// add simple home page
app.MapGet("/health", () => $"Secure MCP server running deployed: UTC: {DateTime.UtcNow}, use /mcp path to use the tools");

// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Map MCP endpoints with authorization
app.MapMcp("/mcp").RequireAuthorization("mcp_tools");

// Run the app
if (app.Environment.IsDevelopment())
{
    app.Run(httpMcpServerUrl);
}
else
{
    // when deployed to app service, just use default binding
    app.Run();
}