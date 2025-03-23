using McpDotNet;
using MCPServer.Options;
using Serilog;

Log.Logger = new LoggerConfiguration()
           .MinimumLevel.Verbose() // Capture all log levels
           .WriteTo.Console()
           .CreateLogger();

try
{
    Log.Information("Starting server...");


    var builder = Host.CreateApplicationBuilder(args);
    builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);

    var emailSettings = builder.Configuration.GetSection("EmailSettings").Get<EmailSettings>();
    AppSettings.EmailSettings = emailSettings; // so we can use it in Tool class

    builder.Services.AddMcpServer().WithStdioServerTransport()
                                   .WithTools();

    var app = builder.Build();

    await app.RunAsync(new CancellationToken());
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}