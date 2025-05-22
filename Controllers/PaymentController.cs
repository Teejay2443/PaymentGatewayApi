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

    public PaymentsController(PaystackService paystackService)
    {
        _paystackService = paystackService;
    }

    // Optional: store payments temporarily for testing
    private static readonly Dictionary<string, PaymentResponse> Payments = new();

    /// <summary>
    /// "This method initiate payment to paystack"
    /// </summary>
    /// <param name="request"></param>
    [HttpPost]
    public async Task<IActionResult> InitiatePayment([FromBody] PaymentRequestDto request)
    {
        // Call Paystack to initialize payment
        var result = await _paystackService.InitiatePaymentAsync(request);

        if (result.Status == "failed")
        {
            return BadRequest(new
            {
                status = "error",
                message = "Failed to initiate payment with Paystack."
            });
        }

        // Store the result temporarily 
        Payments[result.Id] = result;

        // Return success and the Paystack authorization URL
        return Ok(new
        {
            status = "success",
            message = "Payment initiated successfully.",
            data = result
        });
    }

    /// <summary>
    /// "This method verify the payment by id if it has ben initaited"
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
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

        // Simulate completion if still pending
        if (payment.Status == "pending")
        {
            payment.Status = "completed";
            Payments[id] = payment;
        }

        return Ok(new
        {
            status = "success",
            message = "Payment details retrieved successfully.",
            data = payment
        });
    }


    /// <summary>
    /// "This method verify the payment by reference"
    /// </summary>
    /// <param name="reference"></param>
    /// <returns></returns>
    [HttpGet("verify/{reference}")]
    public async Task<IActionResult> VerifyPayment(string reference)
    {
        var result = await _paystackService.VerifyPaymentAsync(reference);

        return Ok(new
        {
            status = result.Status == "completed" ? "success" : "failed",
            message = result.Message,
            data = result
        });
    }

}
