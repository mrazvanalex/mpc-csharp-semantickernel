using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Configuration;
using ModelContextProtocol.Protocol.Transport;
using WebAppMCPPoc;
using WebAppMCPPoc.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddKernel();
builder.Services.AddOpenAIChatCompletion(
        modelId: builder.Configuration["OpenAI:ChatModelId"] ?? "gpt-4o-mini",
        apiKey: builder.Configuration["OpenAI:ApiKey"]!);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


// MCP CLIENT
McpClientOptions clientOptions = new()
{
    ClientInfo = new() { Name = "SimpleToolsConsole", Version = "1.0.0" }
};

var serverId = "mcp-server";
McpServerConfig serverConfig = new()
{
    Id = serverId,
    Name = "MCP Server",
    TransportType = TransportTypes.StdIo,
    TransportOptions = new Dictionary<string, string>
    {
        ["command"] = "dotnet run",
        ["arguments"] = "--verbosity m --project ../MCPServer"
    }
};

var client = await McpClientFactory.CreateAsync(serverConfig, clientOptions);
var kernelFunctions = new List<KernelFunction>();

// Map Tools to KernelFunctions
await foreach (var tool in client.ListToolsAsync())
{
    kernelFunctions.Add(tool.ToKernelFunction(client));
    // Display available tools in console
    Console.WriteLine($"{tool.Name} ({tool.Description})");
}

// Chat endpoint
app.MapPost("/chat", async (Kernel kernel, ChatRequest req) =>
{
    kernel.Plugins.AddFromFunctions("Faktur", kernelFunctions);
    var executionSettings = new OpenAIPromptExecutionSettings
    {
        Temperature = 0,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    };


    var result = await kernel.InvokePromptAsync($"{req.Text}", new(executionSettings));
    return result.ToString();
})
.WithName("ChatResponse");


app.Run();


