using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GatewayApi.Dto;

namespace GatewayApi.Services
{
    public class PaystackService 
    {
        private readonly HttpClient _httpClient;
        private readonly string _secretKey;

        public PaystackService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _secretKey = configuration["SecretKey"] ?? throw new ArgumentNullException("Paystack SecretKey is missing in configuration");
            _httpClient.BaseAddress = new Uri("https://api.paystack.co/");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);
        }

        public async Task<(string AuthorizationUrl, string Reference)> InitializePayment(PaymentRequestDto request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.CustomerEmail)) throw new ArgumentException("Email is required");
            if (request.Amount <= 0) throw new ArgumentException("Amount must be greater than zero");

            var reference = Guid.NewGuid().ToString();

            var payload = new
            {
                email = request.CustomerEmail,
                amount = request.Amount * 100,
                reference = reference
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("transaction/initialize", content);
            var responseData = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Paystack Error: {responseData}");

            var result = JsonSerializer.Deserialize<PaystackInitializeResponse>(responseData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (result?.Data?.AuthorizationUrl == null)
                throw new Exception("Authorization URL is missing in the response.");

            return (result.Data.AuthorizationUrl, reference);
        }

        public async Task<bool> VerifyPayment(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference)) throw new ArgumentException("Reference cannot be null or empty");

            var response = await _httpClient.GetAsync($"transaction/verify/{reference}");
            var responseData = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Paystack Error: {responseData}");

            var result = JsonSerializer.Deserialize<PaystackVerifyResponse>(responseData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (result?.Data?.Status != "success")
                return false;

            return true;
        }

        private class PaystackInitializeResponse
        {
            public bool Status { get; set; }
            public string Message { get; set; }
            public PaystackInitializeData Data { get; set; }
        }

        private class PaystackInitializeData
        {
            [JsonPropertyName("authorization_url")]
            public string AuthorizationUrl { get; set; }

            [JsonPropertyName("access_code")]
            public string AccessCode { get; set; }

            [JsonPropertyName("reference")]
            public string Reference { get; set; }
        }

        private class PaystackVerifyResponse
        {
            public bool Status { get; set; }
            public string Message { get; set; }
            public PaystackVerifyData Data { get; set; }
        }

        private class PaystackVerifyData
        {
            [JsonPropertyName("status")]
            public string Status { get; set; }
        }
    }
}
