using MCPServer.Options;
using MimeKit;

namespace MCPServer.Services
{
    public class EmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(
            EmailSettings emailSettings)
        {
            _emailSettings = emailSettings;
        }


        public async Task SendSMTPAsync(ICollection<string> recipients, string subject, string message)
        {

            try
            {
                var mimeMessage = new MimeMessage();

                mimeMessage.From.Add(new MailboxAddress(_emailSettings.SMTPServer.SenderName, _emailSettings.SMTPServer.Sender));

                foreach (var recipient in recipients)
                {
                    mimeMessage.To.Add(new MailboxAddress("Email User", recipient));
                }


                mimeMessage.Subject = subject;

                mimeMessage.Body = new TextPart("html")
                {
                    Text = message
                };

                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    await client.ConnectAsync(_emailSettings.SMTPServer.MailServer, _emailSettings.SMTPServer.MailPort);

                    // Note: only needed if the SMTP server requires authentication
                    await client.AuthenticateAsync(_emailSettings.SMTPServer.Username, _emailSettings.SMTPServer.Password);

                    await client.SendAsync(mimeMessage);

                    await client.DisconnectAsync(true);
                }

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }
    }
}
