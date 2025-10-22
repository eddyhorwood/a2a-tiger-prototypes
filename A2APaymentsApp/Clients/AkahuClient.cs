using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Mime;
using A2APaymentsApp.Config;

namespace A2APaymentsApp.Clients;

public class AkahuClient(IHttpClientFactory httpClientFactory, ILogger<AkahuClient> logger, IOptions<AkahuSettings> akahuOptions) : IAkahuClient
{
    private readonly HttpClient _httpClient = InitializeHttpClient(httpClientFactory, akahuOptions.Value);
    private readonly ILogger<AkahuClient> _logger = logger;
    private readonly AkahuSettings _akahuSettings = akahuOptions.Value;

    private static HttpClient InitializeHttpClient(IHttpClientFactory httpClientFactory, AkahuSettings akahuSettings)
    {
        var httpClient = httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri(akahuSettings.BaseUrl);
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        var credential = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{akahuSettings.AppToken}:{akahuSettings.AppSecret}"));
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {credential}");
        
        return httpClient;
    }

    public async Task<CreatePaymentResponse> CreatePayment(CreatePaymentRequest request)
    {
        try
        {
            _logger.LogInformation("Creating payment for amount {Amount} to {PayeeName}", request.Amount, request.Payee?.Name);
            
            var jsonString = JsonSerializer.Serialize(request);
            _logger.LogInformation("Sending JSON to Akahu: {JsonPayload}", jsonString);
            
            var jsonContent = new StringContent(
                jsonString,
                Encoding.UTF8,
                MediaTypeNames.Application.Json);

            var response = await _httpClient.PostAsync("/v1/one-off-payments", jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Akahu API error {StatusCode}: {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
            }
            
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Payment created successfully for amount {Amount} to {PayeeName}", request.Amount, request.Payee?.Name);                    
            return await response.Content.ReadFromJsonAsync<CreatePaymentResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while creating payment");
            throw;
        }
    }

    public async Task<PollPaymentResponse> PollPaymentStatus(string paymentId)
    {
        try
        {
            _logger.LogInformation("Polling payment status for payment ID: {PaymentId}", paymentId);

            var response = await _httpClient.GetAsync($"/v1/one-off-payments/{paymentId}");
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Payment status retrieved successfully for payment ID: {PaymentId}", paymentId);
            return await response.Content.ReadFromJsonAsync<PollPaymentResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while polling payment status for payment ID: {PaymentId}", paymentId);
            throw;
        }
    }
}