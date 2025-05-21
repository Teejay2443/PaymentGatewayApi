namespace GatewayApi.Dto
{
    public class PaymentResponse
    {
        public string Id { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public string AuthorizationUrl { get; set; } 
        public string Message { get; set; }          
    }

}
