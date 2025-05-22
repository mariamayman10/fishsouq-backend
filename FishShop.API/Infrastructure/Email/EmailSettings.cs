namespace FishShop.API.Infrastructure.Email;

public class EmailSettings
{
    public string SmtpHost { get; init; } = default!;
    public int SmtpPort { get; init; }
    public string SmtpUsername { get; init; } = default!;
    public string SmtpPassword { get; init; } = default!;
    public string SenderEmail { get; init; } = default!;
    public string SenderName { get; init; } = default!;
}