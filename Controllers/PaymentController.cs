using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using GatewayApi.Dto;
using GatewayApi.Services;

namespace GatewayApi.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly PaystackService _paystackService;

    public PaymentsController(PaystackService paystackService)
    {
        _paystackService = paystackService;
    }
    private static readonly Dictionary<string, PaymentResponse> Payments = new();

    [HttpPost]
    public async Task<IActionResult> InitiatePayment([FromBody] PaymentRequestDto request)
    {
        var id = $"PAY-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

        var payment = new PaymentResponse
        {
            Id = id,
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            Amount = request.Amount,
            Status = "pending" 
        };

        Payments[id] = payment;

        // Call Paystack
        var result = await _paystackService.InitiatePaymentAsync(request);

        return Ok(new
        {
            payment,
            status = "success",
            message = "Payment initiated successfully.",
            paystack = JsonConvert.DeserializeObject<object>(result.ToString())
        });
    }

    [HttpGet("{id}")]
    public IActionResult GetPayment(string id)
    {
        if (!Payments.TryGetValue(id, out var payment))
        {
            return NotFound(new
            {
                status = "error",
                message = "Payment not found."
            });
        }

       
        if (payment.Status == "pending")
        {
            payment.Status = "completed";
            Payments[id] = payment;
        }
        return Ok(new
        {
            payment,
            status = "success",
            message = "Payment details retrieved successfully."
        });
    }


}
