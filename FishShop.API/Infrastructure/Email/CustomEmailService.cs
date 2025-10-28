using System.Net.Mail;
using FishShop.API.Contracts;
using FishShop.API.Entities;
using Microsoft.Extensions.Options;

namespace FishShop.API.Infrastructure.Email;

public class CustomEmailService: ICustomEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailSender> _logger;
    private readonly SmtpClient _smtpClient;

    public CustomEmailService(IOptions<EmailSettings> emailSettings, SmtpClient smtpClient, ILogger<EmailSender> logger)
    {
        _logger = logger;
        _smtpClient = smtpClient;
        _emailSettings = emailSettings.Value;
    }

    public async Task SendConfirmationLinkAsync(User user, string email, string confirmationLink)
    {
        _logger.LogInformation("Sending email confirmation to {Email}", email);

        var body = GenerateEmailConfirmationBody(confirmationLink);
        await ConstructEmail(email, "Confirm your email address", body);
        _logger.LogInformation("Email confirmation sent successfully to {Email}", email);
    }

    public async Task SendPasswordResetLinkAsync(User user, string email, string resetLink)
    {
        _logger.LogInformation("Sending password reset email to {Email}", email);

        var body = GeneratePasswordResetBody(resetLink);
        await ConstructEmail(email, "Reset your password", body);

        _logger.LogInformation("Password reset email sent successfully to {Email}", email);
    }

    public async Task SendPasswordResetCodeAsync(User user, string email, string resetCode)
    {
        _logger.LogInformation("Sending password reset email to {Email}", email);

        var body = GeneratePasswordResetCodeBody(resetCode);
        await ConstructEmail(email, "Reset your password", body);

        _logger.LogInformation("Password reset email sent successfully to {Email}", email);
    }

    public async Task SendOrderNotificationAsync(string user, decimal orderPrice, string email)
    {
        _logger.LogInformation("Sending order notification to {Email}", email);

        var body = GenerateOrderNotificationBody(user, orderPrice);
        await ConstructEmail(email, "New Order Placed", body);

        _logger.LogInformation("Order Notification email sent successfully to {Email}", email);
    }

    public async Task SendMessageAsync(EmailMessage message)
    {
        var mailMessage = new MailMessage
        {
            From = new MailAddress(message.Email, message.Name),
            Subject = message.Subject,
            IsBodyHtml = true,
            Body = $"{message.Body}<br><br>UserName: {message.Name}<br>Phone Number: {message.Phone}",
        };
        mailMessage.To.Add("magfnb@gmail.com");

        await _smtpClient.SendMailAsync(mailMessage);
    }

    private async Task ConstructEmail(string email, string subject, string body)
    {
        var mailMessage = new MailMessage
        {
            From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
            Subject = subject,
            IsBodyHtml = true,
            Body = body
        };
        mailMessage.To.Add(email);

        await _smtpClient.SendMailAsync(mailMessage);
    }

    private string GenerateEmailConfirmationBody(string confirmationLink)
    {
        return $@"
        <html>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9;'>
                <div style='background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); text-align: center;'>
                    <img src='https://fishsouq.runasp.net/images/Logo.png' alt='FishSouq Logo' style='width: 100px; height: auto; margin-bottom: 20px;' />
                    <h2 style='color: #333; margin-bottom: 20px;'>Welcome to FishSouq!</h2>
                    <p style='color: #666; line-height: 1.5;'>Please confirm your email address by clicking the button below:</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{confirmationLink}' 
                           style='display: inline-block;
                                  padding: 12px 24px;
                                  background-color: #007bff;
                                  color: white;
                                  text-decoration: none;
                                  border-radius: 5px;
                                  font-weight: bold;
                                  box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                            Confirm Email
                        </a>
                    </div>
                    <p style='color: #666; line-height: 1.5;'>If you didn't create an account, you can safely ignore this email.</p>
                    <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
                    <p style='color: #666; margin-bottom: 0;'>Best regards,<br><strong>The FishSouq Team</strong></p>
                </div>
            </body>
        </html>";
    }

    private string GeneratePasswordResetBody(string resetLink)
    {
        return $@"
                <html>
                    <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9;'>
                        <div style='background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                            <h2 style='color: #333; margin-bottom: 20px;'>Password Reset Request 🔑</h2>
                            <p style='color: #666; line-height: 1.5;'>You recently requested to reset your password. Click the button below to reset it:</p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{resetLink}' 
                                   style='display: inline-block;
                                          padding: 12px 24px;
                                          background-color: #007bff;
                                          color: white;
                                          text-decoration: none;
                                          border-radius: 5px;
                                          font-weight: bold;
                                          box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                                    Reset Password
                                </a>
                            </div>
                            <p style='color: #666; line-height: 1.5;'>If you didn't request a password reset, you can safely ignore this email.</p>
                            <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
                            <p style='color: #666; margin-bottom: 0;'>Best regards,<br><strong>The FishShop Team</strong></p>
                        </div>
                    </body>
                </html>";
    }

    private string GeneratePasswordResetCodeBody(string resetCode)
    {
        return $@"
                <html>
                    <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9;'>
                        <div style='background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                            <h2 style='color: #333; margin-bottom: 20px;'>Password Reset Code 🔐</h2>
                            <p style='color: #666; line-height: 1.5;'>You recently requested to reset your password. Use the code below:</p>
                            <div style='text-align: center; margin: 30px 0;
                                      padding: 15px;
                                      background-color: #f8f9fa;
                                      border-radius: 5px;
                                      border: 2px dashed #007bff;'>
                                <p style='font-size: 32px; 
                                         font-weight: bold; 
                                         color: #007bff; 
                                         letter-spacing: 3px;
                                         margin: 0;'>{resetCode}</p>
                            </div>
                            <p style='color: #666; line-height: 1.5;'>If you didn't request a password reset, you can safely ignore this email.</p>
                            <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
                            <p style='color: #666; margin-bottom: 0;'>Best regards,<br><strong>The FishShop Team</strong></p>
                        </div>
                    </body>
                </html>";
    }

    private string GenerateOrderNotificationBody(string username, decimal orderPrice)
    {
        return $@"
        <html>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f4f4f4;'>
                <div style='background-color: #ffffff; padding: 30px; border-radius: 10px; box-shadow: 0 2px 5px rgba(0,0,0,0.1);'>
                    <div style='text-align: center; margin-bottom: 20px;'>
                        <img src='https://fishsouq.runasp.net/images/Logo.png' alt='FishSouq Logo' style='width: 100px; height: auto;' />
                    </div>

                    <h2 style='color: #333; text-align: center;'>📦 New Order Received</h2>

                    <p style='color: #555; line-height: 1.6;'>
                        Hello Manager,
                    </p>

                    <p style='color: #555; line-height: 1.6;'>
                        A new order has just been placed on <strong>FishSouq</strong> by 
                        <strong>{username}</strong>.
                    </p>

                    <div style='background-color: #f0f8ff; border-left: 4px solid #007bff; padding: 15px; border-radius: 6px; margin: 20px 0;'>
                        <p style='margin: 0; color: #333; font-size: 16px;'>
                            <strong>Order Total:</strong> EGP {orderPrice}
                        </p>
                    </div>


                    <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>

                    <p style='color: #777; font-size: 14px; margin-bottom: 0; text-align: center;'>
                        This is an automated message from <strong>FishSouq</strong>. Please do not reply.
                    </p>
                </div>
            </body>
        </html>";
    }
}