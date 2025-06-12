using MailKit.Security;
using MimeKit;
using OnlineShoppingCart.Models;

namespace OnlineShoppingCart.Controllers
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        public async Task SendEmailAsync(ConfirmEmployee employee)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(employee.From));
            email.To.Add(MailboxAddress.Parse(employee.Email));
            email.Subject = employee.Subject;

            var bodyText = $@"
Dear {employee.Name},

Welcome to the company!

Your login credentials:
Employee ID: {employee.EmployeeId}
Password: {employee.Password}

Please change your password after your first login.

Best regards,
Admin Team
";

            var builder = new BodyBuilder { TextBody = bodyText };
            email.Body = builder.ToMessageBody();

            using var smtp = new MailKit.Net.Smtp.SmtpClient();
            await smtp.ConnectAsync("smtp.gmail.com", 465, SecureSocketOptions.SslOnConnect);
            await smtp.AuthenticateAsync("minalrazzaq21@gmail.com", "igil taat eubd gujn");
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }















    }
}
