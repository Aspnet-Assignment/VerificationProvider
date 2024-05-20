using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VerificationProvider.Data.Contexts;
using VerificationProvider.Models;

namespace VerificationProvider.Functions;

public class GenerateVerificationCode(ILogger<GenerateVerificationCode> logger, IServiceProvider serviceProvider)
{
    private readonly ILogger<GenerateVerificationCode> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    [Function(nameof(GenerateVerificationCode))]
    [ServiceBusOutput("email_request", Connection = "ServiceBus")]
    public async Task<string> Run([ServiceBusTrigger("verification_request", Connection = "ServiceBus")] ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions)
    {
        try
        {
            var vr = UnpackVerificationRequest(message);
            if (vr != null)
            {
                var code = GeneratedCode();
                if (await SaveVerificationRequest(vr.Email, code))
                {
                    var emailRequest = GenerateEmailRequestEmail(vr.Email, code);
                    if (emailRequest != null)
                    {
                        var payload = GenerateServiceBusMessage(emailRequest);
                        if (!string.IsNullOrEmpty(payload))
                        {
                            await messageActions.CompleteMessageAsync(message);
                            return payload;
                        }
                    }
                }
            }
        }

        catch (Exception ex)
        {
            _logger.LogError($"ERROR : Run  :: {ex.Message}");
        }
        return null!;
    }

    public VerificationRequest UnpackVerificationRequest(ServiceBusReceivedMessage message)
    {
        try
        {
            var request = JsonConvert.DeserializeObject <VerificationRequest>(message.Body.ToString());
            if (request != null)
                return request;
        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR : UnpackVerificationRequest  :: {ex.Message}");
        }
        return null!;
    }

    public string GeneratedCode()
    {
        try
        {
            var rnd = new Random();
            var code = rnd.Next(100000, 999999);

            return code.ToString();
        }

        catch (Exception ex)
        {
            _logger.LogError($"ERROR : GeneratedCode  :: {ex.Message}");
        }
        return null!;
    }

    public async Task<bool> SaveVerificationRequest(string email, string code)
    {
        try
        {
            using var context = _serviceProvider.GetRequiredService<DataContext>();
            var existingRequest = await context.VerificationRequests.FirstOrDefaultAsync(x => x.Email == email);
            if (existingRequest != null)
            {
                existingRequest.Code = code;
                existingRequest.ExpiryDate = DateTime.Now.AddMinutes(5);
                context.Entry(existingRequest).State = EntityState.Modified;
            }
            else
            {
                context.VerificationRequests.Add(new() { Email = email, Code = code });
            }

            await context.SaveChangesAsync();
            return true;
        }

        catch (Exception ex)
        {
            _logger.LogError($"ERROR : SaveVerificationRequest  :: {ex.Message}");
        }
        return false;
    }

    public EmailRequest GenerateEmailRequestEmail(string email, string code)
    {
        try
        {
            if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(code))
            {
                var request = new EmailRequest
                {
                    To = email,
                    Subject = $"Verification code {code}",
                    Body = $@"
                        <html lang='en'>
                            <head>
                                <meta charset='UTF-8'>
                                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                                <title>Verification Code</title>
                            </head>
                            <body>
                                <div style='color: #191919; max-width: 500px'>
                                    <div style='background-color: #4F85F6; color: white; text-align: center; padding: 20px 0;'>
                                        <h1 style='font-weight: 400;'>Verification Code</h1>
                                    </div>
                                    <div style='background-color: #f4f4f4; padding: 1rem 2rem;'>
                                        <p>Dear user,</p>
                                        <p>We received a request to sign in to your accound using e-mail {email} Please verify your account using this code:</p>
                                        <p style='font-weight: 700; text-align: center; font-size: 48px; letter-spacing: 8px;'>
                                            {code}
                                        </p>
                                        <div style='color: #191919; font-size: 11px;'>
                                            <p>If you did not request this code, someone might be trying to access your account and we recommend you to change password immediately.</p>
                                        </div>
                                    </div>
                                    <div style='color: #191919; text-align: center; font-size: 11px;'>
                                        <p>Manero, Sveavägen 12, SE-123 45 Stockholm, Sweden</p>
                                    </div>
                                </div>
                            </body>
                    ",
                    PlainText = $"Please verify your account using the code provided here: {code}. If you did not request this code, someone might be trying to access your account and we recommend you to change password immediately."
                };
            }
        }

        catch (Exception ex)
        {
            _logger.LogError($"ERROR : GenerateEmailRequestEmail  :: {ex.Message}");
        }
        return null!;
    }

    public string GenerateServiceBusMessage(EmailRequest emailRequest)
    {
        try
        {
            var payload = JsonConvert.SerializeObject(emailRequest);
            if (!string.IsNullOrEmpty(payload))
                return payload;
        }

        catch (Exception ex)
        {
            _logger.LogError($"ERROR : GenerateServiceBusMessage  :: {ex.Message}");
        }
        return null!;
    }
}
