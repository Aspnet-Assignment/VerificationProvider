using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static VerificationTester.GenerateCodeTest;
using VerificationProvider.Models;

namespace VerificationTester;

public class SendMessageTester
{
    private readonly Mock<ServiceBusClient> _mockServiceBusClient;
    private readonly Mock<ServiceBusSender> _mockServiceBusSender;
    private readonly Mock<ILogger<CodeGenerator>> _mockLogger;
    private readonly Mock<CodeGenerator> _mockCodeGenerator;
    private readonly Mock<IVerificationService> _mockVerificationService;
    private readonly CodeGenerator _codeGenerator;
    private readonly VerificationService _verificationService;

    public SendMessageTester()
    {
        _mockServiceBusClient = new Mock<ServiceBusClient>();
        _mockServiceBusSender = new Mock<ServiceBusSender>();
        _mockLogger = new Mock<ILogger<CodeGenerator>>();
        _mockCodeGenerator = new Mock<CodeGenerator>(_mockLogger.Object);
        _mockVerificationService = new Mock<IVerificationService>();

        _codeGenerator = new CodeGenerator(_mockLogger.Object);
        _verificationService = new VerificationService(
            _mockServiceBusClient.Object,
            _mockCodeGenerator.Object,
            _mockVerificationService.Object);
    }

    [Fact]
    public async Task SendMessageAsync_ShouldSendMessage_WhenEmailRequestIsValid()
    {
        
        var email = "test@example.com";
        var code = "123456";
        var payload = "{}";

        _mockCodeGenerator.Setup(cg => cg.GeneratedCode()).Returns(code);
        _mockVerificationService.Setup(vs => vs.SaveVerificationRequest(email, code)).ReturnsAsync(true);
        _mockVerificationService.Setup(vs => vs.GenerateEmailRequestEmail(email, code)).Returns(new EmailRequest());
        _mockVerificationService.Setup(vs => vs.GenerateServiceBusMessage(It.IsAny<EmailRequest>())).Returns(payload);
        _mockServiceBusClient.Setup(sbc => sbc.CreateSender(It.IsAny<string>())).Returns(_mockServiceBusSender.Object);

        
        await _verificationService.SendMessageAsync(email);

        
        _mockServiceBusSender.Verify(s => s.SendMessageAsync(It.Is<ServiceBusMessage>(msg => msg.ContentType == "application/json" && msg.Body.ToString() == payload), default), Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_ShouldNotSendMessage_WhenSaveVerificationRequestFails()
    {
        
        var email = "test@example.com";
        var code = "123456";

        _mockCodeGenerator.Setup(cg => cg.GeneratedCode()).Returns(code);
        _mockVerificationService.Setup(vs => vs.SaveVerificationRequest(email, code)).ReturnsAsync(false);

        
        await _verificationService.SendMessageAsync(email);

        
        _mockServiceBusSender.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default), Times.Never);
    }

    
}


public interface IVerificationService
{
    Task<bool> SaveVerificationRequest(string email, string code);
    EmailRequest GenerateEmailRequestEmail(string email, string code);
    string GenerateServiceBusMessage(EmailRequest emailRequest);
}

public class VerificationService
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly CodeGenerator _codeGenerator;
    private readonly IVerificationService _verificationService;

    public VerificationService(ServiceBusClient serviceBusClient, CodeGenerator codeGenerator, IVerificationService verificationService)
    {
        _serviceBusClient = serviceBusClient;
        _codeGenerator = codeGenerator;
        _verificationService = verificationService;
    }

    public async Task SendMessageAsync(string email)
    {
        var code = _codeGenerator.GeneratedCode();
        if (await _verificationService.SaveVerificationRequest(email, code))
        {
            var emailRequest = _verificationService.GenerateEmailRequestEmail(email, code);
            if (emailRequest != null)
            {
                var payload = _verificationService.GenerateServiceBusMessage(emailRequest);
                if (!string.IsNullOrEmpty(payload))
                {
                    var sender = _serviceBusClient.CreateSender("email_request");
                    await sender.SendMessageAsync(new ServiceBusMessage(payload)
                    {
                        ContentType = "application/json"
                    });
                }
            }
        }
    }
}
