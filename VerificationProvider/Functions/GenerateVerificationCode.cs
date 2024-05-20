using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace VerificationProvider.Functions
{
    public class GenerateVerificationCode(ILogger<GenerateVerificationCode> logger, IServiceProvider serviceProvider)
    {
        private readonly ILogger<GenerateVerificationCode> _logger = logger;
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        [Function(nameof(GenerateVerificationCode))]
        public async Task Run([ServiceBusTrigger("verification_request", Connection = "ServiceBus")] ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions)
        {

        }
    }
}
