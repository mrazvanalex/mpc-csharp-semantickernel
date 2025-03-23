using McpDotNet.Protocol.Types;
using McpDotNet.Server;
using MCPServer.Options;
using MCPServer.Services;
using System.ComponentModel;

namespace MCPServer.Tools
{
    [McpToolType]
    public static class EmailTool
    {
        private static readonly Dictionary<string, string> emails = new Dictionary<string, string>
        {
            { "example", "mail@example.com" },
            { "aFriend", "friendEmail@gmail.com" }
        };


        [McpTool, Description("Sends an Email to someone.")]
        public static async Task<CallToolResponse> SendEmail(string person, string message)
        {
            
            var emailAddress = emails[person];

            var settings = AppSettings.EmailSettings;
            var emailService = new EmailService(settings);

            await emailService.SendSMTPAsync([emailAddress], "test", message);

            return new CallToolResponse()
            {
                Content = [new Content() { Text = "Message Sent", Type = "text" }]
            };
        }
    }
}
