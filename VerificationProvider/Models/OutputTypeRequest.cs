using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace VerificationProvider.Models;

public class OutputTypeRequest
{
    [ServiceBusOutput("email_request", Connection = "ServiceBus")]

    public string? OutputEvent { get; set; }

    public HttpResponseData? HttpResponse { get; set; }
}
