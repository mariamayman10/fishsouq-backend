using System.Net.Mail;
using FishShop.API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace FishShop.API.Infrastructure.Email;

public class EmailSender : IEmailSender<User>
{
    private readonly ICustomEmailService _customEmailService;

    public EmailSender(ICustomEmailService customEmailService)
    {
        _customEmailService = customEmailService;
    }

    public Task SendConfirmationLinkAsync(User user, string email, string confirmationLink)
        => _customEmailService.SendConfirmationLinkAsync(user, email, confirmationLink);

    public Task SendPasswordResetLinkAsync(User user, string email, string resetLink)
        => _customEmailService.SendPasswordResetLinkAsync(user, email, resetLink);

    public Task SendPasswordResetCodeAsync(User user, string email, string resetCode)
        => _customEmailService.SendPasswordResetCodeAsync(user, email, resetCode);
}