using Carter;
using FishShop.API.Contracts;
using FishShop.API.Infrastructure.Email;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FishShop.API.Features;

public static class SendEmail
{
    public record Query(EmailMessage Message) : IRequest;

    internal sealed class Handler(ICustomEmailService EmailSender, ILogger<SendMailEndpoint> Logger)
        : IRequestHandler<Query>
    {
        public async Task Handle(Query request, CancellationToken ct)
        {
            await EmailSender.SendMessageAsync(request.Message);
            Logger.LogInformation("Email sent successfully to {Email} from {senderEmail}", "mariamayman3131@gmail.com",
                request.Message.Email);
        }
    }
}

public class SendMailEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("api/users/sendMail", async (EmailMessage message, ISender sender) =>
                {
                    await sender.Send(new SendEmail.Query(message));
                    return Results.Ok(new { Message = "Email sent successfully" });
                })
                .WithName("SendEmail");
        }
    }
