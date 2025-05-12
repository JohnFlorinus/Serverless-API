using System.Net.Mail;
using System.Net;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System;
using Batproxy_API.Repository.Repos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace Batproxy_API.Repository.Helpers
{
    public class MailHelper
    {
        private readonly SmtpClient _smtpClient;
        private readonly HashSet<string> _blockList;
        private readonly AccountRepo _accountRepo;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly string _siteUrl;

        public MailHelper(IConfiguration config, HashSet<string> blockList, AccountRepo accountRepo)
        {
            var apiKey = config["Mailjet:ApiKey"];
            var secretKey = config["Mailjet:SecretKey"];
            _smtpClient = new SmtpClient("in-v3.mailjet.com", 587)
            {
                Credentials = new NetworkCredential(apiKey, secretKey),
                EnableSsl = true
            };

            _siteUrl = "http://127.0.0.1:5500";
            _fromEmail = $"no-reply@batproxy.com";
            _fromName = config["Mailjet:MailName"];

            _blockList = blockList;
            _accountRepo = accountRepo;
        }

        public async Task SendVerificationEmail(string toEmail, string token)
        {
            var verifyUrl = $"{_siteUrl}/pages/verifyemail.html?token={token}";

            var html = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>Verify Your Account</title>
</head>
<body style=""font-family:Arial,sans-serif; margin:0; padding:20px; background-color:#f4f4f4;"">
  <table align=""center"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width:600px; background:#ffffff; border-radius:4px; overflow:hidden;"">
    <tr>
      <td style=""padding:20px;"">
        <h2 style=""margin:0 0 20px; color:#333;"">Welcome to {_fromName}!</h2>
        <p style=""color:#555; line-height:1.6;"">
          Thanks for signing up. Please verify your email by clicking the button below:
        </p>
        <p style=""text-align:center; margin:30px 0;"">
          <a href=""{verifyUrl}""
             style=""background-color:#007bff;
                    color:#ffffff;
                    text-decoration:none;
                    padding:12px 24px;
                    border-radius:4px;
                    display:inline-block;"">
            Verify Account
          </a>
        </p>
        <p style=""color:#888; font-size:12px;"">
          If you didn’t request this, you can safely ignore this email.
        </p>
      </td>
    </tr>
  </table>
</body>
</html>";

            var msg = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
                Subject = $"Verify Your {_fromName} Account",
                Body = html,
                IsBodyHtml = true
            };
            msg.To.Add(toEmail);

            await _smtpClient.SendMailAsync(msg);
        }

        public async Task<bool> CheckEmailValid(string email)
        {
            MailAddress addr;
            if (!MailAddress.TryCreate(email, out addr))
                throw new ValidationException("Invalid email format.");

            string host = addr.Host.TrimEnd('.').ToLowerInvariant();

            if (_blockList.Any(d =>
                   host.Equals(d, StringComparison.OrdinalIgnoreCase)
            || host.EndsWith("." + d, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ValidationException("Disposable emails are not allowed.");
            }

            bool emailExists = await _accountRepo.EmailExists(email);
            if (emailExists)
                throw new ValidationException("That email is already taken.");

            return true;
        }
    }
}
