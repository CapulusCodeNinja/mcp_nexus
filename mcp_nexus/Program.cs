using NLog.Web;
using mcp_nexus.Tools;
using mcp_nexus.Helper;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging
Console.WriteLine("Configuring logging...");
builder.Logging.ClearProviders();
builder.Logging.AddNLogWeb();
Console.WriteLine("Logging configured with NLog");

// Register services
Console.WriteLine("Registering services...");
builder.Services.AddSingleton<TimeTool>();
Console.WriteLine("Registered TimeTool as singleton");

builder.Services.AddSingleton<CdbSession>();
Console.WriteLine("Registered CdbSession as singleton");

builder.Services.AddSingleton<WindbgTool>();
Console.WriteLine("Registered WindbgTool as singleton");

// Configure MCP server
Console.WriteLine("Configuring MCP server...");
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();
Console.WriteLine("MCP server configured with stdio transport and tools from assembly");

Console.WriteLine("Building application host...");
var host = builder.Build();

Console.WriteLine("Starting MCP Nexus server...");
await host.RunAsync();