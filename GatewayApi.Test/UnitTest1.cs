using GatewayApi.Controllers;
using GatewayApi.Dto;
using GatewayApi.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
namespace GatewayApi.Test
{
    public class UnitTest1
    {
        private readonly PaymentsController _controller;
        private readonly Mock<PaystackService> _mockService;

        public UnitTest1()
        {
            _mockService = new Mock<PaystackService>();
            _controller = new PaymentsController(_mockService.Object);
        }

        [Fact]
        public async Task InitiatePayment_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var request = new PaymentRequestDto
            {
                CustomerName = "Jane Doe",
                CustomerEmail = "jane@example.com",
                Amount = 2000
            };

            var expected = new PaymentResponse
            {
                Id = "PAY-123",
                CustomerName = "Jane Doe",
                CustomerEmail = "jane@example.com",
                Amount = 2000,
                Status = "pending",
            };

            _mockService.Setup(s => s.InitiatePaymentAsync(request)).ReturnsAsync(expected);

            // Act
            var result = await _controller.InitiatePayment(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<PaymentResponse>(okResult.Value);
            Assert.Equal("PAY-123", response.Id);
            Assert.Equal("pending", response.Status);
        }

        [Fact]
        public async Task GetPaymentStatus_ReturnsOk_WithDetails()
        {
            // Arrange
            var paymentId = "PAY-123";
            var expected = new PaymentResponse
            {
                Id = paymentId,
                CustomerName = "Jane Doe",
                CustomerEmail = "jane@example.com",
                Amount = 2000,
                Status = "completed",
            };
        }
    }
}

