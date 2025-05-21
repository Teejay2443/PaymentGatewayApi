using Microsoft.AspNetCore.Mvc;
using GatewayApi.Dto;
using GatewayApi.Services;

namespace GatewayApi.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly PaystackService _paystackService;

    // TEMPORARY in-memory storage (replace with DB in future)
    private static readonly Dictionary<string, PaymentResponse> Payments = new();

    public PaymentsController(PaystackService paystackService)
    {
        _paystackService = paystackService;
    }

    [HttpPost]
    public async Task<IActionResult> InitiatePayment([FromBody] PaymentRequestDto request)
    {
        // Call Paystack first
        var paystackResult = await _paystackService.InitializePayment(request);

        // Use Paystack reference as our payment ID
        var payment = new PaymentResponse
        {
            Id = paystackResult.Reference,
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            Amount = request.Amount,
            Status = "pending"
        };

        // Store in-memory
        Payments[payment.Id] = payment;

        return Ok(new
        {
            payment,
            status = "success",
            message = "Payment initiated successfully.",
            authorizationUrl = paystackResult.AuthorizationUrl
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPayment(string id)
    {
        if (!Payments.TryGetValue(id, out var payment))
        {
            return NotFound(new
            {
                status = "error",
                message = "Payment not found."
            });
        }

        // Check real Paystack status
        var isSuccessful = await _paystackService.VerifyPayment(id);
        payment.Status = isSuccessful ? "completed" : "failed";

        // Update memory storage
        Payments[id] = payment;

        return Ok(new
        {
            payment,
            status = "success",
            message = "Payment details retrieved successfully."
        });
    }
}
