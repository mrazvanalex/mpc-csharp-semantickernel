using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Types;
using System.Text.Json;

namespace WebAppMCPPoc
{
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
                var type = fct.Metadata.Parameters.Where(p => p.Name == arg.Key).Select(p => p.ParameterType).FirstOrDefault();
                if (type == typeof(string))
                {
                    dict.Add(arg.Key, (string)arg.Value);
                }
                else if (type == typeof(bool))
                {
                    // arg.Value is something like "true" or "false", so parse it
                    bool parsedBool = bool.Parse((string)arg.Value);
                    dict.Add(arg.Key, parsedBool);
                }
                else if (type == typeof(double))
                {
                    double parsedLong = long.Parse((string)arg.Value);
                    dict.Add(arg.Key, parsedLong);
                }

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

            var parsedSchema = KernelJsonSchema.Parse(tool.InputSchema.GetRawText());
            if (parsedSchema.RootElement.TryGetProperty("properties", out JsonElement properties))
            {
                var requiredElements = new List<string>();
                if (parsedSchema.RootElement.TryGetProperty("required", out JsonElement requiredElementsJson))
                {
                    requiredElements = requiredElementsJson.EnumerateArray().Select(x => x.ToString()).ToList();
                }
   
                foreach (var property in properties.EnumerateObject())
                {
                    var required = (requiredElements is null || requiredElements.Count ==0) ? false : (requiredElements.Where(x => x == property.Name).FirstOrDefault() is not null) ? true : false;

                    var schema = KernelJsonSchema.Parse(property.Value.GetRawText());

                    property.Value.TryGetProperty("default", out var defaultValue);
                    property.Value.TryGetProperty("type", out var type);
                    property.Value.TryGetProperty("description", out var description);
                    var metadata = new KernelParameterMetadata(property.Name)
                    {
                        Description = description.ToString(),
                        DefaultValue = null,
                        IsRequired = required,
                        Schema = schema,
                        ParameterType = GetClrTypeFromJsonType(type.ToString().ToLower() ?? "string")
                    };
                    parameters.Add(metadata);
                }
            }
            //foreach(var item in y.)


            return parameters;

        }


        /// <summary>
        /// Returns a .NET type based on the given JSON type string.
        /// E.g., "number" -> typeof(double), "string" -> typeof(string), etc.
        /// </summary>
        public static Type GetClrTypeFromJsonType(string jsonType)
        {
            // Normalize input (e.g. trim, lowercase, etc. for safety)
            var normalized = jsonType?.Trim().ToLowerInvariant() ?? string.Empty;

            return normalized switch
            {
                "string" => typeof(string),
                "number" => typeof(double),     // or typeof(decimal)/typeof(float)/typeof(int), etc.
                "integer" => typeof(int),        // if you specifically differentiate integer vs. float
                "boolean" => typeof(bool),
                "array" => typeof(object[]),   // or typeof(List<object>) if you prefer
                "object" => typeof(object),     // or typeof(Dictionary<string, object>)
                "null" => typeof(object),     // there's no direct `null` type in .NET, so default to object
                _ => typeof(object)      // fallback
            };
        }
    }


}
