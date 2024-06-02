using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using VerificationProvider.Models;
using VerificationProvider.Services;

namespace VerificationProvider.Functions
{
    public class GenerateVerificationCodeUsingHttp(ILogger<GenerateVerificationCodeUsingHttp> logger, VerificationService verificationService)
    {
        private readonly ILogger<GenerateVerificationCodeUsingHttp> _logger = logger;
        private readonly VerificationService _verificationService = verificationService;

        [Function("GenerateVerificationCodeUsingHttp")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var vr = JsonConvert.DeserializeObject<VerificationRequest>(body);


            await _verificationService.SendMessageAsync(vr.Email);
            return new OkObjectResult(new { Status = 200, Message = "Verification code sent " });

            

        }
    }
}
