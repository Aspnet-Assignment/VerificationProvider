using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VerificationProvider.Data.Contexts;
using VerificationProvider.Models;

namespace VerificationProvider.Functions;

public class ValidateCode(ILogger<ValidateCode> logger, DataContext context)
{
    private readonly ILogger<ValidateCode> _logger = logger;
    private readonly DataContext _context = context;

    [Function("ValidateCode")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        try
        {
            var vr = await UnpackValidateRequestAsync(req);
            if (vr != null)
            {
                var result = await ValidateVerificationCodeAsync(vr);
                if (result)
                    return new OkObjectResult(new { Status = 200, Message = "Verification code accepted" });

            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR : ValidateCode.Run :: {ex.Message}");
        }
        return new UnauthorizedResult();
    }

    public async Task<ValidateRequest> UnpackValidateRequestAsync(HttpRequest req)
    {
        try
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            if (!string.IsNullOrEmpty(body))
            {
                var request = JsonConvert.DeserializeObject<ValidateRequest>(body);
                if (request != null)
                    return request;
            }
            
        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR : UnpackValidateRequest :: {ex.Message}");
        }
        return null!;
    }

    public async Task<bool> ValidateVerificationCodeAsync(ValidateRequest validateRequest)
    {
        try
        {
            var entity = await _context.VerificationRequests.FirstOrDefaultAsync(x => x.Email == validateRequest.Email && x.Code == validateRequest.Code);
            if (entity != null)
            {
                _context.Remove(entity);
                await _context.SaveChangesAsync();

                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR : ValidateVerificationCodeAsync :: {ex.Message}");
        }
        return false;
    }
}
