using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace QLKS.Helpers
{
    public class EmailHelper
    {
        private readonly IConfiguration _configuration;

        public EmailHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = false, byte[] attachmentData = null, string attachmentName = null)
        {
            var smtpClient = new SmtpClient(_configuration["Smtp:Host"])
            {
                Port = int.Parse(_configuration["Smtp:Port"]),
                Credentials = new System.Net.NetworkCredential(_configuration["Smtp:Username"], _configuration["Smtp:Password"]),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_configuration["Smtp:FromEmail"], "Khách Sạn Hoàng Gia"),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml,
            };
            mailMessage.To.Add(toEmail);

            if (attachmentData != null && !string.IsNullOrEmpty(attachmentName))
            {
                var attachment = new Attachment(new MemoryStream(attachmentData), attachmentName, "application/pdf");
                mailMessage.Attachments.Add(attachment);
            }

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}