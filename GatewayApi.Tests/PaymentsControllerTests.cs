using Xunit;
using Moq;
using GatewayApi.Controllers;
using GatewayApi.Services;
using GatewayApi.Dto;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

public class PaymentsControllerTests
{
    private readonly Mock<PaystackService> _mockPaystackService;
    private readonly PaymentsController _controller;

    public PaymentsControllerTests()
    {
        _mockPaystackService = new Mock<PaystackService>(null, null);
        _controller = new PaymentsController(_mockPaystackService.Object);
    }

    [Fact]
    public async Task InitiatePayment_ReturnsSuccessResult()
    {
        // Arrange
        var request = new PaymentRequestDto
        {
            CustomerName = "Alice",
            CustomerEmail = "alice@example.com",
            Amount = 100m
        };

        _mockPaystackService
            .Setup(s => s.InitiatePaymentAsync(It.IsAny<PaymentRequestDto>()))
            .ReturnsAsync("{\"status\":true}");

        // Act
        var result = await _controller.InitiatePayment(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        dynamic response = okResult.Value;

        Assert.Equal("success", (string)response.status);
        Assert.NotNull(response.payment);
        Assert.Equal("Alice", (string)response.payment.CustomerName);
        Assert.Equal(100m, (decimal)response.payment.Amount);
    }

    [Fact]
    public void GetPayment_ReturnsPayment_WhenExists()
    {
        // Arrange
        var request = new PaymentRequestDto
        {
            CustomerName = "Bob",
            CustomerEmail = "bob@example.com",
            Amount = 50m
        };

        // Initiate payment to populate the in-memory store
        var task = _controller.InitiatePayment(request);
        task.Wait();
        var response = task.Result as OkObjectResult;
        dynamic paymentResponse = response.Value;
        string paymentId = paymentResponse.payment.Id;

        // Act
        var getResult = _controller.GetPayment(paymentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(getResult);
        dynamic getResponse = okResult.Value;

        Assert.Equal("success", (string)getResponse.status);
        Assert.Equal(paymentId, (string)getResponse.payment.Id);
        Assert.Equal("completed", (string)getResponse.payment.Status);
    }

    [Fact]
    public void GetPayment_ReturnsNotFound_WhenPaymentDoesNotExist()
    {
        // Act
        var result = _controller.GetPayment("NON_EXISTENT_ID");

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        dynamic response = notFoundResult.Value;

        Assert.Equal("error", (string)response.status);
        Assert.Equal("Payment not found.", (string)response.message);
    }
}
