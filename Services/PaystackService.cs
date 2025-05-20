using Newtonsoft.Json;
using GatewayApi.Dto;
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

        public async Task<string> InitiatePaymentAsync(PaymentRequestDto request)
        {
            var paystackKey = _config["Paystack:SecretKey"];

            var content = new StringContent(JsonConvert.SerializeObject(new
            {
                email = request.CustomerEmail,
                amount = (int)(request.Amount * 100) // Paystack expects kobo
            }), Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", paystackKey);

            var response = await _httpClient.PostAsync("https://api.paystack.co/transaction/initialize", content);
            var responseString = await response.Content.ReadAsStringAsync();

            return responseString;
        }
    }

}
