using System.Net.Mail;
using FishShop.API.Contracts;
using FishShop.API.Entities;
using Microsoft.Extensions.Options;

namespace FishShop.API.Infrastructure.Email;

public interface ICustomEmailService
{
    Task SendConfirmationLinkAsync(User user, string email, string confirmationLink);

    Task SendPasswordResetLinkAsync(User user, string email, string resetLink);

    Task SendPasswordResetCodeAsync(User user, string email, string resetCode);

    Task SendOrderNotificationAsync(string user, decimal orderPrice, string email);
    
    Task SendMessageAsync(EmailMessage message);

}