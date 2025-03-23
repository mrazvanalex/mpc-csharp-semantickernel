namespace MCPServer.Options
{
    public class EmailConfiguration
    {
        public required string MailServer { get; set; }
        public required int MailPort { get; set; }
        public required string SenderName { get; set; }
        public required string Sender { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
    }

    public class EmailSettings
    {
        public required EmailConfiguration SMTPServer { get; set; }
    }
}
