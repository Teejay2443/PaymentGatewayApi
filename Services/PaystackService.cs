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
                    Amount = request.Amount,
                    Message = "Payment initialization failed."
                };
            }

            // Deserialize Paystack response
            dynamic jsonResponse = JsonConvert.DeserializeObject<dynamic>(responseString);
            string reference = jsonResponse.data.reference;
            string authUrl = jsonResponse.data.authorization_url;

            return new PaymentResponse
            {
                Id = reference,
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                Amount = request.Amount,
                Status = "pending",
                AuthorizationUrl = authUrl, 
                Message = "Payment initiated. Redirect user to authorization_url."
            };
        }

        public async Task<PaymentResponse> VerifyPaymentAsync(string reference)
        {
            var paystackKey = _config["Paystack:SecretKey"];

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", paystackKey);

            var response = await _httpClient.GetAsync($"https://api.paystack.co/transaction/verify/{reference}");
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new PaymentResponse
                {
                    Id = reference,
                    Status = "failed",
                    Message = "Failed to verify payment."
                };
            }

            dynamic jsonResponse = JsonConvert.DeserializeObject<dynamic>(responseString);
            var status = jsonResponse.data.status.ToString();

            return new PaymentResponse
            {
                Id = reference,
                CustomerName = jsonResponse.data.customer.name ?? "Unknown",
                CustomerEmail = jsonResponse.data.customer.email ?? "Unknown",
                Amount = ((decimal)jsonResponse.data.amount) / 100, // convert from kobo to naira
                Status = status == "success" ? "completed" : "failed",
                Message = "Payment verification completed."
            };
        }


    }
}
