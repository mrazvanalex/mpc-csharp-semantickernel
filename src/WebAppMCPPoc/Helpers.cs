using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Types;
using System.Text.Json;

namespace WebAppMCPPoc
{
    public class ToolFunctionProperties
    {
        public dynamic properties { get; set; }
        public string type { get; set; }
    }

    public static class Helpers
    {
        public static KernelFunction ToKernelFunction(this Tool tool, IMcpClient mcpClient)
        {
            async Task<string> InvokeToolAsync(Kernel kernel, KernelFunction function, KernelArguments arguments, CancellationToken cancellationToken)
            {
                try
                {
                    // Convert arguments to dictionary format expected by mcpdotnet
                    Dictionary<string, object> mcpArguments = [];
                    mcpArguments = function.ToArgumentValue(arguments);

                    // Call the tool through mcpdotnet
                    var result = await mcpClient.CallToolAsync(
                        tool.Name,
                        mcpArguments,
                        cancellationToken: cancellationToken
                    ).ConfigureAwait(false);

                    // Extract the text content from the result
                    return string.Join("\n", result.Content
                        .Where(c => c.Type == "text")
                        .Select(c => c.Text));
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error invoking tool '{tool.Name}': {ex.Message}");

                    // Rethrowing to allow the kernel to handle the exception
                    throw;
                }
            }

            return KernelFunctionFactory.CreateFromMethod(
                method: InvokeToolAsync,
                functionName: tool.Name,
                description: tool.Description,
                parameters: tool.ToParameters()
            );
        }

        public static Dictionary<string, object> ToArgumentValue(this KernelFunction fct, KernelArguments arguments)
        {
            var dict = new Dictionary<string, object>();
            foreach (var arg in arguments)
            {
                dict.Add(arg.Key, arg.Value);
            }

            return dict;
        }

        public static List<KernelParameterMetadata> ToParameters(this Tool tool)
        {
            if (tool.InputSchema.GetPropertyCount() == 0)
            {
                return new List<KernelParameterMetadata>();
            }

            var parameters = new List<KernelParameterMetadata>();
            using var doc = JsonDocument.Parse(tool.InputSchema.GetRawText());
            var root = doc.RootElement;

            // Try to get the "properties" element:
            if (root.TryGetProperty("properties", out JsonElement properties))
            {
                // Enumerate each child property of the "properties" object
                foreach (var property in properties.EnumerateObject())
                {
                    parameters.Add(new KernelParameterMetadata(property.Name));
                }
            }

            return parameters;

        }
    }
}
