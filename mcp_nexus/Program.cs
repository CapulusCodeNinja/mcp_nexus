using NLog.Web;
using mcp_nexus.Tools;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddNLogWeb();

builder.Services.AddSingleton<TimeTool>();
builder.Services.AddSingleton<WindbgTool>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();