using Microsoft.EntityFrameworkCore;
using Ortho_xact_api.Models;
using System.Net;
using System.Net.Mail;

namespace Ortho_xact_api.Services;

// Durable email outbox: failures remain pending when SMTP is unavailable.
public class PostingFailureNotifier(IServiceScopeFactory scopes, IConfiguration config,
    ILogger<PostingFailureNotifier> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<OrthoxactContext>();
                var pending = await db.UnsuccessfulSOMasters.Where(x => x.NotificationSentAt == null)
                    .OrderBy(x => x.CreatedAt).Take(20).ToListAsync(stoppingToken);
                foreach (var failure in pending)
                {
                    try
                    {
                        var settings = await db.EmailSettings.AsNoTracking().FirstOrDefaultAsync(stoppingToken)
                            ?? throw new InvalidOperationException("Configure SMTP in Email Settings.");
                        var recipients = await db.Users.AsNoTracking().Where(x => x.Roles == "admin" && x.Email != null)
                            .Select(x => x.Email!).Distinct().ToListAsync(stoppingToken);
                        if (recipients.Count == 0) throw new InvalidOperationException("No administrator email addresses are configured.");
                        using var mail = new MailMessage { From = new MailAddress(settings.FromAddress!),
                            Subject = $"SYSPRO posting requires review: {failure.SalesOrder}",
                            Body = $"Order {failure.SalesOrder} was not fully posted. Reference: {failure.Id}. Open Unsuccessful orders in OrthoX to inspect the response XML and any completed steps. Do not repost until the remote order state has been reconciled."
                        };
                        foreach (var recipient in recipients) mail.To.Add(recipient);
                        using var smtp = new SmtpClient(settings.SmtpServer, int.Parse(settings.PortNumber!)) {
                            Credentials = new NetworkCredential(settings.FromAddress, settings.Password),
                            EnableSsl = config.GetValue("SysproPosting:EmailEnableSsl", true)
                        };
                        await smtp.SendMailAsync(mail, stoppingToken);
                        failure.NotificationSentAt = DateTime.UtcNow;
                        failure.NotificationError = null;
                    }
                    catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                    {
                        failure.NotificationError = ex.Message;
                        logger.LogWarning(ex, "Posting failure notification pending: {Attempt}", failure.Id);
                    }
                    await db.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            { logger.LogError(ex, "Could not process posting failure notifications"); }
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
