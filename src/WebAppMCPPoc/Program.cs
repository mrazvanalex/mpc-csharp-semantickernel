using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Ollama;
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


//#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
//#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
//builder.Services.AddOpenAIChatCompletion(
//    modelId: "gemma-3",
//    endpoint: new Uri("http://localhost:8080"),
//    apiKey: null
//    );
//#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
//#pragma warning restore SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

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


// MCP CLIENT
McpClientOptions braveClientOptions = new()
{
    ClientInfo = new() { Name = "BraveSearch", Version = "1.0.0" }
};

// MCP CLIENT
McpClientOptions gitlabClientOptions = new()
{
    ClientInfo = new() { Name = "GitLabClient", Version = "1.0.0" }
};

var braveServerId = "brave-mcp-server";

McpServerConfig braveServerConfig = new()
{
    Id = braveServerId,
    Name = "Brave MCP Server",
    TransportType = TransportTypes.StdIo,
    TransportOptions = new Dictionary<string, string>
    {
        ["command"] = $"set GITLAB_PERSONAL_ACCESS_TOKEN={builder.Configuration["Gitlab:ApiKey"]} && npx",
        ["arguments"] = "-y @zereight/mcp-gitlab",
    }
};

var gitlabServerId = "gitlab-mcp-server";


McpServerConfig gitlabServerConfig = new()
{
    Id = gitlabServerId,
    Name = "Gitlab MCP Server",
    TransportType = TransportTypes.StdIo,
    TransportOptions = new Dictionary<string, string>
    {
        ["command"] = $"set BRAVE_API_KEY={builder.Configuration["Brave:ApiKey"]} && npx",
        ["arguments"] = "-y @modelcontextprotocol/server-brave-search",
    }
};

var fakturClient = await McpClientFactory.CreateAsync(serverConfig, clientOptions);
var gitLabClient = await McpClientFactory.CreateAsync(gitlabServerConfig, gitlabClientOptions);
var braveClient = await McpClientFactory.CreateAsync(braveServerConfig, braveClientOptions);

var kernelFunctions = new List<KernelFunction>();

// Map Tools to KernelFunctions
await foreach (var tool in fakturClient.ListToolsAsync())
{
    kernelFunctions.Add(tool.ToKernelFunction(fakturClient));
    // Display available tools in console
    Console.WriteLine($"{tool.Name} ({tool.Description})");
}

// Map Tools to KernelFunctions
await foreach (var tool in gitLabClient.ListToolsAsync())
{
    kernelFunctions.Add(tool.ToKernelFunction(gitLabClient));
    // Display available tools in console
    Console.WriteLine($"{tool.Name} ({tool.Description})");
}


// Map Tools to KernelFunctions
await foreach (var tool in braveClient.ListToolsAsync())
{
    kernelFunctions.Add(tool.ToKernelFunction(braveClient));
    // Display available tools in console
    Console.WriteLine($"{tool.Name} ({tool.Description})");
}

// Chat endpoint
app.MapPost("/chat", async (Kernel kernel, ChatRequest req) =>
{
    kernel.Plugins.AddFromFunctions("Test", kernelFunctions);

#pragma warning restore SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    //var result = await kernel.InvokePromptAsync($"{req.Text}", new(executionSettings));
    var executionSettings = new OpenAIPromptExecutionSettings
    {
        Temperature = 0,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        ChatSystemPrompt = "You are Roki. An AI Assistant for Faktur, an Invoicing Application"
    };
    var result = await kernel.InvokePromptAsync($"{req.Text}", new(executionSettings));
    return result.ToString();
})
.WithName("ChatResponse");


app.Run();


