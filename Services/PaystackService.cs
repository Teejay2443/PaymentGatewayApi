using GatewayApi.Dto;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace GatewayApi.Services
{
    public class PaystackService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public PaystackService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<PaymentResponse> InitiatePaymentAsync(PaymentRequestDto request)
        {
            var paystackKey = _config["Paystack:SecretKey"];

            var payload = new
            {
                email = request.CustomerEmail,
                amount = (int)(request.Amount * 100) // Paystack expects amount in kobo
            };

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", paystackKey);

            var response = await _httpClient.PostAsync("https://api.paystack.co/transaction/initialize", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new PaymentResponse
                {
                    Status = "failed",
                    CustomerName = request.CustomerName,
                    CustomerEmail = request.CustomerEmail,
                    Amount = request.Amount
                };
            }

            // Deserialize Paystack response
            dynamic jsonResponse = JsonConvert.DeserializeObject<dynamic>(responseString);
            string reference = jsonResponse.data.reference;

            return new PaymentResponse
            {
                Id = reference,
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                Amount = request.Amount,
                Status = "pending",
            };
        }
    }
}
