namespace GatewayApi.Dto
{
    public class PaymentRequestDto
    {
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public decimal Amount { get; set; }
    }
}
