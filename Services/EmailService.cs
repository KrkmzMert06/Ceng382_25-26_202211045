using System.Net;
using System.Net.Mail;

namespace CaterFlow.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendOrderConfirmationToUserAsync(string toEmail, string userName, int orderId,
            decimal totalPrice, List<OrderEmailItem> items)
        {
            var subject = $"CaterFlow - Order #{orderId} Confirmation";
            var body = BuildOrderEmailBody(userName, orderId, totalPrice, items, isForCaterer: false);
            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendOrderNotificationToCatererAsync(string toEmail, string catererName,
            string customerName, int orderId, decimal totalPrice, List<OrderEmailItem> items)
        {
            var subject = $"CaterFlow - New Order #{orderId} Received";
            var body = BuildOrderEmailBody(catererName, orderId, totalPrice, items, isForCaterer: true, customerName);
            await SendEmailAsync(toEmail, subject, body);
        }

        private string BuildOrderEmailBody(string recipientName, int orderId, decimal totalPrice,
            List<OrderEmailItem> items, bool isForCaterer, string? customerName = null)
        {
            var itemsHtml = string.Join("", items.Select(i =>
                $@"<tr>
                    <td style='padding:8px 12px;border-bottom:1px solid #eee;'>{i.Name}</td>
                    <td style='padding:8px 12px;border-bottom:1px solid #eee;text-align:center;'>{i.Quantity}</td>
                    <td style='padding:8px 12px;border-bottom:1px solid #eee;text-align:right;'>{i.UnitPrice:C}</td>
                    <td style='padding:8px 12px;border-bottom:1px solid #eee;text-align:right;'>{i.LineTotal:C}</td>
                </tr>"));

            var greeting = isForCaterer
                ? $"Hello {recipientName},<br/>You have received a new order from <strong>{customerName}</strong>."
                : $"Hello {recipientName},<br/>Your order has been confirmed. Thank you for choosing CaterFlow!";

            return $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'/></head>
<body style='font-family:Segoe UI,Arial,sans-serif;margin:0;padding:0;background:#f8f5f0;'>
<div style='max-width:600px;margin:30px auto;background:#fff;border-radius:16px;overflow:hidden;box-shadow:0 4px 20px rgba(0,0,0,0.08);'>
    <div style='background:#1f2937;color:#fff;padding:24px 30px;'>
        <h1 style='margin:0;font-size:24px;'>🍽️ CaterFlow</h1>
    </div>
    <div style='padding:30px;'>
        <p>{greeting}</p>
        <h2 style='color:#8b5e3c;'>Order #{orderId}</h2>
        <table style='width:100%;border-collapse:collapse;margin:16px 0;'>
            <thead>
                <tr style='background:#f8f5f0;'>
                    <th style='padding:10px 12px;text-align:left;'>Item</th>
                    <th style='padding:10px 12px;text-align:center;'>Qty</th>
                    <th style='padding:10px 12px;text-align:right;'>Price</th>
                    <th style='padding:10px 12px;text-align:right;'>Total</th>
                </tr>
            </thead>
            <tbody>{itemsHtml}</tbody>
        </table>
        <div style='text-align:right;padding:12px 0;'>
            <strong style='font-size:18px;color:#8b5e3c;'>Total: {totalPrice:C}</strong>
        </div>
        <hr style='border:none;border-top:1px solid #eee;margin:20px 0;' />
        <p style='color:#6b7280;font-size:14px;'>This is an automated email from CaterFlow. Please do not reply.</p>
    </div>
</div>
</body>
</html>";
        }

        private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
                var senderEmail = _configuration["Email:SenderEmail"] ?? "";
                var senderPassword = _configuration["Email:SenderPassword"] ?? "";
                var senderName = _configuration["Email:SenderName"] ?? "CaterFlow";

                if (string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(senderPassword))
                {
                    _logger.LogWarning("Email credentials not configured. Skipping email to {Email}. Subject: {Subject}", toEmail, subject);
                    return;
                }

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true
                };

                var message = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                message.To.Add(toEmail);

                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}: {Subject}", toEmail, subject);
            }
        }
    }

    public class OrderEmailItem
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }
}
