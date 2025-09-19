using NLog.Web;
using mcp_nexus.Tools;
using mcp_nexus.Helper;
using System.CommandLine;
using System.CommandLine.Parsing;

// Parse command line arguments for --cdb-path
string? customCdbPath = null;
var cdbPathOption = new Option<string?>("--cdb-path", "Custom path to CDB.exe debugger executable");
var rootCommand = new RootCommand("MCP Nexus - Windows Debugging MCP Server") { cdbPathOption };

var parseResult = rootCommand.Parse(args);
if (parseResult.Errors.Count == 0)
{
    customCdbPath = parseResult.GetValueForOption(cdbPathOption);
}

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

builder.Services.AddSingleton<CdbSession>(serviceProvider =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<CdbSession>>();
    return new CdbSession(logger, customCdbPath: customCdbPath);
});
Console.WriteLine("Registered CdbSession as singleton with custom CDB path: {0}", customCdbPath ?? "auto-detect");

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