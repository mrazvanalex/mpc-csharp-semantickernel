# mpc-csharp-semantickernel
An example usage of Microsoft Semantic Kernel with OpenAI and the The official C# SDK for the Model Context Protocol which can be found at https://github.com/modelcontextprotocol/csharp-sdk

## Prerequisites
A SMTP Email Server (for the EmailTools)
An OpenAI ApiKey

## How to run the sample
1. Use your OpenAI Developer Dashboard to get an ApiKey.
2. Fill out the appsettings for each separate project. 
3. The email addresses that you can send emails to are hardcoded as a dictionary inside Tools/EmailTool.cs. Update the dictionary with your email list. Feel free to make this a setting.
4. Run the project. The default launchSettings are set to http://localhost:5109

### Appsettings
#### WebAppMCPPoc Project
```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "OpenAI": {
    "ApiKey": "<yourApiKey>",
    "ChatModelId" : "gpt-4o-mini"
  }
}

```
#### MCPServer Project
```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "EmailSettings": {
    "SMTPServer": {
      "MailPort": "465",
      "MailServer": "<yourserver.com>",
      "Sender": "<sender@youremail.com>",
      "SenderName": "<AIEmailSender>",
      "Password": "<EmailPassword>",
      "Username": "<EmailUsername>"
    }
  }
}

```

### Sending Requests:
You can use a tool like postman to send requests. 

Example request:
`POST` `https://localhost:7113/chat`
Body: `{ "text": "send and email toaFriend and tell him that you are an ai in 250 words. Tell him you're using C# to do this.."}`

Replace {someone} with a name you have added in the EmailTool emails dictionary.
```
        private static readonly Dictionary<string, string> emails = new Dictionary<string, string>
        {
            { "example", "mail@example.com" },
            { "aFriend", "friendEmail@gmail.com" }
        };
```

## License
This project is licensed under the MIT License.